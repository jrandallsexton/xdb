using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Views.Legacy;
using Xdb.Api.Infrastructure.Background;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;
using Xdb.Core.LegacyWire;

namespace Xdb.Api.Application.Views.Queries;

public sealed record GetViewClientQuery(Guid ViewId, Guid UserId);

public interface IGetViewClientQueryHandler
{
    /// <returns>The response body (the typed serializer's bytes).</returns>
    Task<Result<byte[]>> ExecuteAsync(GetViewClientQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// ViewClient_Get: the cached client view for (view, user), else built and cached in the background after responding.
/// A hit returns the stored snapshot even if the view changed since. Rows written by the original are BinaryFormatter
/// blobs and are decoded; rows written by the port hold the response bytes (recorded deviation: the original service
/// cannot read them).
/// </summary>
public sealed class GetViewClientQueryHandler(IViewProcedures views, ViewLoader loader, IBackgroundWorkQueue background) : IGetViewClientQueryHandler
{
    private const byte JsonMarker = (byte)'{';

    public async Task<Result<byte[]>> ExecuteAsync(GetViewClientQuery query, CancellationToken cancellationToken = default)
    {
        if (await views.GetViewClientCacheAsync(query.ViewId, query.UserId, cancellationToken) is { Length: > 0 } cached)
            return new Success<byte[]>(cached[0] == JsonMarker ? cached : LegacyWire.TypedBytes(NrbfViewClientReader.Read(cached)));

        var client = await loader.ViewClientAsync(query.ViewId, query.UserId, cancellationToken);
        var body = LegacyWire.TypedBytes(client);
        var (viewId, userId) = (client.Id, query.UserId);
        background.Enqueue($"view client cache {viewId}/{userId}", (services, ct) =>
            services.GetRequiredService<IViewProcedures>().InsertViewClientCacheAsync(viewId, userId, body, ct));
        return new Success<byte[]>(body);
    }
}

public sealed record ViewClientModifiedQuery(Guid ViewId, string? CheckDateTime);

public interface IViewClientModifiedQueryHandler
{
    Task<Result<bool>> ExecuteAsync(ViewClientModifiedQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Whether the view's LastModified differs from the client's en-US text. An unparseable text means "modified" when the
/// view has a LastModified; a view without one (or an unknown view) is never modified.
/// </summary>
public sealed class ViewClientModifiedQueryHandler(IViewProcedures views) : IViewClientModifiedQueryHandler
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    public async Task<Result<bool>> ExecuteAsync(ViewClientModifiedQuery query, CancellationToken cancellationToken = default)
    {
        var lastModified = await views.GetViewLastModifiedAsync(query.ViewId, cancellationToken);
        if (DateTime.TryParse(query.CheckDateTime, EnUs, DateTimeStyles.None, out var check))
            return new Success<bool>(lastModified.HasValue && lastModified.Value.CompareTo(check) != 0);
        return new Success<bool>(lastModified.HasValue);
    }
}

public sealed record GetViewQuery(Guid ViewId, Guid UserId);

public interface IGetViewQueryHandler
{
    Task<Result<ViewContract>> ExecuteAsync(GetViewQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetViewQueryHandler(ViewLoader loader) : IGetViewQueryHandler
{
    public async Task<Result<ViewContract>> ExecuteAsync(GetViewQuery query, CancellationToken cancellationToken = default) =>
        new Success<ViewContract>(await loader.ViewAsync(query.ViewId, query.UserId, cancellationToken));
}

public sealed record GetViewsByAssetTypeQuery(Guid AssetTypeId, AssetRequestType RequestType, bool IncludeNonStandAlone);

public interface IGetViewsByAssetTypeQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetViewsByAssetTypeQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// The type's views with its ancestors' views. With a parent: the ancestors' views (nearest first), then the type's own
/// views whose display name is not already present (names compared ordinally, case-sensitive).
/// </summary>
public sealed class GetViewsByAssetTypeQueryHandler(IViewProcedures views, IMetadataProcedures metadata) : IGetViewsByAssetTypeQueryHandler
{
    public async Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetViewsByAssetTypeQuery query, CancellationToken cancellationToken = default)
    {
        var parent = await metadata.GetParentAssetTypeIdAsync(query.AssetTypeId, cancellationToken);
        if (parent is null)
            return new Success<List<KeyValuePair<Guid, string>>>(await WithAncestorsAsync(query.AssetTypeId, query, cancellationToken));

        var byName = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var ordered = new List<KeyValuePair<Guid, string>>();
        foreach (var (id, name) in await WithAncestorsAsync(parent.Value, query, cancellationToken))
        {
            byName.Add(name, id);
            ordered.Add(new KeyValuePair<Guid, string>(id, name));
        }
        foreach (var (id, name) in await WithAncestorsAsync(query.AssetTypeId, query, cancellationToken))
        {
            if (byName.TryAdd(name, id))
                ordered.Add(new KeyValuePair<Guid, string>(id, name));
        }
        return new Success<List<KeyValuePair<Guid, string>>>(ordered);
    }

    /// <summary>ViewLayer.GetDictionaryByAssetTypeId: the type's views, then each ancestor's views not already present (by id).</summary>
    private async Task<List<KeyValuePair<Guid, string>>> WithAncestorsAsync(Guid assetTypeId, GetViewsByAssetTypeQuery query, CancellationToken cancellationToken)
    {
        var result = (await views.GetViewsByAssetTypeAsync(assetTypeId, (int)query.RequestType, query.IncludeNonStandAlone, cancellationToken)).ToList();
        var ids = new HashSet<Guid>(result.Select(r => r.Key));
        var ancestor = await metadata.GetParentAssetTypeIdAsync(assetTypeId, cancellationToken);
        while (ancestor is { } id)
        {
            foreach (var view in await views.GetViewsByAssetTypeAsync(id, (int)query.RequestType, query.IncludeNonStandAlone, cancellationToken))
            {
                if (ids.Add(view.Key))
                    result.Add(view);
            }
            ancestor = await metadata.GetParentAssetTypeIdAsync(id, cancellationToken);
        }
        return result;
    }
}

public sealed record GetViewFilterDisplayQuery(Guid MemberViewId);

public interface IGetViewFilterDisplayQueryHandler
{
    Task<Result<string?>> ExecuteAsync(GetViewFilterDisplayQuery query, CancellationToken cancellationToken = default);
}

/// <summary>A saved quick filter's readable text; "" when absent, null when its text is NULL.</summary>
public sealed class GetViewFilterDisplayQueryHandler(IViewProcedures views) : IGetViewFilterDisplayQueryHandler
{
    public async Task<Result<string?>> ExecuteAsync(GetViewFilterDisplayQuery query, CancellationToken cancellationToken = default) =>
        new Success<string?>(await views.GetMemberViewAsync(query.MemberViewId, cancellationToken) is { } row ? row.FilterString : string.Empty);
}

/// <summary>The filters of an asset search client, as the UI posts them.</summary>
public sealed record SearchFilter(Guid PropertyId, int OperatorId, string Value);

public sealed record GetSearchFilterDisplayQuery(string? FilterExpression, IReadOnlyList<SearchFilter> Filters);

public interface IGetSearchFilterDisplayQueryHandler
{
    Task<Result<string>> ExecuteAsync(GetSearchFilterDisplayQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// SqlDatabaseLayer.FilterString over posted filters: each rendered as "{property} = {value}" (the operator text is
/// always "=" here), values decoded by the property's type, formatted into the filter expression.
/// </summary>
public sealed class GetSearchFilterDisplayQueryHandler(IMetadataProcedures metadata, IReportProcedures reports) : IGetSearchFilterDisplayQueryHandler
{
    private const string EmptyGuid = "00000000-0000-0000-0000-000000000000";

    public async Task<Result<string>> ExecuteAsync(GetSearchFilterDisplayQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Filters.Count == 0)
            return new Success<string>(string.Empty);

        var properties = (await metadata.GetPropertiesAsync(query.Filters.Select(f => f.PropertyId).Distinct().ToList(), cancellationToken)).ToDictionary(p => p.Id);
        var items = new List<object>();
        foreach (var filter in query.Filters)
        {
            var property = properties[filter.PropertyId];
            var name = await metadata.GetPropertyDisplayValueAsync(filter.PropertyId, cancellationToken);
            var value = filter.Value;
            switch ((int?)property.DataTypeId ?? -1)
            {
                case (int)LegacyDataType.Date:
                    if (filter.Value != "NULL" && property.IsSystem != true && Guid.TryParse(filter.Value, out var datePart)
                        && await reports.GetDatePartDisplayValueAsync(datePart, cancellationToken) is { } datePartText)
                        value = datePartText;
                    break;
                case (int)LegacyDataType.Asset:
                    value = await reports.GetAssetNameAsync(new Guid(filter.Value), cancellationToken);
                    break;
                case (int)LegacyDataType.PickList:
                    var pickListValue = await reports.GetPickListValueAsync(new Guid(filter.Value), cancellationToken)
                        ?? throw new NullReferenceException("Pick list value not found.");
                    value = string.IsNullOrEmpty(pickListValue.DisplayValue) ? pickListValue.Value : pickListValue.DisplayValue;
                    break;
                case (int)LegacyDataType.User:
                    value = await MemberDisplayValueAsync(new Guid(filter.Value), cancellationToken);
                    break;
            }
            if (filter.Value == EmptyGuid)
                value = "null";
            items.Add($"{name} = {value}");
        }
        return new Success<string>(string.Format(CultureInfo.InvariantCulture, query.FilterExpression!, [.. items]));
    }

    private async Task<string> MemberDisplayValueAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var member = await reports.GetMemberAsync(memberId, cancellationToken);
        if (member is null)
            return string.Empty;
        if (string.IsNullOrEmpty(member.FName) && string.IsNullOrEmpty(member.LName))
            return member.UserId;
        return $"{member.LName}, {member.FName} [{member.UserId}]";
    }
}

public sealed record ViewMatchesAssetTypeQuery(Guid ViewId, Guid AssetTypeId, AssetRequestType RequestType);

public interface IViewMatchesAssetTypeQueryHandler
{
    Task<Result<bool>> ExecuteAsync(ViewMatchesAssetTypeQuery query, CancellationToken cancellationToken = default);
}

/// <summary>ViewAndAssetTypeAreCompatible: a live view of that type, and an instance view only for instance requests.</summary>
public sealed class ViewMatchesAssetTypeQueryHandler(IViewProcedures views) : IViewMatchesAssetTypeQueryHandler
{
    public async Task<Result<bool>> ExecuteAsync(ViewMatchesAssetTypeQuery query, CancellationToken cancellationToken = default)
    {
        if (!await views.IsLiveViewAsync(query.ViewId, cancellationToken))
            throw new ViewLoader.LogicalException("Invalid view id");
        if (await views.GetViewAssetTypeIdAsync(query.ViewId, cancellationToken) != query.AssetTypeId)
            return new Success<bool>(false);
        if (await views.IsLiveInstanceViewAsync(query.ViewId, cancellationToken) && query.RequestType != AssetRequestType.Instance)
            return new Success<bool>(false);
        return new Success<bool>(true);
    }
}

public sealed record GetRoleViewsQuery(Guid RoleId);

public interface IGetRoleViewsQueryHandler
{
    Task<Result<List<RoleViewRelationContract>>> ExecuteAsync(GetRoleViewsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>A role's live view relations (no ORDER BY), the first row per view.</summary>
public sealed class GetRoleViewsQueryHandler(IViewProcedures views) : IGetRoleViewsQueryHandler
{
    public async Task<Result<List<RoleViewRelationContract>>> ExecuteAsync(GetRoleViewsQuery query, CancellationToken cancellationToken = default)
    {
        var result = new List<RoleViewRelationContract>();
        foreach (var row in await views.GetRoleViewRelationsAsync(query.RoleId, cancellationToken))
        {
            if (result.Any(r => r.ViewId == row.ViewId))
                continue;
            result.Add(new RoleViewRelationContract
            {
                Id = row.Id, RoleId = row.RoleId, ViewId = row.ViewId, IsAutoApprove = row.IsAutoApprove ?? false,
                Created = row.Created ?? default, CreatedBy = row.CreatedBy, DeletedBy = row.DeletedBy
            });
        }
        return new Success<List<RoleViewRelationContract>>(result);
    }
}
