using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Views.Queries;

// Lists the original read straight from one query; ordering, filtering and failure rules live in the SQL.

public sealed record GetPropertyGroupViewsQuery(Guid PropertyGroupId);

public interface IGetPropertyGroupViewsQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetPropertyGroupViewsQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetPropertyGroupViewsQueryHandler(IViewProcedures views) : IGetPropertyGroupViewsQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetPropertyGroupViewsQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await views.GetViewsByPropertyGroupAsync(query.PropertyGroupId, cancellationToken));
}

public sealed record GetViewsDictionaryQuery(bool IncludeDeleted, bool IncludeNonStandAlone);

public interface IGetViewsDictionaryQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetViewsDictionaryQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Views as "Name (DisplayValue)" by name.</summary>
public sealed class GetViewsDictionaryQueryHandler(IViewProcedures views) : IGetViewsDictionaryQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetViewsDictionaryQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await views.GetViewsDictionaryAsync(query.IncludeDeleted, query.IncludeNonStandAlone, cancellationToken));
}

public sealed record GetMemberFiltersQuery(Guid ViewId, Guid MemberId);

public interface IGetMemberFiltersQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetMemberFiltersQuery query, CancellationToken cancellationToken = default);
}

/// <summary>ViewFilters_Get: the member's saved quick filters for a view, by name.</summary>
public sealed class GetMemberFiltersQueryHandler(IViewProcedures views) : IGetMemberFiltersQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetMemberFiltersQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await views.GetMemberFiltersAsync(query.ViewId, query.MemberId, cancellationToken));
}

public sealed record GetUserViewsQuery(Guid UserId);

public interface IGetUserViewsQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetUserViewsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Views_Get / Views_GetForUser: stand-alone views reachable through the member's active roles (RoleMembers).</summary>
public sealed class GetUserViewsQueryHandler(IViewProcedures views) : IGetUserViewsQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetUserViewsQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await views.GetUserViewsAsync(query.UserId, cancellationToken));
}

public sealed record GetUserViewGroupsQuery(Guid UserId);

public interface IGetUserViewGroupsQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetUserViewGroupsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>ViewGroups_Get: (asset type id, pluralization) through RolesMembers, a different table from Views_Get's.</summary>
public sealed class GetUserViewGroupsQueryHandler(IViewProcedures views) : IGetUserViewGroupsQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetUserViewGroupsQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await views.GetUserViewGroupsAsync(query.UserId, cancellationToken));
}

public sealed record GetViewRolesQuery(Guid ViewId);

public interface IGetViewRolesQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetViewRolesQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Roles_GetByViewId: roles linked to a live view (inactive roles included), by name.</summary>
public sealed class GetViewRolesQueryHandler(IViewProcedures views) : IGetViewRolesQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetViewRolesQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await views.GetRolesByViewAsync(query.ViewId, cancellationToken));
}

public sealed record GetMemberRolesQuery(Guid MemberId);

public interface IGetMemberRolesQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetMemberRolesQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Roles_GetForUser: the member's active roles; a duplicate live membership fails, as in the original.</summary>
public sealed class GetMemberRolesQueryHandler(IViewProcedures views) : IGetMemberRolesQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetMemberRolesQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await views.GetMemberRolesAsync(query.MemberId, cancellationToken));
}

public sealed record GetPropertyGroupsQuery(bool IncludeDeleted);

public interface IGetPropertyGroupsQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetPropertyGroupsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>PropertyGroups_GetDictionary: groups as "Name (DisplayValue)" by name.</summary>
public sealed class GetPropertyGroupsQueryHandler(IViewProcedures views) : IGetPropertyGroupsQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetPropertyGroupsQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await views.GetPropertyGroupsDictionaryAsync(query.IncludeDeleted, null, cancellationToken));
}

public sealed record GetPropertyGroupsForPropertyQuery(Guid PropertyId);

public interface IGetPropertyGroupsForPropertyQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetPropertyGroupsForPropertyQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetPropertyGroupsForPropertyQueryHandler(IViewProcedures views) : IGetPropertyGroupsForPropertyQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetPropertyGroupsForPropertyQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await views.GetPropertyGroupsByPropertyAsync(query.PropertyId, cancellationToken));
}

public sealed record GetAssetTypePropertyGroupsQuery(Guid AssetTypeId);

public interface IGetAssetTypePropertyGroupsQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetTypePropertyGroupsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// PropertyGroups_GetByAssetTypeId: with a parent, the nearest parent's groups, then each further ancestor's (a group
/// repeated across ancestors fails, as Dictionary.Add did), then the type's own groups not already present.
/// </summary>
public sealed class GetAssetTypePropertyGroupsQueryHandler(IViewProcedures views, IMetadataProcedures metadata) : IGetAssetTypePropertyGroupsQueryHandler
{
    public async Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetTypePropertyGroupsQuery query, CancellationToken cancellationToken = default)
    {
        var ancestor = await metadata.GetParentAssetTypeIdAsync(query.AssetTypeId, cancellationToken);
        if (ancestor is null)
            return new Success<List<KeyValuePair<Guid, string>>>([.. await views.GetPropertyGroupsDictionaryAsync(false, query.AssetTypeId, cancellationToken)]);

        var result = new List<KeyValuePair<Guid, string>>();
        var ids = new HashSet<Guid>();
        while (ancestor is { } id)
        {
            foreach (var group in await views.GetPropertyGroupsDictionaryAsync(false, id, cancellationToken))
            {
                if (!ids.Add(group.Key))
                    throw new ArgumentException($"An item with the same key has already been added: {group.Key:D}");
                result.Add(group);
            }
            ancestor = await metadata.GetParentAssetTypeIdAsync(id, cancellationToken);
        }
        foreach (var group in await views.GetPropertyGroupsDictionaryAsync(false, query.AssetTypeId, cancellationToken))
        {
            if (ids.Add(group.Key))
                result.Add(group);
        }
        return new Success<List<KeyValuePair<Guid, string>>>(result);
    }
}

public sealed record GetProcessesQuery;

public interface IGetProcessesQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetProcessesQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetProcessesQueryHandler(IViewProcedures views) : IGetProcessesQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetProcessesQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await views.GetProcessesDictionaryAsync(cancellationToken));
}

public sealed record GetAssetTypeProcessesQuery(Guid AssetTypeId, AssetRequestType RequestType);

public interface IGetAssetTypeProcessesQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetTypeProcessesQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Processes with a step on a live view of exactly that type (instance views only for Instance).</summary>
public sealed class GetAssetTypeProcessesQueryHandler(IViewProcedures views) : IGetAssetTypeProcessesQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetTypeProcessesQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(
            await views.GetProcessesByAssetTypeAsync(query.AssetTypeId, query.RequestType == AssetRequestType.Instance, cancellationToken));
}

public sealed record GetHelpSectionsDictionaryQuery;

public interface IGetHelpSectionsDictionaryQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetHelpSectionsDictionaryQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetHelpSectionsDictionaryQueryHandler(IViewProcedures views) : IGetHelpSectionsDictionaryQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetHelpSectionsDictionaryQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await views.GetHelpSectionsDictionaryAsync(cancellationToken));
}
