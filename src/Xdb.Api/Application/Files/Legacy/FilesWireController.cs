using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Xdb.Core.LegacyWire;

namespace Xdb.Api.Application.Files.Legacy;

/// <summary>
/// The original service's image and document streams and uploads (spec M8-F). The upload operations declared no
/// response format (XML): any failure in them is the Request Error page. Their body is read raw, whatever its
/// Content-Type.
/// </summary>
[Route(LegacyWire.BaseRoute)]
public sealed class FilesWireController : LegacyWireControllerBase
{
    /// <summary>The stored bytes with the host's headers: application/octet-stream when the type has no MIME row; the disposition's trailing white space trimmed (IIS).</summary>
    [HttpGet("getImageStream")]
    public async Task<IActionResult> GetImageStream([FromServices] IFileQueryHandler handler, [FromQuery] Guid imageId, [FromQuery] bool createThumbnail,
        [FromQuery] int maxSize, CancellationToken cancellationToken) =>
        (await handler.GetStreamAsync(imageId, createThumbnail, maxSize, cancellationToken)).ToLegacyResult(file => file is null
            ? LegacyWire.NullStream()
            : new LegacyBytesResult(200, file.MimeType.Length == 0 ? "application/octet-stream" : file.MimeType, file.Data,
                new Dictionary<string, string> { ["Content-disposition"] = ("inline; filename=" + file.Name).TrimEnd() }));

    [HttpGet("Image_GetDimensions")]
    public async Task<IActionResult> ImageGetDimensions([FromServices] IFileQueryHandler handler, [FromQuery] Guid imageId, CancellationToken cancellationToken) =>
        (await handler.GetDimensionsAsync(imageId, cancellationToken)).ToLegacyResult(text => LegacyWire.Typed(text));

    [HttpGet("getUploadStatus")]
    public async Task<IActionResult> GetUploadStatus([FromServices] IFileQueryHandler handler, [FromQuery] string? id, CancellationToken cancellationToken) =>
        (await handler.GetUploadStatusAsync(id, cancellationToken)).ToLegacyResult(text => LegacyWire.Typed(text));

    [HttpGet("prepareUploadObjectPropertyValue")]
    public async Task<IActionResult> PrepareUploadObjectPropertyValue([FromServices] IUploadCommandHandler handler, [FromQuery] Guid propertyId,
        [FromQuery] Guid objectId, [FromQuery] string? title, [FromQuery] string? description, [FromQuery] string? fileName, CancellationToken cancellationToken) =>
        (await handler.PrepareAsync(propertyId, objectId, title, description, fileName, cancellationToken)).ToLegacyResult(done => LegacyWire.Typed(done));

    [HttpPost("Image_Upload")]
    [LegacyXmlOperation]
    [LegacyRawBody]
    public async Task<IActionResult> ImageUpload([FromServices] IUploadCommandHandler handler, [FromQuery] Guid imageId, [FromQuery] Guid userId,
        [FromQuery] string? fileName, [FromQuery] Guid propertyId, CancellationToken cancellationToken) =>
        (await handler.UploadImageAsync(await BodyAsync(cancellationToken), imageId, userId, fileName, propertyId, cancellationToken)).ToLegacyResult(LegacyWire.HtmlStream);

    [HttpPost("UploadDocument")]
    [LegacyXmlOperation]
    [LegacyRawBody]
    public async Task<IActionResult> UploadDocument([FromServices] IUploadCommandHandler handler, [FromQuery] Guid documentId, [FromQuery] Guid userId,
        [FromQuery] string? fileName, [FromQuery] Guid propertyId, CancellationToken cancellationToken) =>
        (await handler.UploadDocumentAsync(await BodyAsync(cancellationToken), documentId, userId, fileName, propertyId, cancellationToken))
            .ToLegacyResult(LegacyWire.HtmlStream);

    /// <summary>
    /// The bulk workbook upload. DEFERRED with bulk uploads (owner decision 2026-10-23): its workbook reading needed a
    /// Windows-only provider (Jet). The port faults (the Request Error page) and writes nothing.
    /// </summary>
    [HttpPost("Upload")]
    [LegacyXmlOperation]
    [LegacyRawBody]
    public IActionResult Upload([FromQuery] string? uploadId, [FromQuery] Guid userId) =>
        throw new NotSupportedException("Deferred: bulk uploads (workbook reading needs a Windows-only dependency).");

    /// <summary>
    /// The request body as the operation read it. On the original host a JSON-typed request never reached these
    /// operations (measured: no upload counter was set; the Request Error page was answered).
    /// </summary>
    private async Task<byte[]> BodyAsync(CancellationToken cancellationToken)
    {
        if (Request.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
            throw new InvalidOperationException("A JSON request body does not reach a Stream operation.");
        using var buffer = new MemoryStream();
        await Request.Body.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }
}
