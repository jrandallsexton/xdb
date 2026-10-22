using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects;
using Xdb.Api.Application.Metadata.Queries.Lookups;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Assets.Queries;

/// <summary>UILayer.RelatedItems_Get(assetId, userId, ...): an administrator sees every related item; others only types their roles cover.</summary>
public sealed class RelatedItems(IUiProcedures procedures, IAssetProcedures assets, IMetadataProcedures metadata)
{
    public async Task<List<RelatedItemRow>> ForAssetAsync(Guid assetId, Guid userId, bool includeDependencies, CancellationToken cancellationToken)
    {
        var isInstance = await assets.IsInstanceAsync(assetId, cancellationToken);
        Guid? member = await metadata.IsAdminAsync(userId, SystemRoleIds.Admin, cancellationToken) ? null : userId;
        return await procedures.GetRelatedItemsAsync(assetId, member, isInstance, includeDependencies, cancellationToken);
    }
}

public sealed record GetRelatedItemsHtmlQuery(Guid UserId, Guid AssetId, string? PropertyValue, string? ImageSource);

public interface IGetRelatedItemsHtmlQueryHandler
{
    Task<Result<List<string>>> ExecuteAsync(GetRelatedItemsHtmlQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// RelatedItems_GetHtml: the related items (by asset, or by raw value when the asset id is empty) as accordion HTML
/// lines, one section per (property, asset type) in first-seen order. Names are not HTML-escaped.
/// </summary>
public sealed class GetRelatedItemsHtmlQueryHandler(RelatedItems related, IUiProcedures procedures, IOptions<LegacyEnvironmentOptions> environment)
    : IGetRelatedItemsHtmlQueryHandler
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    public async Task<Result<List<string>>> ExecuteAsync(GetRelatedItemsHtmlQuery query, CancellationToken cancellationToken = default)
    {
        var lines = new List<string> { "<div id=\"accordion\">" };
        var serverUrl = environment.Value.CombinedWebServerUrl();
        var items = query.AssetId == Guid.Empty
            ? await procedures.GetRelatedItemsByValueAsync(query.PropertyValue, cancellationToken)
            : await related.ForAssetAsync(query.AssetId, query.UserId, includeDependencies: false, cancellationToken);

        var plurals = new Dictionary<string, string?>();
        var headers = new List<string>();
        var sections = new Dictionary<int, List<string>>();
        var seen = new List<string>();

        // (property name -> (singular -> count)), both in first-seen order; keys ordinal, null keys fail.
        var groups = new List<(string Property, List<(string Singular, int Count)> Types)>();
        foreach (var item in items)
        {
            var property = item.PropertyName ?? throw new ArgumentNullException(nameof(query), "PropertyName is null.");
            var singular = item.AssetTypeSingular ?? throw new ArgumentNullException(nameof(query), "AssetTypeSingular is null.");
            var group = groups.FindIndex(g => g.Property == property);
            if (group < 0)
            {
                groups.Add((property, [(singular, 1)]));
            }
            else
            {
                var types = groups[group].Types;
                var type = types.FindIndex(t => t.Singular == singular);
                if (type < 0)
                    types.Add((singular, 1));
                else
                    types[type] = (singular, types[type].Count + 1);
            }
            plurals.TryAdd(singular, item.AssetTypePlural);
        }

        foreach (var (property, types) in groups)
        {
            foreach (var (singular, count) in types)
            {
                if (!seen.Contains(property + singular))
                {
                    var iconClass = "at-" + (await procedures.GetAssetTypeIdByNameAsync(singular, cancellationToken)).ToString("N").ToLower(EnUs) + "-24";
                    string header, word;
                    if (singular.ToLower(EnUs) == "role")
                    {
                        header = $"<h4><a href=\"#\"><img width='24' height='24' style='margin-right:5px;vertical-align:middle;border:0;' class='mnuAdminRoles' src='{query.ImageSource}/trans.png'/>";
                        word = "of";
                    }
                    else
                    {
                        header = $"<h4><a href=\"#\"><img width='24' height='24' style='margin-right:5px;vertical-align:middle;border:0' class='{iconClass}' src='{query.ImageSource}/trans.png'/>";
                        word = "for";
                    }
                    if (count > 1)
                    {
                        var plural = plurals[singular] ?? throw new NullReferenceException("Asset type plural is null.");
                        header += $"{property} {word} {count} {plural.ToLower(EnUs)}</a></h4>";
                    }
                    else
                    {
                        var noun = singular == "Software" ? "software application" : singular.ToLower(EnUs);
                        header += $"{property} {word} {count} {noun}</a></h4>";
                    }
                    headers.Add(header);
                    seen.Add(property + singular);
                }

                foreach (var item in items)
                {
                    if (item.PropertyName != property || item.AssetTypeSingular != singular)
                        continue;
                    if (!string.IsNullOrEmpty(item.InstanceOfName))
                    {
                        item.AssetName += string.IsNullOrEmpty(item.InstanceOfDesc)
                            ? $" ({item.InstanceOfName})"
                            : $" ({item.InstanceOfName} - {item.InstanceOfDesc})";
                    }
                    var line = $"<li><a href=\"{serverUrl}/view.html?viewId={item.ViewId:D}&assetId={item.AssetId:D}\">{item.AssetName}</a></li>";
                    if (sections.TryGetValue(headers.Count, out var section))
                        section.Add(line);
                    else
                        sections.Add(headers.Count, [line]);
                }
            }
        }

        if (items.Count > 0)
        {
            for (var i = 0; i < headers.Count; i++)
            {
                lines.Add(headers[i]);
                lines.Add("<div><ul>");
                lines.AddRange(sections[i + 1]);
                lines.Add("</ul></div>");
            }
        }
        else
        {
            lines.Add("<div><ul>");
            lines.Add("<label><i>No related items found</i></label>");
            lines.Add("</ul></div>");
        }
        lines.Add("</div>");
        return new Success<List<string>>(lines);
    }
}

public sealed record GetRelationGraphQuery(string? AssetId);

public interface IGetRelationGraphQueryHandler
{
    Task<Result<RGraphDataContract>> ExecuteAsync(GetRelationGraphQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// RGraph: the asset, its related items, theirs, and a third level that (as in the original) repeats the first-level
/// item's relations under every second-level node. Read as the configured member; names are raw (no instance suffix).
/// </summary>
public sealed class GetRelationGraphQueryHandler(RelatedItems related, IAssetProcedures assets, IOptions<LegacyEnvironmentOptions> environment)
    : IGetRelationGraphQueryHandler
{
    public async Task<Result<RGraphDataContract>> ExecuteAsync(GetRelationGraphQuery query, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(query.AssetId, out var assetId))
            throw new InvalidOperationException("Invalid Asset Id");
        var member = environment.Value.RGraphMemberId;
        var name = await assets.GetAssetNameTextAsync(assetId, cancellationToken);
        var root = Node(assetId, name);
        foreach (var item in await related.ForAssetAsync(assetId, member, includeDependencies: true, cancellationToken))
        {
            var node = Node(item.AssetId, item.AssetName);
            var source = item.InstanceOfId != Guid.Empty ? item.InstanceOfId : item.AssetId;
            foreach (var item2 in await related.ForAssetAsync(source, member, includeDependencies: true, cancellationToken))
            {
                var node2 = Node(item2.AssetId, item2.AssetName);
                foreach (var item3 in await related.ForAssetAsync(item.AssetId, member, includeDependencies: true, cancellationToken))
                    node2.Children.Add(Node(item3.AssetId, item3.AssetName));
                node.Children.Add(node2);
            }
            root.Children.Add(node);
        }
        return new Success<RGraphDataContract>(root);
    }

    private static RGraphDataContract Node(Guid id, string? name) => new() { Id = id.ToString("D"), Name = name };
}

public sealed record GetAssetHitsQuery(Guid AssetTypeId, AssetRequestType RequestType);

public interface IGetAssetHitsQueryHandler
{
    Task<Result<List<AssetHitContract>>> ExecuteAsync(GetAssetHitsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>AssetHits_Get: the type's assets by hits, or (when it has none of the kind) its descendants' and its own, concatenated.</summary>
public sealed class GetAssetHitsQueryHandler(IUiProcedures procedures, MetadataRules rules) : IGetAssetHitsQueryHandler
{
    public async Task<Result<List<AssetHitContract>>> ExecuteAsync(GetAssetHitsQuery query, CancellationToken cancellationToken = default)
    {
        var types = await rules.HasAssetsAsync(query.AssetTypeId, query.RequestType, searchChildAssetTypes: false, cancellationToken)
            ? [query.AssetTypeId]
            : await rules.ChildrenAsync(query.AssetTypeId, includeAllDescendants: true, cancellationToken);
        var result = new List<AssetHitContract>();
        foreach (var type in types)
        {
            foreach (var hit in await procedures.GetAssetHitsAsync(type, (int)query.RequestType, cancellationToken))
                result.Add(new AssetHitContract { Id = hit.AssetId, Tag = hit.Name, Freq = hit.Hits.ToString(CultureInfo.InvariantCulture) });
        }
        return new Success<List<AssetHitContract>>(result);
    }
}

public sealed record GetRecentItemsQuery(Guid UserId);

public interface IGetRecentItemsQueryHandler
{
    Task<Result<List<RecentItemContract>>> ExecuteAsync(GetRecentItemsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>AssetHits_GetForUser: the member's recent assets with view URLs (repeated display names dropped, first wins).</summary>
public sealed class GetRecentItemsQueryHandler(IUiProcedures procedures, IOptions<LegacyEnvironmentOptions> environment) : IGetRecentItemsQueryHandler
{
    public async Task<Result<List<RecentItemContract>>> ExecuteAsync(GetRecentItemsQuery query, CancellationToken cancellationToken = default)
    {
        var baseUrl = environment.Value.ClientUiRoot + "/" + environment.Value.ClientViewPageName;
        var result = new List<RecentItemContract>();
        foreach (var row in await procedures.GetRecentAssetsAsync(query.UserId, cancellationToken))
        {
            if (result.Exists(r => r.AssetName == row.Name))
                continue;
            result.Add(new RecentItemContract
            {
                AssetName = row.Name,
                ViewUrl = $"{baseUrl}?viewId={row.DefaultViewId ?? Guid.Empty:D}&assetId={row.AssetId:D}",
                AssetTypeId = row.AssetTypeId.ToString("D")
            });
        }
        return new Success<List<RecentItemContract>>(result);
    }
}

public sealed record GetViewAssetsQuery(Guid UserId, Guid AssetTypeId, Guid ViewId, string? SearchTerm);

public interface IGetViewAssetsQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetViewAssetsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Assets_GetForAUser / Assets_GetForView: the view's asset type's gen-table names (optionally containing the
/// unescaped term), whatever the user's permissions (the original's three branches return the same dictionary).
/// </summary>
/// <remarks>
/// DEVIATION (recorded in NOTES): a view with filters took AssetDal.Assets_GetMatching (per-filter joins, dates
/// relative to DateTime.Now). No view has filters in the data, so that branch is not ported and fails instead.
/// </remarks>
public sealed class GetViewAssetsQueryHandler(IViewProcedures views, IUiProcedures procedures, IMetadataProcedures metadata, MetadataRules rules)
    : IGetViewAssetsQueryHandler
{
    public async Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetViewAssetsQuery query, CancellationToken cancellationToken = default)
    {
        var view = await ViewDefinition.LoadAsync(views, query.ViewId, cancellationToken) ?? throw new InvalidOperationException("Invalid ViewId");
        var requestType = view.IsInstance ? AssetRequestType.Instance : AssetRequestType.Definition;
        if (!await rules.HasAssetsAsync(query.AssetTypeId, requestType, searchChildAssetTypes: false, cancellationToken))
            _ = await rules.ChildrenAsync(query.AssetTypeId, includeAllDescendants: true, cancellationToken);
        if (view.FilterCount > 0)
            throw new NotSupportedException("Views with filters are not supported (no view has filters).");

        var table = GeneratedObjectNames.Table(view.AssetTypeId, view.IsInstance, isRaw: false);
        var assetTypeName = await metadata.GetAssetTypeNameAsync(view.AssetTypeId, cancellationToken);
        return new Success<List<KeyValuePair<Guid, string>>>([.. await procedures.GetGenTableDictionaryAsync(table, assetTypeName, query.SearchTerm, cancellationToken)]);
    }
}

public sealed record GetDefaultViewForQuery(Guid ReportId);

public interface IGetDefaultViewForQueryHandler
{
    Task<Result<Guid>> ExecuteAsync(GetDefaultViewForQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Asset_GetDefaultViewId: the default (instance) view of the asset type behind a live view id, else behind a report
/// id (deleted reports included); an id that is neither fails.
/// </summary>
public sealed class GetDefaultViewForQueryHandler(IViewProcedures views, IReportProcedures reports, IUiProcedures procedures) : IGetDefaultViewForQueryHandler
{
    public async Task<Result<Guid>> ExecuteAsync(GetDefaultViewForQuery query, CancellationToken cancellationToken = default)
    {
        Guid assetTypeId;
        bool isInstance;
        if (await procedures.IsValidViewIdAsync(query.ReportId, cancellationToken))
        {
            if (await ViewDefinition.LoadAsync(views, query.ReportId, cancellationToken) is not { } view)
                return new Success<Guid>(Guid.Empty);
            assetTypeId = view.AssetTypeId;
            isInstance = view.IsInstance;
        }
        else
        {
            // ReportDal.Get dereferences the missing report (report.Filters = ...), so an id that is neither a live view
            // nor a report fails (M2 finding; the layer's null check is dead code). Deleted views land here too.
            var (report, _) = await reports.GetReportAsync(query.ReportId, cancellationToken);
            if (report is null)
                throw new NullReferenceException("Report not found.");
            _ = await reports.GetReportFiltersAsync(query.ReportId, cancellationToken);
            assetTypeId = report.AssetTypeId ?? throw new InvalidOperationException("Nullable object must have a value.");
            isInstance = report.IsInstance;
        }
        return new Success<Guid>(await procedures.GetDefaultViewIdAsync(assetTypeId, isInstance, cancellationToken));
    }
}

/// <summary>ViewDal.Get as these operations used it: the view row, its group loads and its filters (deleted views included).</summary>
internal sealed record ViewDefinition(Guid AssetTypeId, bool IsInstance, int FilterCount)
{
    public static async Task<ViewDefinition?> LoadAsync(IViewProcedures views, Guid viewId, CancellationToken cancellationToken)
    {
        var row = await views.GetViewAsync(viewId, cancellationToken);
        if (row is null)
            return null;
        var isInstance = row.IsInstance ?? throw new InvalidCastException("View IsInstance is NULL.");
        _ = await views.GetViewPropertyGroupRelationsAsync(viewId, cancellationToken);
        _ = await views.GetViewPropertyGroupsAsync(viewId, cancellationToken);
        var filters = await views.GetViewFiltersAsync(viewId, cancellationToken);
        return new ViewDefinition(row.AssetTypeId, isInstance, filters.Count);
    }
}
