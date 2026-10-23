using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Xdb.Api.Application.Reports.Export;
using Xdb.Core.LegacyWire;

namespace Xdb.Api.Application.Reports.Legacy;

/// <summary>Report_Export ("Export to Excel"), an XML-format operation in the original: a failure is the Request Error page.</summary>
[Route(LegacyWire.BaseRoute)]
public sealed class ReportExportWireController : LegacyWireControllerBase
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet("Report_Export")]
    [LegacyXmlOperation]
    public async Task<IActionResult> ReportExport([FromServices] IReportExportHandler handler, [FromQuery] Guid reportId, [FromQuery] Guid userId,
        [FromQuery] string? groupColName, [FromQuery] bool forBulkUpdate, CancellationToken cancellationToken) =>
        (await handler.ExportAsync(reportId, userId, groupColName, forBulkUpdate, cancellationToken)).ToLegacyResult(workbook =>
            new LegacyBytesResult(200, XlsxContentType, workbook.Data, new Dictionary<string, string>
            {
                ["Pragma"] = "no-cache",
                ["Expires"] = "0",
                ["Content-Disposition"] = "attachment; filename=" + workbook.FileName,
                ["Content-Description"] = "File Transfer"
            }));
}
