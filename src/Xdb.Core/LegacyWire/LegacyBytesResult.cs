using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

namespace Xdb.Core.LegacyWire;

/// <summary>
/// Writes exactly the given status, Content-Type, extra headers and bytes, nothing added (no charset appended, no
/// formatter). A null content type sends no Content-Type header.
/// </summary>
public sealed class LegacyBytesResult(int statusCode, string? contentType, byte[] body, IReadOnlyDictionary<string, string>? headers = null) : IActionResult
{
    public int StatusCode { get; } = statusCode;

    public string? ContentType { get; } = contentType;

    public byte[] Body { get; } = body;

    public IReadOnlyDictionary<string, string> Headers { get; } = headers ?? new Dictionary<string, string>();

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        response.StatusCode = StatusCode;
        if (ContentType is not null)
            response.ContentType = ContentType;
        foreach (var (name, value) in Headers)
            response.Headers[name] = value;
        response.ContentLength = Body.Length;
        if (Body.Length > 0)
            await response.Body.WriteAsync(Body, context.HttpContext.RequestAborted);
    }
}
