using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using Xdb.Api.Application.Assets.Queries;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Metadata.Queries.Lookups;
using Xdb.Api.Application.Metadata.Queries.Values;
using Xdb.Core.LegacyWire;

namespace Xdb.Api.Application.Assets.Legacy;

/// <summary>
/// The original service's asset reads (asset editor, values, type-level dictionaries), byte-compatible for the
/// original UI. Each action uses the writer the original operation used.
/// </summary>
[Route(LegacyWire.BaseRoute)]
public sealed class AssetsWireController : LegacyWireControllerBase
{
    // ---- Asset editor ----

    [HttpGet("AssetClient_Get")]
    public async Task<IActionResult> AssetClientGet([FromServices] IGetAssetClientQueryHandler handler, [FromQuery] Guid assetId, [FromQuery] Guid userId,
        [FromQuery] Guid viewId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetClientQuery(assetId, userId, viewId), cancellationToken)).ToLegacyResult(LegacyWire.TypedBytesResult);

    [HttpGet("PropertyValues_GetForAnAssetByViewId")]
    public async Task<IActionResult> PropertyValuesGetForAnAssetByViewId([FromServices] IGetViewValuesQueryHandler handler, [FromQuery] Guid assetId,
        [FromQuery] Guid viewId, [FromQuery] bool includeUnapproved, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetViewValuesQuery(assetId, viewId, includeUnapproved), cancellationToken)).ToLegacyResult(values => LegacyWire.Typed(values));

    [HttpGet("HistoricalValues_Get")]
    public async Task<IActionResult> HistoricalValuesGet([FromServices] IGetHistoryQueryHandler handler, [FromQuery] Guid assetId, [FromQuery] Guid viewId,
        [FromQuery] bool orderChronologically, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetHistoryQuery(assetId, viewId, orderChronologically), cancellationToken))
            .ToLegacyResult(values => LegacyWire.HtmlStream(JsonConvert.SerializeObject(values)));

    [HttpGet("PropertyValues_GetForAsset")]
    public async Task<IActionResult> PropertyValuesGetForAsset([FromServices] IGetAssetValueTextQueryHandler handler, [FromQuery] Guid assetId,
        [FromQuery] Guid propertyId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetValueTextQuery(assetId, propertyId), cancellationToken)).ToLegacyResult(LegacyWire.TypedOrNull);

    [HttpGet("PropertyValue_GetDisplayValue")]
    public async Task<IActionResult> PropertyValueGetDisplayValue([FromServices] IGetValueDisplayQueryHandler handler, [FromQuery] Guid propertyId,
        [FromQuery] string? propertyValue, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetValueDisplayQuery(propertyId, propertyValue), cancellationToken)).ToLegacyResult(LegacyWire.TypedOrNull);

    // ---- Simple per-asset getters ----

    [HttpGet("Asset_GetName")]
    public async Task<IActionResult> AssetGetName([FromServices] IGetAssetNameQueryHandler handler, [FromQuery] Guid assetId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetNameQuery(assetId), cancellationToken)).ToLegacyResult(name => LegacyWire.Typed(name));

    [HttpGet("Asset_GetDescription")]
    public async Task<IActionResult> AssetGetDescription([FromServices] IGetAssetDescriptionQueryHandler handler, [FromQuery] Guid assetId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetDescriptionQuery(assetId), cancellationToken)).ToLegacyResult(text => LegacyWire.Typed(text));

    [HttpGet("Asset_GetParentId")]
    public async Task<IActionResult> AssetGetParentId([FromServices] IGetAssetParentQueryHandler handler, [FromQuery] Guid assetId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetParentQuery(assetId), cancellationToken))
            .ToLegacyResult(id => id is { } parent ? LegacyWire.Typed(parent) : LegacyWire.NullTyped());

    [HttpGet("AssetClientModified")]
    public async Task<IActionResult> AssetClientModified([FromServices] IAssetModifiedQueryHandler handler, [FromQuery] Guid assetId,
        [FromQuery] string? checkDateTime, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new AssetModifiedQuery(assetId, checkDateTime), cancellationToken)).ToLegacyResult(modified => LegacyWire.Typed(modified));

    [HttpGet("Asset_Exists")]
    public async Task<IActionResult> AssetExists([FromServices] IAssetExistsQueryHandler handler, [FromQuery] string? assetName, [FromQuery] Guid assetTypeId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new AssetExistsQuery(assetName, assetTypeId), cancellationToken)).ToLegacyResult(exists => LegacyWire.Typed(exists));

    [HttpGet("AssetId_Get")]
    public async Task<IActionResult> AssetIdGet([FromServices] IFindAssetIdQueryHandler handler, [FromQuery] string? assetName, [FromQuery] Guid assetTypeId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new FindAssetIdQuery(assetName, assetTypeId), cancellationToken)).ToLegacyResult(id => LegacyWire.Typed(id));

    // ---- Type-level dictionaries and counts ----

    [HttpGet("Assets_Get")]
    public async Task<IActionResult> AssetsGet([FromServices] IGetAssetsQueryHandler handler, [FromQuery] Guid assetTypeId,
        [FromQuery] AssetRequestType assetRequestType, [FromQuery] bool includeChildren, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetsQuery(assetTypeId, assetRequestType, includeChildren), cancellationToken)).ToLegacyResult(TypedDictionary);

    [HttpGet("Assets_GetByAssetTypeId")]
    public async Task<IActionResult> AssetsGetByAssetTypeId([FromServices] IGetAssetsQueryHandler handler, [FromQuery] Guid assetTypeId,
        [FromQuery] AssetRequestType assetRequestType, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetsQuery(assetTypeId, assetRequestType, IncludeChildren: false), cancellationToken))
            .ToLegacyResult(values => LegacyWire.HtmlStream(LegacyFormatters.FormatDictionary(values)));

    [HttpGet("Assets_GetDictionary")]
    public async Task<IActionResult> AssetsGetDictionary([FromServices] IGetAssetDefinitionsQueryHandler handler, [FromQuery] Guid assetTypeId,
        [FromQuery] bool includeDescriptions, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetDefinitionsQuery(assetTypeId, includeDescriptions), cancellationToken)).ToLegacyResult(TypedDictionary);

    [HttpGet("Assets_GetInstances")]
    public async Task<IActionResult> AssetsGetInstances([FromServices] IGetAssetInstancesQueryHandler handler, [FromQuery] Guid assetTypeId,
        [FromQuery] bool includeChildren, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetInstancesQuery(assetTypeId, includeChildren), cancellationToken)).ToLegacyResult(TypedDictionary);

    [HttpGet("Assets_GetCount")]
    public async Task<IActionResult> AssetsGetCount([FromServices] ICountAssetsQueryHandler handler, [FromQuery] Guid assetTypeId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new CountAssetsQuery(assetTypeId, Instances: false), cancellationToken)).ToLegacyResult(count => LegacyWire.Typed(count));

    /// <remarks>includeChildren is bound (and validated) but ignored, as in the original.</remarks>
    [HttpGet("Assets_GetInstanceCount")]
    public async Task<IActionResult> AssetsGetInstanceCount([FromServices] ICountAssetsQueryHandler handler, [FromQuery] Guid assetTypeId,
        [FromQuery] bool includeChildren, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new CountAssetsQuery(assetTypeId, Instances: true), cancellationToken)).ToLegacyResult(count => LegacyWire.Typed(count));

    /// <remarks>includeDesc is bound (and validated) but ignored, as in the original.</remarks>
    [HttpGet("Assets_SearchByName")]
    public async Task<IActionResult> AssetsSearchByName([FromServices] ISearchAssetsByNameQueryHandler handler, [FromQuery] Guid assetTypeId,
        [FromQuery] int assetRequestType, [FromQuery] string? searchValue, [FromQuery] bool includeDesc, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new SearchAssetsByNameQuery(assetTypeId, assetRequestType, searchValue), cancellationToken)).ToLegacyResult(TypedDictionary);

    [HttpGet("Assets_GetFromPropertyValue")]
    public async Task<IActionResult> AssetsGetFromPropertyValue([FromServices] IGetAssetsFromPropertyValueQueryHandler handler, [FromQuery] Guid propertyId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetsFromPropertyValueQuery(propertyId), cancellationToken)).ToLegacyResult(TypedDictionary);

    [HttpGet("Assets_GetInPropertyValue")]
    public async Task<IActionResult> AssetsGetInPropertyValue([FromServices] IGetAssetsInPropertyValueQueryHandler handler, [FromQuery] Guid assetTypeId,
        [FromQuery] Guid propertyId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetsInPropertyValueQuery(assetTypeId, propertyId), cancellationToken)).ToLegacyResult(TypedDictionary);

    [HttpGet("Assets_GetGrouped")]
    public async Task<IActionResult> AssetsGetGrouped([FromServices] IGetAssetsGroupedQueryHandler handler, [FromQuery] Guid assetTypeId,
        [FromQuery] Guid propertyId, [FromQuery] bool includeChildAssetTypes, [FromQuery] AssetRequestType requestType, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetsGroupedQuery(assetTypeId, propertyId, includeChildAssetTypes, requestType), cancellationToken))
            .ToLegacyResult(groups => LegacyWire.HtmlStream(LegacyFormatters.FormatGroupedDictionary(groups)));

    // ---- Filter engine ----

    [HttpGet("Assets_GetMatchingByAssetTypeId")]
    public async Task<IActionResult> AssetsGetMatchingByAssetTypeId([FromServices] IMatchAssetsByValueQueryHandler handler, [FromQuery] Guid assetTypeId,
        [FromQuery] int assetRequestType, [FromQuery] Guid propertyId, [FromQuery] string? propertyValue, [FromQuery] bool includeDesc,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new MatchAssetsByValueQuery(assetTypeId, assetRequestType, propertyId, propertyValue), cancellationToken)).ToLegacyResult(TypedDictionary);

    [HttpGet("Assets_GetMatchingByPropertyValue")]
    public async Task<IActionResult> AssetsGetMatchingByPropertyValue([FromServices] IMatchAssetsByValueQueryHandler handler, [FromQuery] Guid assetTypeId,
        [FromQuery] int assetRequestType, [FromQuery] Guid propertyId, [FromQuery] string? propertyValue, [FromQuery] bool includeDesc,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new MatchAssetsByValueQuery(assetTypeId, assetRequestType, propertyId, propertyValue), cancellationToken))
            .ToLegacyResult(values => LegacyWire.HtmlStream(LegacyFormatters.FormatDictionary(values)));

    [HttpPost("Assets_GetViaFiltering")]
    public async Task<IActionResult> AssetsGetViaFiltering([FromServices] IMatchAssetsQueryHandler handler, CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken);
        if (error is not null)
            return error;
        AssetSearchClient? client;
        try
        {
            client = LegacyJsonBody.SearchClient(root);
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        if (client is null)
            return LegacyWire.Fault();
        return (await handler.ExecuteAsync(new MatchAssetsQuery(client.AssetTypeId, client.AssetRequestType, client.FilterExpression, client.Filters), cancellationToken))
            .ToLegacyResult(values => LegacyWire.JsonStream(LegacyFormatters.FormatDictionary(values)));
    }

    [HttpGet("Assets_GetByViewFilter")]
    public async Task<IActionResult> AssetsGetByViewFilter([FromServices] IMatchAssetsBySavedFilterQueryHandler handler, [FromQuery] string? filterId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new MatchAssetsBySavedFilterQuery(filterId), cancellationToken))
            .ToLegacyResult(values => LegacyWire.JsonStream(LegacyFormatters.FormatDictionary(values)));

    [HttpPost("PropertyValues_GetViaFiltering")]
    public async Task<IActionResult> PropertyValuesGetViaFiltering([FromServices] IGetPotentialValuesViaFilteringQueryHandler handler, CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken);
        if (error is not null)
            return error;
        AssetSearchClient? client;
        try
        {
            client = LegacyJsonBody.SearchClient(root);
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        if (client is null)
            return LegacyWire.Fault();
        return (await handler.ExecuteAsync(new GetPotentialValuesViaFilteringQuery(client.AssetTypeId, client.PropertyId, client.AssetRequestType,
                client.FilterExpression, client.Filters), cancellationToken))
            .ToLegacyResult(values => LegacyWire.JsonStream(LegacyFormatters.FormatDictionary(values)));
    }

    [HttpPost("PropertyValues_GetPotential")]
    public async Task<IActionResult> PropertyValuesGetPotential([FromServices] IGetPotentialValuesForAssetsQueryHandler handler,
        [FromServices] IOptions<LegacyEnvironmentOptions> environment, CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken, wrapped: true);
        if (error is not null)
            return error;
        GetPotentialValuesForAssetsQuery query;
        try
        {
            query = new GetPotentialValuesForAssetsQuery(
                LegacyJsonBody.Member(root, "assetIds", LegacyJsonBody.GuidList, null),
                LegacyJsonBody.Member(root, "propertyId", LegacyJsonBody.Guid, Guid.Empty),
                LegacyJsonBody.Member(root, "assetTypeId", LegacyJsonBody.Guid, Guid.Empty),
                LegacyJsonBody.Member(root, "requestType", LegacyJsonBody.Enum(environment.Value.AssetRequestTypeName), 0));
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        return (await handler.ExecuteAsync(query, cancellationToken)).ToLegacyResult(TypedDictionary);
    }

    [HttpGet("PropertyValues_GetPotentialByAssetType")]
    public async Task<IActionResult> PropertyValuesGetPotentialByAssetType([FromServices] IGetPotentialValuesQueryHandler handler, [FromQuery] Guid assetTypeId,
        [FromQuery] Guid propertyId, [FromQuery] AssetRequestType assetRequestType, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetPotentialValuesQuery(assetTypeId, propertyId, assetRequestType), cancellationToken))
            .ToLegacyResult(values => LegacyWire.JsonStream(LegacyFormatters.FormatDictionary(values)));

    [HttpPost("Assets_GetMatching")]
    public async Task<IActionResult> AssetsGetMatching([FromServices] IGetMatchingAssetsQueryHandler handler, CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken, wrapped: true);
        if (error is not null)
            return error;
        GetMatchingAssetsQuery query;
        try
        {
            query = new GetMatchingAssetsQuery(
                LegacyJsonBody.Member(root, "assetIds", LegacyJsonBody.GuidList, null),
                LegacyJsonBody.Member(root, "propertyId", LegacyJsonBody.Guid, Guid.Empty),
                LegacyJsonBody.Member(root, "propertyValue", LegacyJsonBody.Text, null),
                LegacyJsonBody.Member(root, "includeDesc", LegacyJsonBody.Bool, false));
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        return (await handler.ExecuteAsync(query, cancellationToken)).ToLegacyResult(TypedDictionary);
    }

    [HttpPost("Assets_GetByPropertyValues")]
    public async Task<IActionResult> AssetsGetByPropertyValues([FromServices] IGetAssetsByPropertyValuesQueryHandler handler, CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken, wrapped: true);
        if (error is not null)
            return error;
        GetAssetsByPropertyValuesQuery query;
        try
        {
            query = new GetAssetsByPropertyValuesQuery(
                LegacyJsonBody.Member(root, "assetTypeId", LegacyJsonBody.Guid, Guid.Empty),
                LegacyJsonBody.Member(root, "propertyIdsAndValues", LegacyJsonBody.GuidDictionary, null));
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        return (await handler.ExecuteAsync(query, cancellationToken)).ToLegacyResult(TypedDictionary);
    }

    // ---- UI layer ----

    [HttpPost("AssetFilterClient_Get")]
    public async Task<IActionResult> AssetFilterClientGet([FromServices] IGetChartFilterQueryHandler handler, CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken);
        if (error is not null)
            return error;
        AssetSearchClient? client;
        try
        {
            client = LegacyJsonBody.SearchClient(root);
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        if (client is null)
            return LegacyWire.Fault();
        return (await handler.ExecuteAsync(new GetChartFilterQuery(client.Id, client.AssetTypeId, client.AssetRequestType, client.PropertyId,
                client.FilterExpression, client.Filters), cancellationToken))
            .ToLegacyResult(filterClient => LegacyWire.Typed(filterClient));
    }

    [HttpGet("RelatedItems_GetHtml")]
    public async Task<IActionResult> RelatedItemsGetHtml([FromServices] IGetRelatedItemsHtmlQueryHandler handler, [FromQuery] Guid userId, [FromQuery] Guid assetId,
        [FromQuery] string? propertyValue, [FromQuery] string? imgSrc, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetRelatedItemsHtmlQuery(userId, assetId, propertyValue, imgSrc), cancellationToken)).ToLegacyResult(lines => LegacyWire.Typed(lines));

    /// <remarks>The original UI calls this one as "rgraph"; routes match case-insensitively.</remarks>
    [HttpGet("RGraph")]
    public async Task<IActionResult> RGraph([FromServices] IGetRelationGraphQueryHandler handler, [FromQuery] string? assetId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetRelationGraphQuery(assetId), cancellationToken)).ToLegacyResult(graph => LegacyWire.Typed(graph));

    [HttpGet("AssetHits_Get")]
    public async Task<IActionResult> AssetHitsGet([FromServices] IGetAssetHitsQueryHandler handler, [FromQuery] Guid assetTypeId,
        [FromQuery] AssetRequestType requestType, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetHitsQuery(assetTypeId, requestType), cancellationToken)).ToLegacyResult(hits => LegacyWire.Typed(hits));

    [HttpGet("AssetHits_GetForUser")]
    public async Task<IActionResult> AssetHitsGetForUser([FromServices] IGetRecentItemsQueryHandler handler, [FromQuery] Guid userId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetRecentItemsQuery(userId), cancellationToken)).ToLegacyResult(items => LegacyWire.Typed(items));

    [HttpGet("Assets_GetForAUser")]
    public async Task<IActionResult> AssetsGetForAUser([FromServices] IGetViewAssetsQueryHandler handler, [FromQuery] Guid userId, [FromQuery] Guid assetTypeId,
        [FromQuery] Guid viewId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetViewAssetsQuery(userId, assetTypeId, viewId, string.Empty), cancellationToken)).ToLegacyResult(TypedDictionary);

    [HttpGet("Assets_GetForView")]
    public async Task<IActionResult> AssetsGetForView([FromServices] IGetViewAssetsQueryHandler handler, [FromQuery] Guid userId, [FromQuery] Guid assetTypeId,
        [FromQuery] Guid viewId, [FromQuery] string? term, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetViewAssetsQuery(userId, assetTypeId, viewId, term), cancellationToken))
            .ToLegacyResult(values => LegacyWire.JsonStream(LegacyFormatters.FormatDictionary(values)));

    [HttpGet("Asset_GetDefaultViewId")]
    public async Task<IActionResult> AssetGetDefaultViewId([FromServices] IGetDefaultViewForQueryHandler handler, [FromQuery] Guid reportId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetDefaultViewForQuery(reportId), cancellationToken)).ToLegacyResult(id => LegacyWire.Typed(id));

    [HttpGet("DictionaryValues_GetForPropertyRestricted")]
    public async Task<IActionResult> DictionaryValuesGetForPropertyRestricted([FromServices] IGetRestrictedValuesQueryHandler handler, [FromQuery] Guid parentPropertyId,
        [FromQuery] Guid childPropertyId, [FromQuery] string? parentPropertyValue, [FromQuery] Guid sourcePropertyId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetRestrictedValuesQuery(parentPropertyId, childPropertyId, parentPropertyValue, sourcePropertyId), cancellationToken))
            .ToLegacyResult(restricted => restricted.AsJson
                ? LegacyWire.JsonStream(LegacyFormatters.FormatDictionary(restricted.Values))
                : LegacyWire.HtmlStream(LegacyFormatters.FormatDictionary(restricted.Values)));

    [HttpGet("Assets_GetPotentialParents")]
    public async Task<IActionResult> AssetsGetPotentialParents([FromServices] IGetPotentialParentsQueryHandler handler, [FromQuery] Guid childAssetTypeId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetPotentialParentsQuery(childAssetTypeId), cancellationToken))
            .ToLegacyResult(values => values is null ? LegacyWire.NullTyped() : TypedDictionary(values));

    /// <summary>A typed Dictionary&lt;Guid,string&gt;: [{"Key":..,"Value":..}] in insertion order.</summary>
    private static IActionResult TypedDictionary(List<KeyValuePair<Guid, string>> values)
    {
        var dictionary = new Dictionary<Guid, string>();
        foreach (var (key, value) in values)
            dictionary.Add(key, value);
        return LegacyWire.Typed(dictionary);
    }
}
