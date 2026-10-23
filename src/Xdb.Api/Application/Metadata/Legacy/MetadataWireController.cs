using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Metadata.Queries.AssetTypes;
using Xdb.Api.Application.Metadata.Queries.Lookups;
using Xdb.Api.Application.Metadata.Queries.PickLists;
using Xdb.Api.Application.Metadata.Queries.Properties;
using Xdb.Api.Application.Metadata.Queries.Values;
using Xdb.Core.Common;
using Xdb.Core.LegacyWire;

namespace Xdb.Api.Application.Metadata.Legacy;

/// <summary>
/// The original service's metadata reads (asset types, properties, picklists, lookups), byte-compatible for the
/// original UI. Each action uses the writer the original operation used: hand-built text as text/html or
/// application/json, or a typed value through the data-contract serializer.
/// </summary>
[Route(LegacyWire.BaseRoute)]
public sealed class MetadataWireController : LegacyWireControllerBase
{
    // ---- Asset types ----

    [HttpGet("AssetType_Get")]
    public async Task<IActionResult> AssetTypeGet([FromServices] IGetAssetTypeQueryHandler handler, [FromQuery] Guid assetTypeId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetTypeQuery(assetTypeId), cancellationToken)).ToLegacyResult(LegacyWire.TypedOrNull);

    [HttpGet("AssetType_GetParent")]
    public async Task<IActionResult> AssetTypeGetParent([FromServices] IGetAssetTypeParentQueryHandler handler, [FromQuery] Guid assetTypeId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetTypeParentQuery(assetTypeId), cancellationToken)).ToLegacyResult(LegacyWire.TypedOrNull);

    [HttpGet("AssetType_GetParentId")]
    public async Task<IActionResult> AssetTypeGetParentId([FromServices] IGetAssetTypeParentIdQueryHandler handler, [FromQuery] Guid assetTypeId,
        [FromQuery] bool topMostParent, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetTypeParentIdQuery(assetTypeId, topMostParent), cancellationToken)).ToLegacyResult(id => LegacyWire.Typed(id));

    [HttpGet("AssetType_GetName")]
    public async Task<IActionResult> AssetTypeGetName([FromServices] IGetAssetTypeNameQueryHandler handler, [FromQuery] Guid assetTypeId,
        [FromQuery] bool getPlural, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetTypeNameQuery(assetTypeId, getPlural), cancellationToken)).ToLegacyResult(name => LegacyWire.Typed(name));

    [HttpGet("AssetType_GetReportingLabel")]
    public async Task<IActionResult> AssetTypeGetReportingLabel([FromServices] IGetAssetTypeReportingLabelQueryHandler handler, [FromQuery] Guid assetTypeId,
        [FromQuery] AssetRequestType assetRequestType, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetTypeReportingLabelQuery(assetTypeId, assetRequestType), cancellationToken)).ToLegacyResult(label => LegacyWire.Typed(label));

    [HttpGet("AssetType_HasAssets")]
    public async Task<IActionResult> AssetTypeHasAssets([FromServices] IAssetTypeHasAssetsQueryHandler handler, [FromQuery] Guid assetTypeId,
        [FromQuery] AssetRequestType assetRequestType, [FromQuery] bool searchChildAssetTypes, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new AssetTypeHasAssetsQuery(assetTypeId, assetRequestType, searchChildAssetTypes), cancellationToken))
            .ToLegacyResult(has => LegacyWire.Typed(has));

    [HttpGet("AssetType_GetDefaultViewId")]
    public async Task<IActionResult> AssetTypeGetDefaultViewId([FromServices] IGetAssetTypeDefaultViewIdQueryHandler handler, [FromQuery] Guid assetTypeId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetTypeDefaultViewIdQuery(assetTypeId), cancellationToken)).ToLegacyResult(id => LegacyWire.Typed(id));

    [HttpGet("AssetType_GetChildren")]
    public async Task<IActionResult> AssetTypeGetChildren([FromServices] IGetAssetTypeDescendantsQueryHandler handler, [FromQuery] Guid parentAssetTypeId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetTypeDescendantsQuery(parentAssetTypeId), cancellationToken)).ToLegacyResult(TypedDictionary);

    [HttpGet("AssetType_GetChildrenOnly")]
    public async Task<IActionResult> AssetTypeGetChildrenOnly([FromServices] IGetAssetTypeDescendantsQueryHandler handler, [FromQuery] Guid parentAssetTypeId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetTypeDescendantsQuery(parentAssetTypeId), cancellationToken))
            .ToLegacyResult(values => LegacyWire.HtmlStream(LegacyFormatters.FormatDictionary(values)));

    [HttpGet("AssetType_GetLowestLevelParents")]
    public async Task<IActionResult> AssetTypeGetLowestLevelParents([FromServices] IGetAssetTypesAvailableForCreationQueryHandler handler,
        [FromQuery] Guid assetTypeId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetTypesAvailableForCreationQuery(assetTypeId), cancellationToken)).ToLegacyResult(TypedDictionary);

    [HttpGet("AssetType_GetByProcessId")]
    public async Task<IActionResult> AssetTypeGetByProcessId([FromServices] IGetAssetTypeByProcessStepQueryHandler handler, [FromQuery] Guid processId,
        [FromQuery] int processStepIndex, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetTypeByProcessStepQuery(processId, processStepIndex), cancellationToken)).ToLegacyResult(id => LegacyWire.Typed(id));

    [HttpGet("AssetTypes_GetByProcessId")]
    public async Task<IActionResult> AssetTypesGetByProcessId([FromServices] IGetAssetTypesByProcessStepQueryHandler handler, [FromQuery] Guid processId,
        [FromQuery] int processStepIndex, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetTypesByProcessStepQuery(processId, processStepIndex), cancellationToken)).ToLegacyResult(TypedDictionary);

    [HttpGet("AssetTypes_GetAll")]
    [HttpGet("AssetTypes_GetDictionary")]
    public async Task<IActionResult> AssetTypesGetAll([FromServices] IGetAssetTypesDictionaryQueryHandler handler, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetTypesDictionaryQuery(IncludeUnused: true), cancellationToken)).ToLegacyResult(JsonDictionary);

    [HttpGet("AssetTypes_GetUsed")]
    public async Task<IActionResult> AssetTypesGetUsed([FromServices] IGetAssetTypesDictionaryQueryHandler handler, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetTypesDictionaryQuery(IncludeUnused: false), cancellationToken)).ToLegacyResult(JsonDictionary);

    [HttpGet("AssetTypes_GetForDynaTree")]
    public async Task<IActionResult> AssetTypesGetForDynaTree([FromServices] IGetRootAssetTypesQueryHandler handler, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetRootAssetTypesQuery(), cancellationToken)).ToLegacyResult(HtmlDictionary);

    [HttpGet("AssetTypes_GetForTreeView")]
    public async Task<IActionResult> AssetTypesGetForTreeView([FromServices] IGetAssetTypeTreeQueryHandler handler, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetTypeTreeQuery(), cancellationToken)).ToLegacyResult(roots => LegacyWire.HtmlStream(AssetTypeTreeJson.Build(roots)));

    [HttpGet("AssetTypes_GetDefaultViews")]
    public async Task<IActionResult> AssetTypesGetDefaultViews([FromServices] IGetDefaultViewsQueryHandler handler, [FromQuery] Guid userId,
        [FromQuery] AssetRequestType assetRequestType, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetDefaultViewsQuery(userId, assetRequestType), cancellationToken))
            .ToLegacyResult(values => LegacyWire.JsonStream(LegacyFormatters.FormatDictionary(values)));

    [HttpGet("AssetTypeId_GetByAssetId")]
    public async Task<IActionResult> AssetTypeIdGetByAssetId([FromServices] IGetAssetTypeIdOfAssetQueryHandler handler, [FromQuery] Guid assetId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetTypeIdOfAssetQuery(assetId), cancellationToken)).ToLegacyResult(TypedNullableGuid);

    // ---- Properties ----

    [HttpGet("Properties_Get")]
    public async Task<IActionResult> PropertiesGet([FromServices] IGetCachedPropertiesQueryHandler handler, [FromQuery] Guid assetTypeId,
        [FromQuery] AssetRequestType requestType, [FromQuery] bool includeDef, [FromQuery] bool includeInstance, [FromQuery] bool excludeUnused,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetCachedPropertiesQuery(assetTypeId, requestType, includeDef, includeInstance, excludeUnused), cancellationToken))
            .ToLegacyResult(LegacyWire.JsonStreamLabelledUtf8);

    [HttpGet("Properties_GetByAssetTypeId")]
    public async Task<IActionResult> PropertiesGetByAssetTypeId([FromServices] IGetAssetTypePropertiesQueryHandler handler, [FromQuery] Guid assetTypeId,
        [FromQuery] AssetRequestType requestType, [FromQuery(Name = "includeInheritedPropeties")] bool includeInherited, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetTypePropertiesQuery(assetTypeId, requestType, includeInherited), cancellationToken)).ToLegacyResult(HtmlDictionary);

    [HttpGet("Properties_GetByECustomReportFieldType")]
    public async Task<IActionResult> PropertiesGetByCustomField([FromServices] IGetPropertiesForCustomFieldQueryHandler handler,
        [FromQuery] LegacyCustomReportFieldType reportFieldType, [FromQuery] Guid assetTypeId, [FromQuery] AssetRequestType requestType,
        [FromQuery] bool includeInheritedProperties, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetPropertiesForCustomFieldQuery((int)reportFieldType, assetTypeId, requestType, includeInheritedProperties), cancellationToken))
            .ToLegacyResult(HtmlDictionary);

    [HttpGet("Properties_GetNonFreeformByAssetTypeId")]
    public async Task<IActionResult> PropertiesGetNonFreeform([FromServices] IGetNonFreeformPropertiesQueryHandler handler, [FromQuery] Guid assetTypeId,
        [FromQuery] AssetRequestType requestType, [FromQuery(Name = "includeInheritedPropeties")] bool includeInherited, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetNonFreeformPropertiesQuery(assetTypeId, requestType, includeInherited), cancellationToken)).ToLegacyResult(HtmlDictionary);

    [HttpGet("Properties_GetByPickListId")]
    public async Task<IActionResult> PropertiesGetByPickListId([FromServices] IGetPickListPropertiesQueryHandler handler, [FromQuery] Guid pickListId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetPickListPropertiesQuery(pickListId), cancellationToken)).ToLegacyResult(HtmlDictionary);

    /// <summary>userId must parse but is unused, as in the original.</summary>
    [HttpGet("Properties_GetDictionary")]
    public async Task<IActionResult> PropertiesGetDictionary([FromServices] IGetPropertiesDictionaryQueryHandler handler, [FromQuery] Guid userId,
        [FromQuery] bool includeDeleted, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetPropertiesDictionaryQuery(includeDeleted), cancellationToken)).ToLegacyResult(JsonDictionary);

    [HttpGet("Property_Get")]
    public async Task<IActionResult> PropertyGet([FromServices] IGetPropertyQueryHandler handler, [FromQuery] Guid propertyId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetPropertyQuery(propertyId), cancellationToken)).ToLegacyResult(LegacyWire.TypedOrNull);

    [HttpGet("Property_GetDataType")]
    public async Task<IActionResult> PropertyGetDataType([FromServices] IGetPropertyDataTypeQueryHandler handler, [FromQuery] Guid propertyId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetPropertyDataTypeQuery(propertyId), cancellationToken)).ToLegacyResult(dataType => LegacyWire.Typed(dataType));

    [HttpGet("Property_GetName")]
    public async Task<IActionResult> PropertyGetName([FromServices] IGetPropertyNameQueryHandler handler, [FromQuery] Guid propertyId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetPropertyNameQuery(propertyId), cancellationToken)).ToLegacyResult(name => LegacyWire.Typed(name));

    [HttpGet("PropertyIds_Get")]
    public async Task<IActionResult> PropertyIdsGet([FromServices] IGetViewPropertyIdsQueryHandler handler, [FromQuery] Guid viewId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetViewPropertyIdsQuery(viewId), cancellationToken)).ToLegacyResult(ids => LegacyWire.Typed(new List<Guid>(ids)));

    [HttpGet("Property_GetAllValues")]
    public async Task<IActionResult> PropertyGetAllValues([FromServices] IGetPotentialValuesQueryHandler handler, [FromQuery] Guid assetTypeId,
        [FromQuery] Guid propertyId, [FromQuery] AssetRequestType assetRequestType, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetPotentialValuesQuery(assetTypeId, propertyId, assetRequestType), cancellationToken)).ToLegacyResult(JsonDictionary);

    /// <summary>A cache hit (or a missing property) is written as text/html, a freshly built answer as application/json.</summary>
    [HttpGet("DictionaryValues_GetForProperty")]
    public async Task<IActionResult> DictionaryValuesGetForProperty([FromServices] IGetDictionaryValuesQueryHandler handler, [FromQuery] Guid propertyId,
        [FromQuery] Guid assetTypeId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetDictionaryValuesQuery(propertyId, assetTypeId), cancellationToken))
            .ToLegacyResult(values => values.FromCache ? LegacyWire.HtmlStream(values.Json) : LegacyWire.JsonStream(values.Json));

    // ---- Picklists ----

    [HttpGet("PickList_Get")]
    public async Task<IActionResult> PickListGet([FromServices] IGetPickListQueryHandler handler, [FromQuery] Guid pickListId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetPickListQuery(pickListId), cancellationToken)).ToLegacyResult(LegacyWire.TypedOrNull);

    [HttpGet("PickList_GetName")]
    public async Task<IActionResult> PickListGetName([FromServices] IGetPickListNameQueryHandler handler, [FromQuery] Guid pickListId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetPickListNameQuery(pickListId), cancellationToken)).ToLegacyResult(name => LegacyWire.Typed(name));

    /// <summary>userId must parse but is unused, as in the original.</summary>
    [HttpGet("PickLists_GetDictionary")]
    public async Task<IActionResult> PickListsGetDictionary([FromServices] IGetPickListsDictionaryQueryHandler handler, [FromQuery] Guid userId,
        [FromQuery] bool includeDeleted, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetPickListsDictionaryQuery(includeDeleted), cancellationToken)).ToLegacyResult(JsonDictionary);

    [HttpGet("PickListValue_Get")]
    public async Task<IActionResult> PickListValueGet([FromServices] IGetPickListValueTextQueryHandler handler, [FromQuery] Guid pickListId,
        [FromQuery] Guid pickListValueId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetPickListValueTextQuery(pickListId, pickListValueId), cancellationToken)).ToLegacyResult(text => LegacyWire.Typed(text));

    [HttpGet("PickListValue_Exists")]
    public async Task<IActionResult> PickListValueExists([FromServices] IFindPickListValueQueryHandler handler, [FromQuery] Guid pickListId,
        [FromQuery] string? value, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new FindPickListValueQuery(pickListId, value), cancellationToken)).ToLegacyResult(TypedNullableGuid);

    [HttpGet("PickListValues")]
    public async Task<IActionResult> PickListValues([FromServices] IGetPickListValueDictionaryQueryHandler handler, [FromQuery] Guid pickListId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetPickListValueDictionaryQuery(pickListId), cancellationToken)).ToLegacyResult(HtmlDictionary);

    [HttpGet("PickListValues_Get")]
    public async Task<IActionResult> PickListValuesGet([FromServices] IGetPickListValuesQueryHandler handler, [FromQuery] Guid pickListId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetPickListValuesQuery(pickListId), cancellationToken)).ToLegacyResult(values => LegacyWire.Typed(values));

    // ---- Lookups and environment ----

    /// <summary>A constant: the original's data type enum names sorted under en-US (computed on .NET Framework).</summary>
    [HttpGet("DataTypes_Get")]
    public IActionResult DataTypesGet() => LegacyWire.HtmlStream(DataTypesJson);

    [HttpGet("Operators_GetDictionary")]
    public async Task<IActionResult> OperatorsGetDictionary([FromServices] IGetOperatorsQueryHandler handler, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetOperatorsQuery(), cancellationToken)).ToLegacyResult(values => LegacyWire.JsonStream(LegacyFormatters.FormatDictionary(values)));

    [HttpGet("Permissions_Get")]
    public async Task<IActionResult> PermissionsGet([FromServices] IGetPermissionsQueryHandler handler, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetPermissionsQuery(), cancellationToken)).ToLegacyResult(values => LegacyWire.HtmlStream(LegacyFormatters.FormatDictionary(values)));

    /// <summary>The original's method was CurrencySymbols_Get but its URI is CurrencySymbols.</summary>
    [HttpGet("CurrencySymbols")]
    public async Task<IActionResult> CurrencySymbolsGet([FromServices] IGetCurrencySymbolsQueryHandler handler, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetCurrencySymbolsQuery(), cancellationToken)).ToLegacyResult(HtmlDictionary);

    [HttpGet("CurrencyValue_Get")]
    public async Task<IActionResult> CurrencyValueGet([FromServices] IGetCurrencyValueQueryHandler handler, [FromQuery] Guid id, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetCurrencyValueQuery(id), cancellationToken)).ToLegacyResult(LegacyWire.TypedOrNull);

    [HttpGet("ImageDictionary_GetSystem")]
    public async Task<IActionResult> ImageDictionaryGetSystem([FromServices] IGetSystemImageDictionaryQueryHandler handler, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetSystemImageDictionaryQuery(), cancellationToken)).ToLegacyResult(HtmlDictionary);

    [HttpGet("Images_GetSystem")]
    public async Task<IActionResult> ImagesGetSystem([FromServices] IGetSystemImagesQueryHandler handler, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetSystemImagesQuery(), cancellationToken)).ToLegacyResult(images => LegacyWire.Typed(images));

    [HttpGet("GenerateGuid")]
    public async Task<IActionResult> GenerateGuid([FromServices] IGenerateGuidQueryHandler handler, [FromQuery] LegacyDataType dataType, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GenerateGuidQuery((int)dataType), cancellationToken)).ToLegacyResult(id => LegacyWire.Typed(id));

    /// <summary>The server's local time (the original host ran on US Eastern time; so must the port's host).</summary>
    [HttpGet("GetServerDateTime")]
    public IActionResult GetServerDateTime([FromServices] IDateTimeProvider clock) => LegacyWire.Typed(clock.UtcNow().ToLocalTime());

    /// <summary>The application version, with the Content-Type the original set explicitly ("text").</summary>
    [HttpGet("Service_GetVersion")]
    public IActionResult ServiceGetVersion() => new LegacyBytesResult(200, "text", Encoding.UTF8.GetBytes("\"0.1.0\""));

    private const string DataTypesJson =
        "[{\"key\":\"9\",\"value\":\"Asset\"},{\"key\":\"14\",\"value\":\"Currency\"},{\"key\":\"0\",\"value\":\"Date\"},{\"key\":\"1\",\"value\":\"DateTime\"}," +
        "{\"key\":\"13\",\"value\":\"Dependency\"},{\"key\":\"2\",\"value\":\"Document\"},{\"key\":\"3\",\"value\":\"Float\"},{\"key\":\"25\",\"value\":\"Geo\"}," +
        "{\"key\":\"24\",\"value\":\"HTML\"},{\"key\":\"4\",\"value\":\"Image\"},{\"key\":\"5\",\"value\":\"Int\"},{\"key\":\"20\",\"value\":\"IPv4\"}," +
        "{\"key\":\"21\",\"value\":\"IPv6\"},{\"key\":\"10\",\"value\":\"Memo\"},{\"key\":\"23\",\"value\":\"PickList\"},{\"key\":\"12\",\"value\":\"Relation_ChildParent\"}," +
        "{\"key\":\"15\",\"value\":\"Relation_Other\"},{\"key\":\"11\",\"value\":\"Relation_ParentChild\"},{\"key\":\"6\",\"value\":\"String\"}," +
        "{\"key\":\"16\",\"value\":\"System_AssetType\"},{\"key\":\"17\",\"value\":\"System_Description\"},{\"key\":\"22\",\"value\":\"System_InstanceOfDesc\"}," +
        "{\"key\":\"-1\",\"value\":\"Undefined\"},{\"key\":\"7\",\"value\":\"URL\"},{\"key\":\"8\",\"value\":\"User\"}]";

    private static IActionResult HtmlDictionary(IReadOnlyList<KeyValuePair<Guid, string>> values) =>
        LegacyWire.HtmlStream(LegacyFormatters.FormatDictionary(values));

    private static IActionResult HtmlDictionary(List<KeyValuePair<Guid, string>> values) =>
        LegacyWire.HtmlStream(LegacyFormatters.FormatDictionary(values));

    private static IActionResult JsonDictionary(IReadOnlyList<KeyValuePair<Guid, string>> values) =>
        LegacyWire.JsonStream(LegacyFormatters.FormatDictionary(values));

    /// <summary>A typed Dictionary&lt;Guid,string&gt;: [{"Key":..,"Value":..}] in insertion order.</summary>
    private static IActionResult TypedDictionary(List<KeyValuePair<Guid, string>> values)
    {
        var dictionary = new Dictionary<Guid, string>();
        foreach (var (key, value) in values)
            dictionary.Add(key, value);
        return LegacyWire.Typed(dictionary);
    }

    private static IActionResult TypedNullableGuid(Guid? value) => value is { } id ? LegacyWire.Typed(id) : LegacyWire.NullTyped();
}
