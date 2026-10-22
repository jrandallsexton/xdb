using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Common.Identity;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Roles.Legacy;
using Xdb.Api.Application.Views.Legacy;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Roles.Queries;

public sealed record GetRolesDictionaryQuery(bool IncludeDeleted);

public interface IGetRolesDictionaryQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetRolesDictionaryQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Roles_GetDictionary: roles by name; inactive ones only for an administrator caller (the userId parameter is ignored).</summary>
public sealed class GetRolesDictionaryQueryHandler(IRoleProcedures roles, IMetadataProcedures metadata, CurrentMember currentMember) : IGetRolesDictionaryQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetRolesDictionaryQuery query, CancellationToken cancellationToken = default)
    {
        var caller = await currentMember.GetIdAsync(cancellationToken);
        var isAdmin = await metadata.IsAdminAsync(caller, SystemRoleIds.Admin, cancellationToken);
        return new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await roles.GetRolesDictionaryAsync(query.IncludeDeleted, includeInactive: isAdmin, cancellationToken));
    }
}

public sealed record GetRoleQuery(Guid RoleId);

public interface IGetRoleQueryHandler
{
    Task<Result<RoleContract?>> ExecuteAsync(GetRoleQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Role_Get: the role with members, reports and asset-type permissions; null when absent.</summary>
public sealed class GetRoleQueryHandler(IRoleProcedures roles) : IGetRoleQueryHandler
{
    public async Task<Result<RoleContract?>> ExecuteAsync(GetRoleQuery query, CancellationToken cancellationToken = default) =>
        new Success<RoleContract?>(await roles.GetRoleAsync(query.RoleId, cancellationToken));
}

public sealed record CloneRoleQuery(Guid RoleId, Guid UserId);

public interface ICloneRoleQueryHandler
{
    Task<Result<RoleContract?>> ExecuteAsync(CloneRoleQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Role_Clone: name, description, dates, flags and members (new ids, no names) of the role; asset types and reports
/// are not cloned. The clone's id is the userId argument, as the original passed it.
/// </summary>
public sealed class CloneRoleQueryHandler(IRoleProcedures roles) : ICloneRoleQueryHandler
{
    public async Task<Result<RoleContract?>> ExecuteAsync(CloneRoleQuery query, CancellationToken cancellationToken = default)
    {
        if (await roles.GetRoleAsync(query.RoleId, cancellationToken) is not { } source)
            return new Success<RoleContract?>(null);
        return new Success<RoleContract?>(new RoleContract
        {
            Id = query.UserId,
            Name = source.Name,
            Description = source.Description,
            Created = source.Created,
            CreatedBy = source.CreatedBy,
            IsActive = source.IsActive,
            IsSystem = source.IsSystem,
            Members = [.. source.Members!.Select(m => new RoleMemberContract { Id = Guid.NewGuid(), MemberId = m.MemberId, IsNew = true, IsDirty = true })]
        });
    }
}

public sealed record CloneRoleViewRelationsQuery(Guid SourceRoleId, Guid NewRoleId, Guid UserId);

public interface ICloneRoleViewRelationsQueryHandler
{
    Task<Result<List<RoleViewRelationContract>>> ExecuteAsync(CloneRoleViewRelationsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>RoleViewRelationList_Clone: the source role's live view relations as new relations of the new role.</summary>
public sealed class CloneRoleViewRelationsQueryHandler(IViewProcedures views, IDateTimeProvider dateTimeProvider) : ICloneRoleViewRelationsQueryHandler
{
    public async Task<Result<List<RoleViewRelationContract>>> ExecuteAsync(CloneRoleViewRelationsQuery query, CancellationToken cancellationToken = default)
    {
        var result = new List<RoleViewRelationContract>();
        foreach (var relation in await views.GetRoleViewRelationsAsync(query.SourceRoleId, cancellationToken))
        {
            if (result.Exists(r => r.ViewId == relation.ViewId))
                continue;
            result.Add(new RoleViewRelationContract
            {
                Id = Guid.NewGuid(),
                Created = dateTimeProvider.UtcNow().ToLocalTime(),
                CreatedBy = query.UserId,
                IsAutoApprove = relation.IsAutoApprove ?? false,
                IsDirty = true,
                IsNew = true,
                RoleId = query.NewRoleId,
                ViewId = relation.ViewId
            });
        }
        return new Success<List<RoleViewRelationContract>>(result);
    }
}

public sealed record GetRoleMembersQuery(Guid RoleId, bool InRole);

public interface IGetRoleMembersQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetRoleMembersQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Users_GetForRole / Users_GetNotInRole: members (display values) in or not in the role.</summary>
public sealed class GetRoleMembersQueryHandler(IRoleProcedures roles) : IGetRoleMembersQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetRoleMembersQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await roles.GetMembersAsync(query.RoleId, query.InRole, cancellationToken));
}

public sealed record GetRoleReportsQuery(Guid RoleId, bool InRole);

public interface IGetRoleReportsQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetRoleReportsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Reports_GetForRole / Reports_GetNotInRole.</summary>
public sealed class GetRoleReportsQueryHandler(IRoleProcedures roles) : IGetRoleReportsQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetRoleReportsQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await roles.GetReportsAsync(query.RoleId, query.InRole, cancellationToken));
}

public sealed record GetRoleUserIdsQuery(Guid RoleId);

public interface IGetRoleUserIdsQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetRoleUserIdsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>UserIds_GetByRole: each member's user id (one lookup per member; a repeated member fails).</summary>
public sealed class GetRoleUserIdsQueryHandler(IRoleProcedures roles) : IGetRoleUserIdsQueryHandler
{
    public async Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetRoleUserIdsQuery query, CancellationToken cancellationToken = default)
    {
        var result = new List<KeyValuePair<Guid, string>>();
        foreach (var memberId in await roles.GetMemberIdsAsync(query.RoleId, cancellationToken))
        {
            if (result.Exists(p => p.Key == memberId))
                throw new ArgumentException($"An item with the same key has already been added: {memberId:D}");
            result.Add(new KeyValuePair<Guid, string>(memberId, await roles.GetUserIdAsync(memberId, cancellationToken)));
        }
        return new Success<List<KeyValuePair<Guid, string>>>(result);
    }
}
