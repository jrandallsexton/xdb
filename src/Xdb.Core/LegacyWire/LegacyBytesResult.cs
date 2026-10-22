using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

namespace Xdb.Core.LegacyWire;

/// <summary>
/// Writes exactly the given status, Content-Type and bytes, nothing added (no charset appended, no formatter).
/// A null content type sends no Content-Type header.
/// </summary>
public sealed class LegacyBytesResult(int statusCode, string? contentType, byte[] body) : IActionResult
{
    public int StatusCode { get; } = statusCode;

    public string? ContentType { get; } = contentType;

    public byte[] Body { get; } = body;

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        response.StatusCode = StatusCode;
        if (ContentType is not null)
            response.ContentType = ContentType;
        response.ContentLength = Body.Length;
        if (Body.Length > 0)
            await response.Body.WriteAsync(Body, context.HttpContext.RequestAborted);
    }
}
