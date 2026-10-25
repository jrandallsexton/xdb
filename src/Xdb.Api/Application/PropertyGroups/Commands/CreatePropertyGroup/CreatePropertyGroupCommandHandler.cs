using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentValidation;
using FluentValidation.Results;

using Microsoft.EntityFrameworkCore;

using Xdb.Api.Infrastructure.Data;
using Xdb.Api.Infrastructure.Data.Entities;
using Xdb.Core.Common;

namespace Xdb.Api.Application.PropertyGroups.Commands.CreatePropertyGroup;

public interface ICreatePropertyGroupCommandHandler
{
    /// <summary>The new group's id (Created); Conflict when the id is taken; Validation for a bad command.</summary>
    Task<Result<Guid>> ExecuteAsync(CreatePropertyGroupCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Replaces the create half of the legacy PropertyGroup_Save (PropertyGroupSaver with spr_PropertyGroup_Save and
/// spr_PropertyGroupPropertyRelation_Save, plain upserts). The group and its members are written together; the
/// original wrote them one statement at a time and checked only the actor, the type and the name. Here a member must
/// be linked to the type at the group's level (or be a system property), names are unique per type and level, and
/// order comes from position. Member default values are not carried over: almost all were empty, and nothing that
/// creates assets applies them.
/// </summary>
public sealed class CreatePropertyGroupCommandHandler(
    XdbDataContext db,
    IValidator<CreatePropertyGroupCommand> validator,
    IDateTimeProvider clock) : ICreatePropertyGroupCommandHandler
{
    public async Task<Result<Guid>> ExecuteAsync(CreatePropertyGroupCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Failed(ResultStatus.Validation, validation.Errors);

        var errors = await ValidateGroupAsync(command, cancellationToken);
        errors.AddRange(await ValidatePropertiesAsync(command, cancellationToken));
        if (errors.Count > 0)
            return Failed(ResultStatus.Validation, errors);

        var id = command.Id ?? Guid.NewGuid();
        if (await db.PropertyGroups.AsNoTracking().AnyAsync(g => g.Id == id, cancellationToken))
            return Failed(ResultStatus.Conflict, [new ValidationFailure(nameof(CreatePropertyGroupCommand.Id), $"Property group {id} already exists.")]);

        var now = clock.UtcNow();
        db.PropertyGroups.Add(new PropertyGroup
        {
            Id = id,
            AssetTypeId = command.AssetTypeId,
            IsInstance = command.IsInstance,
            Name = command.Name,
            DisplayValue = command.DisplayValue ?? command.Name,
            Created = now,
            CreatedBy = command.ActorId
        });
        for (var i = 0; i < command.Properties.Count; i++)
        {
            var member = command.Properties[i];
            db.PropertyGroupsProperties.Add(new PropertyGroupsProperty
            {
                Id = Guid.NewGuid(),
                PropertyGroupId = id,
                PropertyId = member.PropertyId,
                Order = i,
                IsRequired = member.IsRequired,
                IsReadOnly = member.IsReadOnly,
                Created = now,
                CreatedBy = command.ActorId
            });
        }
        await db.SaveChangesAsync(cancellationToken);

        return new Success<Guid>(id, ResultStatus.Created);
    }

    private async Task<List<ValidationFailure>> ValidateGroupAsync(CreatePropertyGroupCommand command, CancellationToken cancellationToken)
    {
        var errors = new List<ValidationFailure>();

        if (!await db.Members.AsNoTracking().AnyAsync(m => m.Id == command.ActorId && m.Deleted == null, cancellationToken))
            errors.Add(new ValidationFailure(nameof(CreatePropertyGroupCommand.ActorId), "The actor is not a live member."));

        var type = await db.AssetTypes.AsNoTracking()
            .Where(t => t.Id == command.AssetTypeId && t.Deleted == null)
            .Select(t => new { t.InstanceLabel })
            .SingleOrDefaultAsync(cancellationToken);
        if (type is null)
            errors.Add(new ValidationFailure(nameof(CreatePropertyGroupCommand.AssetTypeId), $"Asset type {command.AssetTypeId} does not exist."));
        else if (command.IsInstance && string.IsNullOrEmpty(type.InstanceLabel))
            errors.Add(new ValidationFailure(nameof(CreatePropertyGroupCommand.IsInstance), "The asset type has no instances."));

        if (await db.PropertyGroups.AsNoTracking().AnyAsync(g => g.AssetTypeId == command.AssetTypeId && g.IsInstance == command.IsInstance
                && g.Name == command.Name && g.Deleted == null, cancellationToken))
            errors.Add(new ValidationFailure(nameof(CreatePropertyGroupCommand.Name),
                $"The asset type already has a group named '{command.Name}' at this level."));

        return errors;
    }

    private async Task<List<ValidationFailure>> ValidatePropertiesAsync(CreatePropertyGroupCommand command, CancellationToken cancellationToken)
    {
        var errors = new List<ValidationFailure>();
        if (command.Properties.Count == 0)
            return errors;

        var propertyIds = command.Properties.Select(m => m.PropertyId).ToList();
        var properties = await db.Properties.AsNoTracking()
            .Where(p => propertyIds.Contains(p.Id) && p.Deleted == null)
            .Select(p => new { p.Id, p.Name, p.IsSystem, p.AssetTypeIsInstance })
            .ToDictionaryAsync(p => p.Id, cancellationToken);
        var linked = (await db.AssetTypesProperties.AsNoTracking()
                .Where(l => l.AssetTypeId == command.AssetTypeId && l.IsInstance == command.IsInstance && l.Deleted == null
                    && propertyIds.Contains(l.PropertyId))
                .Select(l => l.PropertyId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        for (var i = 0; i < command.Properties.Count; i++)
        {
            var propertyId = command.Properties[i].PropertyId;
            var field = $"{nameof(CreatePropertyGroupCommand.Properties)}[{i}]";
            if (!properties.TryGetValue(propertyId, out var property))
                errors.Add(new ValidationFailure(field, $"Property {propertyId} does not exist."));
            // System properties are never linked; the instance-of pair only means something on an instance.
            else if (property.IsSystem && property.AssetTypeIsInstance == true && !command.IsInstance)
                errors.Add(new ValidationFailure(field, $"'{property.Name}' belongs to instances only."));
            else if (!property.IsSystem && !linked.Contains(propertyId))
                errors.Add(new ValidationFailure(field, $"'{property.Name}' is not linked to the asset type at this level."));
        }

        return errors;
    }

    private static Failure<Guid> Failed(ResultStatus status, List<ValidationFailure> errors) =>
        new(default, status, errors);
}
