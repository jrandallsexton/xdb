using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentValidation;
using FluentValidation.Results;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Xdb.Api.Application.GeneratedObjects.Commands.RegenerateAll;
using Xdb.Api.Application.GeneratedObjects.GenTables;
using Xdb.Api.Infrastructure.Data;
using Xdb.Api.Infrastructure.Data.Entities;
using Xdb.Core.Common;

namespace Xdb.Api.Application.AssetTypes.Commands.CreateAssetType;

/// <param name="Id">The new type's id.</param>
/// <param name="GenTables">Gen tables built (decoded and raw, for definitions, and for instances when the type has an instance label).</param>
/// <param name="Failures">Generation that failed; the type itself was still created.</param>
public sealed record CreateAssetTypeResult(Guid Id, int GenTables, IReadOnlyList<GenerationFailure> Failures);

public interface ICreateAssetTypeCommandHandler
{
    /// <summary>The new type (Created); Conflict when the id is taken; Validation for a bad command.</summary>
    Task<Result<CreateAssetTypeResult>> ExecuteAsync(CreateAssetTypeCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Replaces the create half of the legacy AssetType_Save (AssetTypeSaver with spr_AssetType_Save, a plain upsert).
/// The type, its parent relation, the links inherited from the parent and its own links are written together; the
/// original wrote them one statement at a time and left inherited links to the client. The type's gen tables are
/// then built, which the original left to a separate step. Ancestors' gen tables are not rebuilt here.
/// </summary>
public sealed class CreateAssetTypeCommandHandler(
    XdbDataContext db,
    IValidator<CreateAssetTypeCommand> validator,
    IGenTableGenerator genTables,
    IDateTimeProvider clock,
    ILogger<CreateAssetTypeCommandHandler> logger) : ICreateAssetTypeCommandHandler
{
    public async Task<Result<CreateAssetTypeResult>> ExecuteAsync(CreateAssetTypeCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Failed(ResultStatus.Validation, validation.Errors);

        var inherited = command.ParentId is { } parentId
            ? await db.AssetTypesProperties.AsNoTracking()
                .Where(l => l.AssetTypeId == parentId && l.Deleted == null)
                .Select(l => new CreateAssetTypeProperty(l.PropertyId, l.IsInstance))
                .Distinct()
                .ToListAsync(cancellationToken)
            : [];

        var errors = await ValidateReferencesAsync(command, inherited, cancellationToken);
        if (errors.Count > 0)
            return Failed(ResultStatus.Validation, errors);

        var id = command.Id ?? Guid.NewGuid();
        if (await db.AssetTypes.AsNoTracking().AnyAsync(t => t.Id == id, cancellationToken))
            return Failed(ResultStatus.Conflict, [new ValidationFailure(nameof(CreateAssetTypeCommand.Id), $"Asset type {id} already exists.")]);

        var now = clock.UtcNow();
        db.AssetTypes.Add(new AssetType
        {
            Id = id,
            Name = command.Name,
            Pluralization = command.Pluralization,
            DefinitionLabel = command.DefinitionLabel,
            InstanceLabel = command.InstanceLabel,
            AllowAssets = command.AllowAssets,
            IsSystem = false,
            Created = now,
            CreatedBy = command.ActorId
        });
        if (command.ParentId is { } parent)
        {
            db.AssetTypeRelations.Add(new AssetTypeRelation
            {
                Id = Guid.NewGuid(), ParentAssetTypeId = parent, ChildAssetTypeId = id, Created = now, CreatedBy = command.ActorId
            });
        }
        foreach (var link in inherited)
            db.AssetTypesProperties.Add(Link(id, link, isInherited: true, now, command.ActorId));
        foreach (var link in command.Properties)
            db.AssetTypesProperties.Add(Link(id, link, isInherited: false, now, command.ActorId));
        await db.SaveChangesAsync(cancellationToken);

        var failures = new List<GenerationFailure>();
        var tables = 0;
        try
        {
            tables = await genTables.GenerateAsync(id, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Asset type {Id} was created but its gen tables failed", id);
            failures.Add(new GenerationFailure("AssetType", id, ex.Message));
        }

        return new Success<CreateAssetTypeResult>(new CreateAssetTypeResult(id, tables, failures), ResultStatus.Created);
    }

    private async Task<List<ValidationFailure>> ValidateReferencesAsync(CreateAssetTypeCommand command,
        IReadOnlyCollection<CreateAssetTypeProperty> inherited, CancellationToken cancellationToken)
    {
        var errors = new List<ValidationFailure>();

        if (!await db.Members.AsNoTracking().AnyAsync(m => m.Id == command.ActorId && m.Deleted == null, cancellationToken))
            errors.Add(new ValidationFailure(nameof(CreateAssetTypeCommand.ActorId), "The actor is not a live member."));

        // AssetTypes.Name is unique across all rows, deleted ones included (uc_Unique_AssetTypes).
        if (await db.AssetTypes.AsNoTracking().AnyAsync(t => t.Name == command.Name, cancellationToken))
            errors.Add(new ValidationFailure(nameof(CreateAssetTypeCommand.Name), $"An asset type named '{command.Name}' already exists."));

        if (command.ParentId is { } parentId
            && !await db.AssetTypes.AsNoTracking().AnyAsync(t => t.Id == parentId && t.Deleted == null, cancellationToken))
            errors.Add(new ValidationFailure(nameof(CreateAssetTypeCommand.ParentId), $"Parent asset type {parentId} does not exist."));

        var propertyIds = command.Properties.Select(l => l.PropertyId).Distinct().ToList();
        var properties = await db.Properties.AsNoTracking()
            .Where(p => propertyIds.Contains(p.Id) && p.Deleted == null)
            .Select(p => new { p.Id, p.IsSystem })
            .ToDictionaryAsync(p => p.Id, p => p.IsSystem, cancellationToken);
        foreach (var propertyId in propertyIds)
        {
            if (!properties.TryGetValue(propertyId, out var isSystem))
                errors.Add(new ValidationFailure(nameof(CreateAssetTypeCommand.Properties), $"Property {propertyId} does not exist."));
            else if (isSystem)
                errors.Add(new ValidationFailure(nameof(CreateAssetTypeCommand.Properties), $"Property {propertyId} is a system property."));
        }

        foreach (var link in command.Properties.Where(inherited.Contains))
            errors.Add(new ValidationFailure(nameof(CreateAssetTypeCommand.Properties),
                $"Property {link.PropertyId} is already inherited from the parent at the same level."));

        return errors;
    }

    private static AssetTypesProperty Link(Guid assetTypeId, CreateAssetTypeProperty link, bool isInherited, DateTime now, Guid actorId) => new()
    {
        Id = Guid.NewGuid(),
        AssetTypeId = assetTypeId,
        PropertyId = link.PropertyId,
        IsInstance = link.IsInstance,
        IsInherited = isInherited,
        IsInheritedValue = false,
        Created = now,
        CreatedBy = actorId
    };

    private static Failure<CreateAssetTypeResult> Failed(ResultStatus status, List<ValidationFailure> errors) =>
        new(default!, status, errors);
}
