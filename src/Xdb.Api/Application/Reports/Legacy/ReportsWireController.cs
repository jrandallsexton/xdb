using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Xdb.Api.Application.Reports.Queries.GetReportClient;
using Xdb.Api.Application.Reports.Queries.GetReportData;
using Xdb.Api.Application.Reports.Queries.GetReportExecutionStats;
using Xdb.Core.LegacyWire;

namespace Xdb.Api.Application.Reports.Legacy;

/// <summary>The original service's report-viewer operations, byte-compatible for the original UI.</summary>
[Route(LegacyWire.BaseRoute)]
public sealed class ReportsWireController : LegacyWireControllerBase
{
    /// <summary>Invalid ids return an empty 200 (the original returned a null Stream).</summary>
    [HttpGet("ReportClient_Get")]
    public async Task<IActionResult> ReportClientGet(
        [FromServices] IGetReportClientQueryHandler handler,
        [FromQuery] string? userId,
        [FromQuery] string? reportId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(userId, out _) || !Guid.TryParse(reportId, out var id))
            return LegacyWire.NullStream();

        var result = await handler.ExecuteAsync(new GetReportClientQuery(id), cancellationToken);
        return result.ToLegacyResult(dto => LegacyWire.JsonStream(ReportClientJson.Serialize(reportId!, dto)));
    }

    /// <summary>
    /// jqGrid's _search and nd, and the later asOf, are accepted and ignored, as in the original.
    /// Unparseable typed values fail (the original's exact response for that case was never captured).
    /// </summary>
    [HttpGet("Report_GetData")]
    public async Task<IActionResult> ReportGetData(
        [FromServices] IGetReportDataQueryHandler handler,
        [FromQuery] Guid reportId,
        [FromQuery] Guid userId,
        [FromQuery] int rows,
        [FromQuery] int page,
        [FromQuery] string? sidx,
        [FromQuery] string? sord,
        CancellationToken cancellationToken)
    {
        // Unconvertible typed parameters never reach here: LegacyBindingFilter answers them with the host's error page.
        var result = await handler.ExecuteAsync(new GetReportDataQuery(reportId, userId, page, sidx, sord), cancellationToken);
        return result.ToLegacyResult(table => LegacyWire.JsonStream(ReportDataJson.Build(table, page, rows)));
    }

    /// <summary>Invalid ids return "" (typed string through the data-contract serializer).</summary>
    [HttpGet("Report_GetExecutionStats")]
    public async Task<IActionResult> ReportGetExecutionStats(
        [FromServices] IGetReportExecutionStatsQueryHandler handler,
        [FromQuery] string? reportId,
        [FromQuery] string? userId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(reportId, out var id) || !Guid.TryParse(userId, out var user))
            return LegacyWire.Typed(string.Empty);

        var result = await handler.ExecuteAsync(new GetReportExecutionStatsQuery(id, user), cancellationToken);
        return result.ToLegacyResult(LegacyWire.Typed);
    }
}
