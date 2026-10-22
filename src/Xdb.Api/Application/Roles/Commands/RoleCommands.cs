using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Common.Identity;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects.Views;
using Xdb.Api.Application.Roles.Legacy;
using Xdb.Api.Application.Views.Legacy;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Roles.Commands;

public sealed record SaveRoleCommand(RoleContract? Role);

public interface ISaveRoleCommandHandler
{
    Task<Result<bool>> ExecuteAsync(SaveRoleCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Role_Save (RoleLayer.Save): validate, spr_Role_Save, the inline batch for members, reports and asset-type
/// permissions (with its bugs, see <see cref="RoleSaveBatch"/>), then the row-security views from the posted role.
/// Nothing is transactional: a failure leaves the earlier steps committed.
/// </summary>
public sealed class SaveRoleCommandHandler(IRoleProcedures roles, IRoleViewGenerator views, CurrentMember currentMember) : ISaveRoleCommandHandler
{
    public async Task<Result<bool>> ExecuteAsync(SaveRoleCommand command, CancellationToken cancellationToken = default)
    {
        var callerId = await currentMember.GetIdAsync(cancellationToken);
        var role = command.Role ?? throw new NullReferenceException("role is null.");
        if (role.Id == Guid.Empty)
            throw new LegacyLogicalException("Id must be provided.");
        if (string.IsNullOrEmpty(role.Description))
            throw new LegacyLogicalException("Description is required");
        if (string.IsNullOrEmpty(role.Name))
            throw new LegacyLogicalException("Name is required");
        if (role.IsSystem && (role.Deleted.HasValue || role.DeletedBy.HasValue))
            throw new LegacyLogicalException("System roles cannot be deleted.");

        await roles.SaveRoleAsync(role, cancellationToken);
        var batch = RoleSaveBatch.Build(role, callerId);
        if (batch.Length > 0)
            await roles.ExecuteBatchAsync(batch, cancellationToken);

        await views.GenerateAsync(role.Id, PostedAssetTypes(role), cancellationToken);
        return new Success<bool>(true);
    }

    /// <summary>The posted asset types as the view generator reads them (posted order, flags ignored, null filter lists skipped).</summary>
    private static List<RoleAssetTypeRow> PostedAssetTypes(RoleContract role)
    {
        var result = new List<RoleAssetTypeRow>();
        foreach (var assetType in role.AssetTypes!)
        {
            var row = new RoleAssetTypeRow { Id = assetType.Id, AssetTypeId = assetType.AssetTypeId, IsInstance = assetType.IsInstance, IsInherited = assetType.IsInherited };
            if (!assetType.IsInherited)
            {
                foreach (var permission in assetType.Permissions ?? throw new NullReferenceException("Permissions is null."))
                {
                    // A null expression stays null: the original's SQL builder failed on it before dropping the view.
                    var permissionRow = new RolePermissionRow { Id = permission.Id, PermissionId = permission.PermissionId, FilterExpression = permission.FilterExpression! };
                    foreach (var filter in permission.Filters ?? [])
                        permissionRow.Filters.Add(new RoleFilterRow
                        {
                            PropertyId = filter.PropertyId, OperatorId = filter.OperatorId, Operator = filter.Operator ?? string.Empty, Value = filter.Value ?? string.Empty
                        });
                    row.Permissions.Add(permissionRow);
                }
            }
            result.Add(row);
        }
        return result;
    }
}

public sealed record SaveRoleViewRelationsCommand(List<RoleViewRelationContract>? Relations);

public interface ISaveRoleViewRelationsCommandHandler
{
    Task<Result<bool>> ExecuteAsync(SaveRoleViewRelationsCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Role_SaveViewRelations: each dirty relation saved in order (update or insert by id), no transaction. A null list
/// (the admin page posts "null" on every save) fails, as in the original.
/// </summary>
public sealed class SaveRoleViewRelationsCommandHandler(IRoleProcedures roles) : ISaveRoleViewRelationsCommandHandler
{
    public async Task<Result<bool>> ExecuteAsync(SaveRoleViewRelationsCommand command, CancellationToken cancellationToken = default)
    {
        foreach (var relation in command.Relations ?? throw new NullReferenceException("relations is null."))
        {
            if (relation.IsDirty)
                await roles.SaveViewRelationAsync(relation, cancellationToken);
        }
        return new Success<bool>(true);
    }
}

public sealed record SaveRoleProcessRelationsCommand(List<RoleProcessRelationContract>? Relations);

public interface ISaveRoleProcessRelationsCommandHandler
{
    Task<Result<bool>> ExecuteAsync(SaveRoleProcessRelationsCommand command, CancellationToken cancellationToken = default);
}

/// <summary>Role_SaveProcessRelations: each dirty relation saved in order; a null list fails.</summary>
public sealed class SaveRoleProcessRelationsCommandHandler(IRoleProcedures roles) : ISaveRoleProcessRelationsCommandHandler
{
    public async Task<Result<bool>> ExecuteAsync(SaveRoleProcessRelationsCommand command, CancellationToken cancellationToken = default)
    {
        foreach (var relation in command.Relations ?? throw new NullReferenceException("relations is null."))
        {
            if (relation.IsDirty)
                await roles.SaveProcessRelationAsync(relation, cancellationToken);
        }
        return new Success<bool>(true);
    }
}

public sealed record KillRoleCommand(Guid RoleId, Guid UserId);

public interface IKillRoleCommandHandler
{
    Task<Result<bool>> ExecuteAsync(KillRoleCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Role_Kill: an administrator (the query userId, exactly one live Admin row) hard-deletes a non-system role through
/// spr_Role_Kill, which clears only the older relation tables (the newer ones and the role's views stay).
/// </summary>
public sealed class KillRoleCommandHandler(IRoleProcedures roles) : IKillRoleCommandHandler
{
    public async Task<Result<bool>> ExecuteAsync(KillRoleCommand command, CancellationToken cancellationToken = default)
    {
        if (!await roles.ContainsMemberAsync(command.UserId, SystemRoleIds.Admin, cancellationToken))
            throw new LegacyLogicalException("No permissions to kill objects");
        var role = await roles.GetRoleAsync(command.RoleId, cancellationToken) ?? throw new LegacyLogicalException("Invalid Role Id");
        if (role.IsSystem)
            throw new LegacyLogicalException("System roles cannot be destroyed.");
        await roles.KillRoleAsync(command.RoleId, cancellationToken);
        return new Success<bool>(true);
    }
}
