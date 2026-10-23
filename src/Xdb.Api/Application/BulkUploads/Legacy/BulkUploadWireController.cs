using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;

using Xdb.Core.LegacyWire;

namespace Xdb.Api.Application.BulkUploads.Legacy;

/// <summary>The original service's bulk upload reads (spec M8-B section 1).</summary>
[Route(LegacyWire.BaseRoute)]
public sealed class BulkUploadWireController : LegacyWireControllerBase
{
    [HttpGet("BulkUpload_Get")]
    public async Task<IActionResult> BulkUploadGet([FromServices] IBulkUploadQueryHandler handler, [FromQuery] Guid uploadId, CancellationToken cancellationToken) =>
        (await handler.GetUploadAsync(uploadId, cancellationToken)).ToLegacyResult(LegacyWire.TypedOrNull);

    [HttpGet("BulkUploads_Get")]
    public async Task<IActionResult> BulkUploadsGet([FromServices] IBulkUploadQueryHandler handler, [FromQuery] string? userId, [FromQuery] int filter,
        CancellationToken cancellationToken) =>
        (await handler.GetUploadsAsync(userId, filter, cancellationToken)).ToLegacyResult(uploads => LegacyWire.HtmlStream(LegacyFormatters.FormatDictionary(uploads)));

    /// <summary>startPos is bound (a bad one is the request-error page) and discarded.</summary>
    [HttpGet("BulkUploadLogs_Get")]
    public async Task<IActionResult> BulkUploadLogsGet([FromServices] IBulkUploadQueryHandler handler, [FromQuery] Guid bulkUploadId, [FromQuery] bool desc,
        [FromQuery] int startPos, CancellationToken cancellationToken) =>
        (await handler.GetLogsAsync(bulkUploadId, desc, cancellationToken)).ToLegacyResult(logs => LegacyWire.HtmlStream(JsonConvert.SerializeObject(logs)));
}
