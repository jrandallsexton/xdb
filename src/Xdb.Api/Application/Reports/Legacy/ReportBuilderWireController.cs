using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Common.Identity;
using Xdb.Api.Application.Reports.Commands;
using Xdb.Api.Application.Reports.Queries;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;
using Xdb.Core.LegacyWire;

namespace Xdb.Api.Application.Reports.Legacy;

/// <summary>
/// The original service's report builder and administration operations (the viewer's are in
/// <see cref="ReportsWireController"/>). Report_Save and MigrateReport answer logical failures as the original's
/// HandleLogicalException did; elsewhere they fault.
/// </summary>
[Route(LegacyWire.BaseRoute)]
public sealed class ReportBuilderWireController : LegacyWireControllerBase
{
    // ---- Reads ----

    [HttpGet("Report_Get")]
    public async Task<IActionResult> ReportGet([FromServices] IGetReportQueryHandler handler, [FromQuery] Guid userId, [FromQuery] Guid reportId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetReportQuery(reportId), cancellationToken)).ToLegacyResult(report => LegacyWire.Typed(report));

    [HttpGet("Report_GetAssetTypeId")]
    public async Task<IActionResult> ReportGetAssetTypeId([FromServices] IReportStore store, [FromQuery] Guid reportId, CancellationToken cancellationToken) =>
        LegacyWire.Typed(await store.GetAssetTypeIdAsync(reportId, cancellationToken));

    [HttpGet("Report_GetFilterString")]
    public async Task<IActionResult> ReportGetFilterString([FromServices] IGetReportFilterStringQueryHandler handler, [FromQuery] Guid reportId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetReportFilterStringQuery(reportId), cancellationToken)).ToLegacyResult(text => LegacyWire.Typed(text));

    [HttpGet("ReportGroupColumns_Get")]
    public async Task<IActionResult> ReportGroupColumnsGet([FromServices] IGetReportGroupColumnsQueryHandler handler, [FromQuery] Guid reportId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetReportGroupColumnsQuery(reportId), cancellationToken))
            .ToLegacyResult(values => LegacyWire.HtmlStream(LegacyFormatters.FormatDictionary(values)));

    [HttpGet("Report_GetUnusedAvailableProperties")]
    public async Task<IActionResult> ReportGetUnusedAvailableProperties([FromServices] IGetUnusedReportPropertiesQueryHandler handler, [FromQuery] Guid reportId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetUnusedReportPropertiesQuery(reportId), cancellationToken))
            .ToLegacyResult(values => LegacyWire.JsonStream(LegacyFormatters.FormatDictionary(values)));

    [HttpGet("Reports_Get")]
    public async Task<IActionResult> ReportsGet([FromServices] IGetReportsDictionaryQueryHandler handler, [FromQuery] Guid userId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetReportsDictionaryQuery(userId), cancellationToken))
            .ToLegacyResult(values => LegacyWire.HtmlStream(LegacyFormatters.FormatDictionary(values)));

    [HttpGet("Reports_GetForBuilder")]
    public async Task<IActionResult> ReportsGetForBuilder([FromServices] IGetReportsDictionaryQueryHandler handler, [FromQuery] Guid userId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetReportsDictionaryQuery(userId), cancellationToken))
            .ToLegacyResult(values => LegacyWire.JsonStream(LegacyFormatters.FormatDictionary(values)));

    /// <summary>Dead in the data: its table no longer exists, so every bound call faults.</summary>
    [HttpGet("Reports_GetSaved")]
    public IActionResult ReportsGetSaved([FromQuery] Guid userId, [FromQuery] bool includePublic) => LegacyWire.Fault();

    [HttpGet("ReportCache_Get")]
    public async Task<IActionResult> ReportCacheGet([FromServices] IGetReportCacheQueryHandler handler, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetReportCacheQuery(), cancellationToken)).ToLegacyResult(entries => LegacyWire.Typed(entries));

    [HttpGet("Report_Create")]
    public IActionResult ReportCreate([FromServices] IDateTimeProvider dateTimeProvider, [FromQuery] Guid userId) =>
        LegacyWire.Typed(new ReportContract { Id = Guid.NewGuid(), Created = dateTimeProvider.UtcNow().ToLocalTime(), CreatedBy = userId });

    [HttpGet("Report_Clone")]
    public async Task<IActionResult> ReportClone([FromServices] ICloneReportQueryHandler handler, [FromQuery] Guid reportId, [FromQuery] Guid userId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new CloneReportQuery(reportId, userId), cancellationToken)).ToLegacyResult(report => LegacyWire.Typed(report));

    // ---- Writes ----

    [HttpGet("Report_ClearCached")]
    public async Task<IActionResult> ReportClearCached([FromServices] IReportBookmarkCommandHandler handler, [FromQuery] Guid reportId, CancellationToken cancellationToken) =>
        (await handler.ClearCacheAsync(reportId, cancellationToken)).ToLegacyResult(ok => LegacyWire.Typed(ok));

    [HttpGet("Bookmark_AddForReport")]
    public async Task<IActionResult> BookmarkAddForReport([FromServices] IReportBookmarkCommandHandler handler, [FromQuery] Guid userId, [FromQuery] Guid reportId,
        [FromQuery] bool showOnHome, CancellationToken cancellationToken) =>
        (await handler.AddAsync(new AddReportBookmarkCommand(userId, reportId, showOnHome), cancellationToken)).ToLegacyResult(ok => LegacyWire.Typed(ok));

    [HttpGet("Bookmark_RemoveForReport")]
    public async Task<IActionResult> BookmarkRemoveForReport([FromServices] IReportBookmarkCommandHandler handler, [FromQuery] Guid userId, [FromQuery] Guid reportId,
        CancellationToken cancellationToken) =>
        (await handler.RemoveAsync(userId, reportId, cancellationToken)).ToLegacyResult(ok => LegacyWire.Typed(ok));

    [HttpPost("Report_Save")]
    public async Task<IActionResult> ReportSave([FromServices] ISaveReportCommandHandler handler, [FromServices] LegacyErrorLog errors, CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken);
        if (error is not null)
            return error;
        ReportContract? report;
        try
        {
            report = ReportBodies.Report(root);
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        return await LogicalAsync(errors, async () => await handler.ExecuteAsync(new SaveReportCommand(report), cancellationToken), cancellationToken);
    }

    [HttpGet("Report_CreateFromMemberFilter")]
    public async Task<IActionResult> ReportCreateFromMemberFilter([FromServices] ICreateReportFromMemberFilterCommandHandler handler, [FromQuery] string? filterId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new CreateReportFromMemberFilterCommand(filterId), cancellationToken)).ToLegacyResult(ok => LegacyWire.Typed(ok));

    [HttpGet("Report_Delete")]
    public async Task<IActionResult> ReportDelete([FromServices] IDeleteReportCommandHandler handler, [FromQuery] Guid reportId, [FromQuery] Guid userId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new DeleteReportCommand(reportId, userId), cancellationToken)).ToLegacyResult(ok => LegacyWire.Typed(ok));

    [HttpGet("Report_Kill")]
    public async Task<IActionResult> ReportKill([FromServices] IKillReportCommandHandler handler, [FromQuery] Guid reportId, [FromQuery] Guid userId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new KillReportCommand(reportId, userId), cancellationToken)).ToLegacyResult(ok => LegacyWire.Typed(ok));

    /// <summary>The body is a KeyValuePair&lt;Guid, application instance&gt;: lowercase key and value, a Guid and a number.</summary>
    [HttpPost("MigrateReport")]
    public async Task<IActionResult> MigrateReport([FromServices] IMigrateReportCommandHandler handler, [FromServices] LegacyErrorLog errors,
        CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken);
        if (error is not null)
            return error;
        string? key, target;
        try
        {
            (key, var value) = LegacyJsonBody.KeyValue(root);
            if (key is not null && !Guid.TryParse(key, out _))
                throw new LegacyBodyException("Not a Guid.");
            target = value is null ? "0" : int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
                ? number.ToString(CultureInfo.InvariantCulture)
                : throw new LegacyBodyException("Not a number.");
            key ??= Guid.Empty.ToString("D");
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        return await LogicalAsync(errors, async () => await handler.ExecuteAsync(new MigrateReportCommand(key, target), cancellationToken), cancellationToken);
    }

    /// <summary>HandleLogicalException: a logical failure saves an Errors row and answers 500 with its id; anything else faults.</summary>
    private static async Task<IActionResult> LogicalAsync(LegacyErrorLog errors, Func<Task<Result<bool>>> run, CancellationToken cancellationToken)
    {
        try
        {
            return (await run()).ToLegacyResult(ok => LegacyWire.Typed(ok));
        }
        catch (LegacyLogicalException e)
        {
            return LegacyWire.LogicalFailure(await errors.SaveAsync(e.Message, cancellationToken));
        }
    }
}
