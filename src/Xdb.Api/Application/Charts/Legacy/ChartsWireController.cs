using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Charts.Commands;
using Xdb.Api.Application.Charts.Queries;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Core.LegacyWire;

namespace Xdb.Api.Application.Charts.Legacy;

/// <summary>
/// The original service's chart, dashboard and map operations, byte-compatible for the original UI. Six of them were
/// stubs in the deployed release (their bodies ignore the inputs); they are reproduced as such, after binding.
/// </summary>
[Route(LegacyWire.BaseRoute)]
public sealed class ChartsWireController : LegacyWireControllerBase
{
    // ---- Stubs in the deployed release ----

    [HttpGet("ChartDef_Get")]
    public IActionResult ChartDefGet([FromQuery] Guid assetTypeId, [FromQuery] AssetRequestType requestType, [FromQuery] Guid propertyId,
        [FromQuery] int maxRecords, [FromQuery] Guid filterPropertyId, [FromQuery] Guid operatorId, [FromQuery] string? value) =>
        LegacyWire.NullTyped();

    [HttpGet("ChartGauge_GetForDashboard")]
    public IActionResult ChartGaugeGetForDashboard([FromQuery] string? chartId) => LegacyWire.NullTyped();

    [HttpGet("ChartDefinitions")]
    public IActionResult ChartDefinitions([FromQuery] Guid userId) => LegacyWire.NullTyped();

    [HttpGet("MapData")]
    public IActionResult MapData([FromQuery] string? chartId) => LegacyWire.Typed(new List<object>());

    [HttpGet("MapMarkerData")]
    public IActionResult MapMarkerData([FromQuery] string? chartId, [FromQuery] string? itemId) => LegacyWire.Typed(new Dictionary<string, string>());

    [HttpGet("ChartDataForGrid")]
    public IActionResult ChartDataForGrid([FromQuery] string? chartId) => LegacyWire.HtmlStream(string.Empty);

    // ---- Without database access ----

    [HttpGet("ChartTypes_Get")]
    public IActionResult ChartTypesGet() => LegacyWire.HtmlStream(LegacyFormatters.FormatDictionary(ChartTypes.All));

    [HttpGet("Chart_Create")]
    public async Task<IActionResult> ChartCreate([FromServices] ICreateChartQueryHandler handler, [FromQuery] Guid userId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new CreateChartQuery(userId), cancellationToken)).ToLegacyResult(chart => LegacyWire.Typed(chart));

    // ---- Chart reads ----

    [HttpGet("Chart_GetForDashboard")]
    public async Task<IActionResult> ChartGetForDashboard([FromServices] IGetDashboardChartQueryHandler handler, [FromQuery] string? chartId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetDashboardChartQuery(chartId), cancellationToken)).ToLegacyResult(chart => LegacyWire.Typed(chart));

    [HttpGet("Chart_GetSupportData")]
    public async Task<IActionResult> ChartGetSupportData([FromServices] IGetChartSupportDataQueryHandler handler, [FromQuery] Guid chartId,
        [FromQuery] string? x, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetChartSupportDataQuery(chartId, x), cancellationToken)).ToLegacyResult(values => LegacyWire.Typed(values));

    [HttpPost("Chart_GetViaFiltering")]
    public async Task<IActionResult> ChartGetViaFiltering([FromServices] IRenderChartCommandHandler handler, CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken);
        if (error is not null)
            return error;
        AssetSearchClient? options;
        try
        {
            options = LegacyJsonBody.SearchClient(root);
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        return (await handler.ExecuteAsync(new RenderChartCommand(options), cancellationToken)).ToLegacyResult(chart => LegacyWire.Typed(chart));
    }

    // ---- Dashboards ----

    [HttpGet("Dashboards_Get")]
    public async Task<IActionResult> DashboardsGet([FromServices] IGetDashboardsQueryHandler handler, [FromQuery] string? userId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetDashboardsQuery(userId), cancellationToken))
            .ToLegacyResult(values => LegacyWire.JsonStream(LegacyFormatters.FormatDictionary(values)));

    [HttpPost("Dashboard_Save")]
    public async Task<IActionResult> DashboardSave([FromServices] ISaveDashboardCommandHandler handler, CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken);
        if (error is not null)
            return error;
        DashboardBody? dashboard;
        try
        {
            dashboard = LegacyJsonBody.Dashboard(root);
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        return (await handler.ExecuteAsync(new SaveDashboardCommand(dashboard), cancellationToken)).ToLegacyResult(saved => LegacyWire.Typed(saved));
    }

    [HttpPost("DashboardItem_Add")]
    public async Task<IActionResult> DashboardItemAdd([FromServices] IAddDashboardItemCommandHandler handler, CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken);
        if (error is not null)
            return error;
        DashboardItemBody? item;
        try
        {
            item = LegacyJsonBody.DashboardItem(root);
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        return (await handler.ExecuteAsync(new AddDashboardItemCommand(item), cancellationToken)).ToLegacyResult(added => LegacyWire.Typed(added));
    }

    // ---- Mappings ----

    [HttpGet("Mappings_GetUnmapped")]
    public async Task<IActionResult> MappingsGetUnmapped([FromServices] IGetUnmappedQueryHandler handler, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetUnmappedQuery(), cancellationToken))
            .ToLegacyResult(values => LegacyWire.HtmlStream(LegacyFormatters.FormatDictionary(values)));

    [HttpPost("Mapping_Set")]
    public async Task<IActionResult> MappingSet([FromServices] ISetMappingCommandHandler handler, CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken);
        if (error is not null)
            return error;
        (string? Key, string? Value) mapping;
        try
        {
            mapping = LegacyJsonBody.KeyValue(root);
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        return (await handler.ExecuteAsync(new SetMappingCommand(mapping.Key, mapping.Value), cancellationToken)).ToLegacyResult(set => LegacyWire.Typed(set));
    }
}
