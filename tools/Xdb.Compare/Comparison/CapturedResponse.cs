using System;
using System.Text;

namespace Xdb.Compare.Comparison;

/// <summary>A response as seen on the wire.</summary>
public sealed record CapturedResponse(int Status, string? ContentType, byte[] Body, string? ErrorIdHeader = null)
{
    public static CapturedResponse FromText(int status, string? contentType, string? body, string? errorIdHeader = null) =>
        new(status, contentType, body is null ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(body), errorIdHeader);
}
