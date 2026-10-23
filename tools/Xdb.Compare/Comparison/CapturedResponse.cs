using System;
using System.Text;

namespace Xdb.Compare.Comparison;

/// <summary>A response as seen on the wire.</summary>
/// <param name="ContentDisposition">The raw Content-Disposition header (file streams), compared exactly.</param>
public sealed record CapturedResponse(int Status, string? ContentType, byte[] Body, string? ErrorIdHeader = null, string? ContentDisposition = null)
{
    public static CapturedResponse FromText(int status, string? contentType, string? body, string? errorIdHeader = null) =>
        new(status, contentType, body is null ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(body), errorIdHeader);
}
