using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentValidation;
using FluentValidation.Results;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Xdb.Api.Application.Assets.Writes;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects.Commands.RegenerateAll;
using Xdb.Api.Infrastructure.Data;
using Xdb.Api.Infrastructure.Data.Entities;
using Xdb.Core.Common;

using DataType = Xdb.Api.Application.Common.Metadata.DataType;

namespace Xdb.Api.Application.Assets.Commands.CreateAsset;

/// <param name="Id">The new asset's id.</param>
/// <param name="Failures">Gen-table refreshes that failed; the asset itself was still created.</param>
public sealed record CreateAssetResult(Guid Id, IReadOnlyList<GenerationFailure> Failures);

public interface ICreateAssetCommandHandler
{
    /// <summary>The new asset (Created); Conflict when the id is taken; Validation for a bad command.</summary>
    Task<Result<CreateAssetResult>> ExecuteAsync(CreateAssetCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Replaces the legacy Asset_Create / Asset_CreateInstance plus PropertyValues_Save path (AssetWriter and
/// SubmittalEngine over raw SQL). The asset, one reviewed submittal, its name value and its property values are
/// written together, approved by the actor; the original wrote them statement by statement in two passes. The
/// asset is then merged into its type stack's gen tables (the original queued that in the background). Not carried
/// over: legacy narratives (written for fixed legacy types), parent relations and view default values.
/// </summary>
public sealed class CreateAssetCommandHandler(
    XdbDataContext db,
    IValidator<CreateAssetCommand> validator,
    GenTableRefresher genTables,
    IDateTimeProvider clock,
    ILogger<CreateAssetCommandHandler> logger) : ICreateAssetCommandHandler
{
    private sealed record LinkedProperty(Guid Id, string Name, DataType DataType, Guid? PickListId, Guid? AssetTypeId, bool? AssetTypeIsInstance,
        Guid? RoleId, bool AllowMultiValue);

    public async Task<Result<CreateAssetResult>> ExecuteAsync(CreateAssetCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Failed(ResultStatus.Validation, validation.Errors);

        var isInstance = command.InstanceOfId.HasValue;
        var errors = await ValidateAssetAsync(command, isInstance, cancellationToken);
        if (errors.Count > 0)
            return Failed(ResultStatus.Validation, errors);

        var (values, valueErrors) = await NormalizeValuesAsync(command, isInstance, cancellationToken);
        if (valueErrors.Count > 0)
            return Failed(ResultStatus.Validation, valueErrors);

        var id = command.Id ?? Guid.NewGuid();
        if (await db.Assets.AsNoTracking().AnyAsync(a => a.Id == id, cancellationToken))
            return Failed(ResultStatus.Conflict, [new ValidationFailure(nameof(CreateAssetCommand.Id), $"Asset {id} already exists.")]);

        var now = clock.UtcNow();
        var submittalId = Guid.NewGuid();
        db.Assets.Add(new Asset
        {
            Id = id,
            AssetTypeId = command.AssetTypeId,
            Name = command.Name,
            // NULL, never "": readers fall back with ISNULL(DisplayValue, Name).
            DisplayValue = null,
            Description = command.Description,
            IsInstance = isInstance,
            InstanceOfId = command.InstanceOfId,
            Created = now,
            CreatedBy = command.ActorId,
            Approved = now,
            ApprovedBy = command.ActorId
        });
        db.PropertyValueSubmittals.Add(new PropertyValueSubmittal
        {
            Id = submittalId, AssetId = id, Created = now, CreatedBy = command.ActorId, Reviewed = now, ReviewedBy = command.ActorId
        });
        // The name is also a value, so the asset's history starts with it, as the original's creations did.
        db.PropertyValues.Add(Value(SystemPropertyIds.AssetName, id, submittalId, command.Name, order: null, now, command.ActorId));
        foreach (var (propertyId, text, order) in values)
            db.PropertyValues.Add(Value(propertyId, id, submittalId, text, order, now, command.ActorId));
        await db.SaveChangesAsync(cancellationToken);

        var failures = new List<GenerationFailure>();
        try
        {
            await genTables.InsertAsync(id, isInstance, command.AssetTypeId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Asset {Id} was created but its gen-table rows failed", id);
            failures.Add(new GenerationFailure("Asset", id, ex.Message));
        }

        return new Success<CreateAssetResult>(new CreateAssetResult(id, failures), ResultStatus.Created);
    }

    private async Task<List<ValidationFailure>> ValidateAssetAsync(CreateAssetCommand command, bool isInstance, CancellationToken cancellationToken)
    {
        var errors = new List<ValidationFailure>();

        if (!await db.Members.AsNoTracking().AnyAsync(m => m.Id == command.ActorId && m.Deleted == null, cancellationToken))
            errors.Add(new ValidationFailure(nameof(CreateAssetCommand.ActorId), "The actor is not a live member."));

        var type = await db.AssetTypes.AsNoTracking()
            .Where(t => t.Id == command.AssetTypeId && t.Deleted == null)
            .Select(t => new { t.AllowAssets, t.InstanceLabel })
            .SingleOrDefaultAsync(cancellationToken);
        if (type is null)
            errors.Add(new ValidationFailure(nameof(CreateAssetCommand.AssetTypeId), $"Asset type {command.AssetTypeId} does not exist."));
        else if (!type.AllowAssets)
            errors.Add(new ValidationFailure(nameof(CreateAssetCommand.AssetTypeId), "The asset type holds no assets of its own."));
        else if (isInstance && string.IsNullOrEmpty(type.InstanceLabel))
            errors.Add(new ValidationFailure(nameof(CreateAssetCommand.InstanceOfId), "The asset type has no instances."));

        if (command.InstanceOfId is { } definitionId
            && !await db.Assets.AsNoTracking().AnyAsync(a => a.Id == definitionId && a.Deleted == null && !a.IsInstance
                && a.AssetTypeId == command.AssetTypeId, cancellationToken))
            errors.Add(new ValidationFailure(nameof(CreateAssetCommand.InstanceOfId), $"{definitionId} is not a live definition of the asset type."));

        return errors;
    }

    private async Task<(List<(Guid PropertyId, string Text, int? Order)> Values, List<ValidationFailure> Errors)> NormalizeValuesAsync(
        CreateAssetCommand command, bool isInstance, CancellationToken cancellationToken)
    {
        var values = new List<(Guid, string, int?)>();
        var errors = new List<ValidationFailure>();
        if (command.Values.Count == 0)
            return (values, errors);

        var linked = await db.AssetTypesProperties.AsNoTracking()
            .Where(l => l.AssetTypeId == command.AssetTypeId && l.IsInstance == isInstance && l.Deleted == null && l.Property.Deleted == null)
            .Select(l => new LinkedProperty(l.Property.Id, l.Property.Name, (DataType)l.Property.DataTypeId, l.Property.PickListId,
                l.Property.AssetTypeId, l.Property.AssetTypeIsInstance, l.Property.RoleId, l.Property.AllowMultiValue))
            .Distinct()
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var counts = new Dictionary<Guid, int>();
        for (var i = 0; i < command.Values.Count; i++)
        {
            var value = command.Values[i];
            var field = $"{nameof(CreateAssetCommand.Values)}[{i}]";
            if (!linked.TryGetValue(value.PropertyId, out var property))
            {
                errors.Add(new ValidationFailure(field, $"Property {value.PropertyId} is not linked to the asset type at this level."));
                continue;
            }

            counts.TryGetValue(property.Id, out var seen);
            counts[property.Id] = seen + 1;
            if (seen > 0 && !property.AllowMultiValue)
            {
                errors.Add(new ValidationFailure(field, $"'{property.Name}' takes a single value."));
                continue;
            }

            var text = PropertyValueText.Normalize(property.DataType, value.Value, out var error);
            if (text is null)
            {
                errors.Add(new ValidationFailure(field, $"'{property.Name}': {error}"));
                continue;
            }
            if (await ReferenceErrorAsync(property, Guid.TryParse(text, out var referenced) ? referenced : Guid.Empty, cancellationToken) is { } referenceError)
            {
                errors.Add(new ValidationFailure(field, $"'{property.Name}': {referenceError}"));
                continue;
            }
            if (values.Any(v => v.Item1 == property.Id && string.Equals(v.Item2, text, StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add(new ValidationFailure(field, $"'{property.Name}' has the same value twice."));
                continue;
            }

            values.Add((property.Id, text, property.AllowMultiValue ? seen : null));
        }

        return (values, errors);
    }

    /// <summary>The id a reference value names must exist and fit the property; scalar types have nothing to check.</summary>
    private async Task<string?> ReferenceErrorAsync(LinkedProperty property, Guid id, CancellationToken cancellationToken)
    {
        switch (property.DataType)
        {
            case DataType.PickList:
                return await db.PickListValues.AsNoTracking().AnyAsync(v => v.Id == id && v.PickListId == property.PickListId && v.Deleted == null, cancellationToken)
                    ? null
                    : $"{id} is not a value of the property's pick list.";
            case DataType.Asset:
                // A grouping type holds no assets itself; its descendants' assets are its selectable values.
                var typeIds = property.AssetTypeId is { } targetId ? await SelfAndDescendantsAsync(targetId, cancellationToken) : [];
                return await db.Assets.AsNoTracking().AnyAsync(a => a.Id == id && a.Deleted == null && typeIds.Contains(a.AssetTypeId)
                        && (property.AssetTypeIsInstance == null || a.IsInstance == property.AssetTypeIsInstance), cancellationToken)
                    ? null
                    : $"{id} is not a live asset of the type the property refers to.";
            case DataType.User:
                if (!await db.Members.AsNoTracking().AnyAsync(m => m.Id == id && m.Deleted == null, cancellationToken))
                    return $"{id} is not a live member.";
                return property.RoleId is not { } roleId
                    || await db.RoleMembers.AsNoTracking().AnyAsync(m => m.MemberId == id && m.RoleId == roleId && m.Deleted == null, cancellationToken)
                    ? null
                    : $"{id} is not a member of the role the property is limited to.";
            default:
                return null;
        }
    }

    /// <summary>The type and its live descendants, through AssetTypeRelations.</summary>
    private async Task<List<Guid>> SelfAndDescendantsAsync(Guid assetTypeId, CancellationToken cancellationToken)
    {
        var all = new List<Guid> { assetTypeId };
        var level = all.ToList();
        while (level.Count > 0)
        {
            level = (await db.AssetTypeRelations.AsNoTracking()
                    .Where(r => level.Contains(r.ParentAssetTypeId) && r.Deleted == null)
                    .Select(r => r.ChildAssetTypeId)
                    .ToListAsync(cancellationToken))
                .Except(all)
                .ToList();
            all.AddRange(level);
        }
        return all;
    }

    private static PropertyValue Value(Guid propertyId, Guid assetId, Guid submittalId, string text, int? order, DateTime now, Guid actorId) => new()
    {
        Id = Guid.NewGuid(),
        PropertyId = propertyId,
        AssetId = assetId,
        SubmittalGroupId = submittalId,
        Value = text,
        Order = order,
        Created = now,
        CreatedBy = actorId,
        // The gen-table decoders read approved values only.
        Approved = now,
        ApprovedBy = actorId
    };

    private static Failure<CreateAssetResult> Failed(ResultStatus status, List<ValidationFailure> errors) =>
        new(default!, status, errors);
}
