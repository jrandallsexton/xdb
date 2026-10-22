using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json.Linq;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Views.Queries;
using Xdb.Core.Common;
using Xdb.Core.LegacyWire;

namespace Xdb.Api.Application.Views.Legacy;

/// <summary>
/// The original service's view, property group, process, help and role/view reads, byte-compatible for the original UI.
/// </summary>
[Route(LegacyWire.BaseRoute)]
public sealed class ViewsWireController : LegacyWireControllerBase
{
    // ---- Views ----

    [HttpGet("ViewClient_Get")]
    public async Task<IActionResult> ViewClientGet([FromServices] IGetViewClientQueryHandler handler, [FromQuery] Guid viewId, [FromQuery] Guid userId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetViewClientQuery(viewId, userId), cancellationToken)).ToLegacyResult(LegacyWire.TypedBytesResult);

    [HttpGet("ViewClientModified")]
    public async Task<IActionResult> ViewClientModified([FromServices] IViewClientModifiedQueryHandler handler, [FromQuery] Guid viewId,
        [FromQuery] string? checkDateTime, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new ViewClientModifiedQuery(viewId, checkDateTime), cancellationToken)).ToLegacyResult(modified => LegacyWire.Typed(modified));

    [HttpGet("View_Get")]
    public async Task<IActionResult> ViewGet([FromServices] IGetViewQueryHandler handler, [FromQuery] Guid viewId, [FromQuery] Guid userId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetViewQuery(viewId, userId), cancellationToken)).ToLegacyResult(view => LegacyWire.Typed(view));

    [HttpGet("Views_GetByAssetType")]
    public async Task<IActionResult> ViewsGetByAssetType([FromServices] IGetViewsByAssetTypeQueryHandler handler, [FromQuery] Guid assetTypeId,
        [FromQuery] AssetRequestType assetRequestType, [FromQuery] bool includeNonStandAlone, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetViewsByAssetTypeQuery(assetTypeId, assetRequestType, includeNonStandAlone), cancellationToken)).ToLegacyResult(Html);

    [HttpGet("Views_GetByPropertyGroupId")]
    public async Task<IActionResult> ViewsGetByPropertyGroupId([FromServices] IGetPropertyGroupViewsQueryHandler handler, [FromQuery] Guid propertyGroupId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetPropertyGroupViewsQuery(propertyGroupId), cancellationToken)).ToLegacyResult(Html);

    [HttpGet("Views_GetDictionary")]
    public async Task<IActionResult> ViewsGetDictionary([FromServices] IGetViewsDictionaryQueryHandler handler, [FromQuery] bool includeDeleted,
        [FromQuery] bool includeNonStandAlone, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetViewsDictionaryQuery(includeDeleted, includeNonStandAlone), cancellationToken)).ToLegacyResult(Html);

    /// <summary>Both ids are strings the original parsed itself: an invalid one is the generic fault, not the request-error page.</summary>
    [HttpGet("ViewFilters_Get")]
    public async Task<IActionResult> ViewFiltersGet([FromServices] IGetMemberFiltersQueryHandler handler, [FromQuery] string? userId, [FromQuery] string? viewId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(userId, out var member) || !Guid.TryParse(viewId, out var view))
            return LegacyWire.Fault();
        return (await handler.ExecuteAsync(new GetMemberFiltersQuery(view, member), cancellationToken)).ToLegacyResult(Json);
    }

    [HttpGet("ViewFilter_GetDisplay")]
    public async Task<IActionResult> ViewFilterGetDisplay([FromServices] IGetViewFilterDisplayQueryHandler handler, [FromQuery] string? viewFilterId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(viewFilterId, out var id))
            return LegacyWire.Fault();
        return (await handler.ExecuteAsync(new GetViewFilterDisplayQuery(id), cancellationToken)).ToLegacyResult(LegacyWire.TypedOrNull);
    }

    /// <summary>
    /// POST with an AssetSearchClient body, bound as the original host bound it: malformed JSON is 400 with no body;
    /// a missing required member is the request-error page; an empty body or null filters fail with the generic fault.
    /// </summary>
    [HttpPost("ViewFilter_GetDisplayBySearchClient")]
    public async Task<IActionResult> ViewFilterGetDisplayBySearchClient([FromServices] IGetSearchFilterDisplayQueryHandler handler, CancellationToken cancellationToken)
    {
        string body;
        using (var reader = new StreamReader(Request.Body))
            body = await reader.ReadToEndAsync(cancellationToken);
        if (body.Length == 0)
            return LegacyWire.Fault();
        // A body without a Content-Type never reaches the operation (measured: the request-error page).
        if (string.IsNullOrEmpty(Request.ContentType))
            return LegacyWire.RequestError();

        if (Assets.Legacy.LegacyJsonBody.Malformed(body) is { } rejected)
            return rejected;
        var root = JsonDocument.Parse(body).RootElement;
        // A literal null body reaches the operation as a null client, which it dereferences.
        if (root.ValueKind == JsonValueKind.Null)
            return LegacyWire.Fault();

        string[] required = ["assetTypeId", "assetRequestType", "filterName", "filterExpression", "filters"];
        if (root.ValueKind != JsonValueKind.Object || required.Any(name => !root.TryGetProperty(name, out _)))
            return LegacyWire.RequestError();

        var filters = root.GetProperty("filters");
        if (filters.ValueKind == JsonValueKind.Array && filters.EnumerateArray().Any(f =>
                f.ValueKind == JsonValueKind.Object && new[] { "propertyId", "operatorId", "value", "order" }.Any(name => !f.TryGetProperty(name, out _))))
            return LegacyWire.RequestError();

        if (!Guid.TryParse(Text(root.GetProperty("assetTypeId")), out _) || filters.ValueKind != JsonValueKind.Array)
            return LegacyWire.Fault();

        var searchFilters = new List<SearchFilter>();
        foreach (var filter in filters.EnumerateArray())
        {
            var operatorId = Number(filter.GetProperty("operatorId"));
            if (operatorId is < 0 or > 6)
                return LegacyWire.Fault();
            if (!Guid.TryParse(Text(filter.GetProperty("propertyId")), out var propertyId))
                return LegacyWire.Fault();
            searchFilters.Add(new SearchFilter(propertyId, operatorId, Text(filter.GetProperty("value")) ?? string.Empty));
        }

        return (await handler.ExecuteAsync(new GetSearchFilterDisplayQuery(Text(root.GetProperty("filterExpression")), searchFilters), cancellationToken))
            .ToLegacyResult(text => LegacyWire.Typed(text));
    }

    [HttpGet("ViewAndAssetTypeAreCompatible")]
    public async Task<IActionResult> ViewAndAssetTypeAreCompatible([FromServices] IViewMatchesAssetTypeQueryHandler handler, [FromQuery] Guid viewId,
        [FromQuery] Guid assetTypeId, [FromQuery] AssetRequestType requestType, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new ViewMatchesAssetTypeQuery(viewId, assetTypeId, requestType), cancellationToken)).ToLegacyResult(match => LegacyWire.Typed(match));

    [HttpGet("Views_Get")]
    public async Task<IActionResult> ViewsGet([FromServices] IGetUserViewsQueryHandler handler, [FromQuery] Guid userId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetUserViewsQuery(userId), cancellationToken)).ToLegacyResult(TypedDictionary);

    [HttpGet("Views_GetForUser")]
    public async Task<IActionResult> ViewsGetForUser([FromServices] IGetUserViewsQueryHandler handler, [FromQuery] Guid userId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetUserViewsQuery(userId), cancellationToken)).ToLegacyResult(Html);

    [HttpGet("ViewGroups_Get")]
    public async Task<IActionResult> ViewGroupsGet([FromServices] IGetUserViewGroupsQueryHandler handler, [FromQuery] Guid userId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetUserViewGroupsQuery(userId), cancellationToken)).ToLegacyResult(Html);

    [HttpGet("Views_GetByRoleId")]
    public async Task<IActionResult> ViewsGetByRoleId([FromServices] IGetRoleViewsQueryHandler handler, [FromQuery] Guid roleId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetRoleViewsQuery(roleId), cancellationToken)).ToLegacyResult(relations => LegacyWire.Typed(relations));

    /// <summary>A new, empty relation list (its IsNew/IsDirty are not serialized): always [].</summary>
    [HttpGet("RoleViewRelationList")]
    public IActionResult RoleViewRelationList() => LegacyWire.Typed(new List<RoleViewRelationContract>());

    [HttpGet("Roles_GetByViewId")]
    public async Task<IActionResult> RolesGetByViewId([FromServices] IGetViewRolesQueryHandler handler, [FromQuery] Guid viewId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetViewRolesQuery(viewId), cancellationToken)).ToLegacyResult(Html);

    [HttpGet("Roles_GetForUser")]
    public async Task<IActionResult> RolesGetForUser([FromServices] IGetMemberRolesQueryHandler handler, [FromQuery] Guid userId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetMemberRolesQuery(userId), cancellationToken)).ToLegacyResult(Html);

    // ---- Property groups ----

    [HttpGet("PropertyGroup_Get")]
    public async Task<IActionResult> PropertyGroupGet([FromServices] IGetPropertyGroupQueryHandler handler, [FromQuery] Guid propertyGroupId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetPropertyGroupQuery(propertyGroupId), cancellationToken)).ToLegacyResult(LegacyWire.TypedOrNull);

    [HttpGet("PropertyGroups_GetByAssetTypeId")]
    public async Task<IActionResult> PropertyGroupsGetByAssetTypeId([FromServices] IGetAssetTypePropertyGroupsQueryHandler handler, [FromQuery] Guid assetTypeId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetTypePropertyGroupsQuery(assetTypeId), cancellationToken)).ToLegacyResult(Html);

    [HttpGet("PropertyGroups_GetByPropertyId")]
    public async Task<IActionResult> PropertyGroupsGetByPropertyId([FromServices] IGetPropertyGroupsForPropertyQueryHandler handler, [FromQuery] Guid propertyId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetPropertyGroupsForPropertyQuery(propertyId), cancellationToken)).ToLegacyResult(Html);

    /// <summary>userId must parse but is unused, as in the original.</summary>
    [HttpGet("PropertyGroups_GetDictionary")]
    public async Task<IActionResult> PropertyGroupsGetDictionary([FromServices] IGetPropertyGroupsQueryHandler handler, [FromQuery] Guid userId,
        [FromQuery] bool includeDeleted, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetPropertyGroupsQuery(includeDeleted), cancellationToken)).ToLegacyResult(Html);

    // ---- Processes ----

    /// <summary>The id is a string the original parsed itself: an invalid one returns null (empty 200).</summary>
    [HttpGet("Process_Get")]
    public async Task<IActionResult> ProcessGet([FromServices] IGetProcessQueryHandler handler, [FromQuery] string? id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var processId))
            return LegacyWire.NullTyped();
        return (await handler.ExecuteAsync(new GetProcessQuery(processId), cancellationToken)).ToLegacyResult(LegacyWire.TypedOrNull);
    }

    [HttpGet("Processes_GetAll")]
    public async Task<IActionResult> ProcessesGetAll([FromServices] IGetProcessesQueryHandler handler, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetProcessesQuery(), cancellationToken)).ToLegacyResult(Html);

    [HttpGet("Processes_GetByAssetTypeId")]
    public async Task<IActionResult> ProcessesGetByAssetTypeId([FromServices] IGetAssetTypeProcessesQueryHandler handler, [FromQuery] Guid assetTypeId,
        [FromQuery] AssetRequestType requestType, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetAssetTypeProcessesQuery(assetTypeId, requestType), cancellationToken)).ToLegacyResult(Html);

    [HttpGet("ProcessStep_AssetNameLabel")]
    public async Task<IActionResult> ProcessStepAssetNameLabel([FromServices] IGetProcessStepLabelQueryHandler handler, [FromQuery] Guid processId,
        [FromQuery] int stepIndex, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetProcessStepLabelQuery(processId, stepIndex), cancellationToken)).ToLegacyResult(label => LegacyWire.Typed(label));

    [HttpGet("ProcessStep_IsCreation")]
    public Task<IActionResult> ProcessStepIsCreation([FromServices] IGetProcessStepFlagQueryHandler handler, [FromQuery] Guid processId, [FromQuery] int stepIndex,
        CancellationToken cancellationToken) => Flag(handler, processId, stepIndex, ProcessStepFlag.IsCreation, cancellationToken);

    [HttpGet("ProcessStep_IsEdit")]
    public Task<IActionResult> ProcessStepIsEdit([FromServices] IGetProcessStepFlagQueryHandler handler, [FromQuery] Guid processId, [FromQuery] int stepIndex,
        CancellationToken cancellationToken) => Flag(handler, processId, stepIndex, ProcessStepFlag.IsEdit, cancellationToken);

    [HttpGet("ProcessStep_IsInstance")]
    public Task<IActionResult> ProcessStepIsInstance([FromServices] IGetProcessStepFlagQueryHandler handler, [FromQuery] Guid processId, [FromQuery] int stepIndex,
        CancellationToken cancellationToken) => Flag(handler, processId, stepIndex, ProcessStepFlag.IsInstance, cancellationToken);

    [HttpGet("ProcessStep_ShowAssetTypeSelector")]
    public Task<IActionResult> ProcessStepShowAssetTypeSelector([FromServices] IGetProcessStepFlagQueryHandler handler, [FromQuery] Guid processId,
        [FromQuery] int stepIndex, CancellationToken cancellationToken) => Flag(handler, processId, stepIndex, ProcessStepFlag.ShowAssetTypeSelector, cancellationToken);

    [HttpGet("ProcessStep_GetFilters")]
    public async Task<IActionResult> ProcessStepGetFilters([FromServices] IGetProcessStepFiltersQueryHandler handler, [FromQuery] Guid processId,
        [FromQuery] int processStepIndex, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetProcessStepFiltersQuery(processId, processStepIndex), cancellationToken)).ToLegacyResult(values =>
        {
            var dictionary = new Dictionary<int, Guid>();
            foreach (var (order, propertyId) in values)
                dictionary.Add(order, propertyId);
            return LegacyWire.Typed(dictionary);
        });

    [HttpGet("ProcessStep_GetFilterValues")]
    public async Task<IActionResult> ProcessStepGetFilterValues([FromServices] IGetFilterValuesQueryHandler handler, [FromQuery] Guid propertyId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetFilterValuesQuery(propertyId), cancellationToken)).ToLegacyResult(TypedDictionary);

    [HttpGet("ProcessStep_GetNextFilterValues")]
    public async Task<IActionResult> ProcessStepGetNextFilterValues([FromServices] IGetNextFilterValuesQueryHandler handler, [FromQuery] Guid propertyId,
        [FromQuery] string? selectedValue, [FromQuery] Guid nextPropertyId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetNextFilterValuesQuery(propertyId, selectedValue, nextPropertyId), cancellationToken)).ToLegacyResult(TypedDictionary);

    [HttpGet("ViewId_GetByProcessId")]
    public async Task<IActionResult> ViewIdGetByProcessId([FromServices] IGetProcessStepViewQueryHandler handler, [FromQuery] Guid processId,
        [FromQuery] int processStepIndex, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetProcessStepViewQuery(processId, processStepIndex), cancellationToken)).ToLegacyResult(id => LegacyWire.Typed(id));

    // ---- Help ----

    [HttpGet("HelpSection_Get")]
    public async Task<IActionResult> HelpSectionGet([FromServices] IGetHelpSectionQueryHandler handler, [FromQuery] Guid helpSectionId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetHelpSectionQuery(helpSectionId), cancellationToken)).ToLegacyResult(LegacyWire.TypedOrNull);

    /// <summary>Serialized by Json.NET in the original (declaration order, \r\n escapes, raw '/'), sent as text/html.</summary>
    [HttpGet("HelpSections_Get")]
    public async Task<IActionResult> HelpSectionsGet([FromServices] IGetHelpSectionsQueryHandler handler, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetHelpSectionsQuery(), cancellationToken)).ToLegacyResult(sections =>
        {
            var array = new JArray();
            foreach (var section in sections)
            {
                array.Add(section is null
                    ? JValue.CreateNull()
                    : new JObject
                    {
                        ["Id"] = section.Id, ["Title"] = section.Title, ["ImageId"] = section.ImageId, ["Order"] = section.Order, ["Content"] = section.Content
                    });
            }
            return LegacyWire.HtmlStream(array.ToString(Newtonsoft.Json.Formatting.None));
        });

    /// <summary>userId must parse but is unused, as in the original.</summary>
    [HttpGet("HelpSections_GetDictionary")]
    public async Task<IActionResult> HelpSectionsGetDictionary([FromServices] IGetHelpSectionsDictionaryQueryHandler handler, [FromQuery] Guid userId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetHelpSectionsDictionaryQuery(), cancellationToken)).ToLegacyResult(Json);

    private static async Task<IActionResult> Flag(IGetProcessStepFlagQueryHandler handler, Guid processId, int stepIndex, ProcessStepFlag flag,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetProcessStepFlagQuery(processId, stepIndex, flag), cancellationToken)).ToLegacyResult(value => LegacyWire.Typed(value));

    private static IActionResult Html(IReadOnlyList<KeyValuePair<Guid, string>> values) => LegacyWire.HtmlStream(LegacyFormatters.FormatDictionary(values));

    private static IActionResult Html(List<KeyValuePair<Guid, string>> values) => LegacyWire.HtmlStream(LegacyFormatters.FormatDictionary(values));

    private static IActionResult Json(IReadOnlyList<KeyValuePair<Guid, string>> values) => LegacyWire.JsonStream(LegacyFormatters.FormatDictionary(values));

    private static IActionResult TypedDictionary(IReadOnlyList<KeyValuePair<Guid, string>> values)
    {
        var dictionary = new Dictionary<Guid, string>();
        foreach (var (key, value) in values)
            dictionary.Add(key, value);
        return LegacyWire.Typed(dictionary);
    }

    /// <summary>A JSON string, or the text of a number/literal (the original accepted "0" where an int was declared).</summary>
    private static string? Text(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Null => null,
        _ => element.GetRawText()
    };

    private static int Number(JsonElement element) => element.ValueKind == JsonValueKind.Number
        ? element.GetInt32()
        : int.Parse(element.GetString() ?? string.Empty, NumberStyles.Integer, CultureInfo.InvariantCulture);
}
