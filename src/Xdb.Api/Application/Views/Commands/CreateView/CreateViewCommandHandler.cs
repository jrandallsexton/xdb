using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentValidation;
using FluentValidation.Results;

using Microsoft.EntityFrameworkCore;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Infrastructure.Data;
using Xdb.Api.Infrastructure.Data.Entities;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Views.Commands.CreateView;

public interface ICreateViewCommandHandler
{
    /// <summary>The new view's id (Created); Conflict when the id is taken; Validation for a bad command.</summary>
    Task<Result<Guid>> ExecuteAsync(CreateViewCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Replaces the create half of the legacy View_Save (ViewSaver with spr_View_Save, spr_ViewPropertyGroupRelation_Save
/// and a role relation for Admin). The view, its groups and its roles are written together, and the type's default
/// view is set in the same save when asked; the original wrote them one statement at a time and set defaults
/// elsewhere. Here every group must belong to the view's type and level, and names are unique per type. Not carried
/// over: filters and filter logic (unused, and the original's insert always failed), the confirmation and reporting
/// labels and the icon (never read), and per-role auto-approval (stored but ignored by the original).
/// </summary>
public sealed class CreateViewCommandHandler(
    XdbDataContext db,
    IValidator<CreateViewCommand> validator,
    IDateTimeProvider clock) : ICreateViewCommandHandler
{
    public async Task<Result<Guid>> ExecuteAsync(CreateViewCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Failed(ResultStatus.Validation, validation.Errors);

        var errors = await ValidateViewAsync(command, cancellationToken);
        errors.AddRange(await ValidateGroupsAsync(command, cancellationToken));
        errors.AddRange(await ValidateRolesAsync(command, cancellationToken));
        if (errors.Count > 0)
            return Failed(ResultStatus.Validation, errors);

        var id = command.Id ?? Guid.NewGuid();
        if (await db.Views.AsNoTracking().AnyAsync(v => v.Id == id, cancellationToken))
            return Failed(ResultStatus.Conflict, [new ValidationFailure(nameof(CreateViewCommand.Id), $"View {id} already exists.")]);

        var now = clock.UtcNow();
        db.Views.Add(new View
        {
            Id = id,
            AssetTypeId = command.AssetTypeId,
            IsInstance = command.IsInstance,
            Name = command.Name,
            DisplayValue = command.DisplayValue ?? command.Name,
            Description = command.Description ?? string.Empty,
            DriverCaption = command.DriverCaption,
            NewItemCaption = command.NewItemCaption,
            IsReadOnly = command.IsReadOnly,
            IsStandAlone = command.IsStandAlone,
            IsCreation = command.IsCreation,
            Created = now,
            CreatedBy = command.ActorId
        });
        for (var i = 0; i < command.PropertyGroupIds.Count; i++)
        {
            db.ViewsPropertyGroups.Add(new ViewsPropertyGroup
            {
                Id = Guid.NewGuid(), ViewId = id, PropertyGroupId = command.PropertyGroupIds[i], Order = i, Created = now, CreatedBy = command.ActorId
            });
        }
        // Without the Admin role nobody could reach the view to manage it.
        foreach (var roleId in command.RoleIds.Append(SystemRoleIds.Admin).Distinct())
            db.RolesViews.Add(new RolesView { Id = Guid.NewGuid(), RoleId = roleId, ViewId = id, Created = now, CreatedBy = command.ActorId });

        if (command.IsDefault)
        {
            var type = await db.AssetTypes.SingleAsync(t => t.Id == command.AssetTypeId, cancellationToken);
            if (command.IsInstance)
                type.DefaultInstanceViewId = id;
            else
                type.DefaultViewId = id;
            type.LastModified = now;
            type.LastModifiedBy = command.ActorId;
        }
        await db.SaveChangesAsync(cancellationToken);

        return new Success<Guid>(id, ResultStatus.Created);
    }

    private async Task<List<ValidationFailure>> ValidateViewAsync(CreateViewCommand command, CancellationToken cancellationToken)
    {
        var errors = new List<ValidationFailure>();

        if (!await db.Members.AsNoTracking().AnyAsync(m => m.Id == command.ActorId && m.Deleted == null, cancellationToken))
            errors.Add(new ValidationFailure(nameof(CreateViewCommand.ActorId), "The actor is not a live member."));

        var type = await db.AssetTypes.AsNoTracking()
            .Where(t => t.Id == command.AssetTypeId && t.Deleted == null)
            .Select(t => new { t.InstanceLabel })
            .SingleOrDefaultAsync(cancellationToken);
        if (type is null)
            errors.Add(new ValidationFailure(nameof(CreateViewCommand.AssetTypeId), $"Asset type {command.AssetTypeId} does not exist."));
        else if (command.IsInstance && string.IsNullOrEmpty(type.InstanceLabel))
            errors.Add(new ValidationFailure(nameof(CreateViewCommand.IsInstance), "The asset type has no instances."));

        if (await db.Views.AsNoTracking().AnyAsync(v => v.AssetTypeId == command.AssetTypeId && v.Name == command.Name && v.Deleted == null, cancellationToken))
            errors.Add(new ValidationFailure(nameof(CreateViewCommand.Name), $"The asset type already has a view named '{command.Name}'."));

        return errors;
    }

    private async Task<List<ValidationFailure>> ValidateGroupsAsync(CreateViewCommand command, CancellationToken cancellationToken)
    {
        var errors = new List<ValidationFailure>();
        if (command.PropertyGroupIds.Count == 0)
            return errors;

        var ids = command.PropertyGroupIds.ToList();
        var groups = await db.PropertyGroups.AsNoTracking()
            .Where(g => ids.Contains(g.Id) && g.Deleted == null)
            .Select(g => new { g.Id, g.Name, g.AssetTypeId, g.IsInstance })
            .ToDictionaryAsync(g => g.Id, cancellationToken);
        for (var i = 0; i < ids.Count; i++)
        {
            var field = $"{nameof(CreateViewCommand.PropertyGroupIds)}[{i}]";
            if (!groups.TryGetValue(ids[i], out var group))
                errors.Add(new ValidationFailure(field, $"Property group {ids[i]} does not exist."));
            else if (group.AssetTypeId != command.AssetTypeId || group.IsInstance != command.IsInstance)
                errors.Add(new ValidationFailure(field, $"'{group.Name}' belongs to another asset type or level."));
        }

        return errors;
    }

    private async Task<List<ValidationFailure>> ValidateRolesAsync(CreateViewCommand command, CancellationToken cancellationToken)
    {
        var errors = new List<ValidationFailure>();
        if (command.RoleIds.Count == 0)
            return errors;

        var ids = command.RoleIds.ToList();
        var live = (await db.Roles.AsNoTracking().Where(r => ids.Contains(r.Id) && r.Deleted == null).Select(r => r.Id).ToListAsync(cancellationToken)).ToHashSet();
        for (var i = 0; i < ids.Count; i++)
            if (!live.Contains(ids[i]))
                errors.Add(new ValidationFailure($"{nameof(CreateViewCommand.RoleIds)}[{i}]", $"Role {ids[i]} does not exist."));

        return errors;
    }

    private static Failure<Guid> Failed(ResultStatus status, List<ValidationFailure> errors) =>
        new(default, status, errors);
}
