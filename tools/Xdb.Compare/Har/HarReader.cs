using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Xdb.Compare.Har;

/// <summary>Extracts the service calls from a HAR capture (any request whose path contains "/Service.svc/").</summary>
public static class HarReader
{
    private const string ServiceMarker = "/Service.svc/";

    public static IReadOnlyList<RecordedCall> Read(string harPath) => Parse(File.ReadAllText(harPath));

    public static IReadOnlyList<RecordedCall> Parse(string harJson)
    {
        using var doc = JsonDocument.Parse(harJson);
        var calls = new List<RecordedCall>();
        var index = 0;
        foreach (var entry in doc.RootElement.GetProperty("log").GetProperty("entries").EnumerateArray())
        {
            var request = entry.GetProperty("request");
            var url = request.GetProperty("url").GetString() ?? string.Empty;
            var at = url.IndexOf(ServiceMarker, StringComparison.OrdinalIgnoreCase);
            if (at < 0)
                continue;

            var relative = url[(at + ServiceMarker.Length)..];
            var operation = relative.Split('?', 2)[0];
            var response = entry.GetProperty("response");
            var content = response.TryGetProperty("content", out var c) ? c : default;

            calls.Add(new RecordedCall(
                index++,
                operation,
                request.GetProperty("method").GetString() ?? "GET",
                relative,
                request.TryGetProperty("postData", out var post) && post.TryGetProperty("text", out var text) ? text.GetString() : null,
                Header(request, "Content-Type") ?? PostMimeType(request),
                response.GetProperty("status").GetInt32(),
                Header(response, "Content-Type"),
                content.ValueKind == JsonValueKind.Object ? Body(content) : null,
                BinaryPostData(request)));
        }
        return calls;
    }

    private static string? Header(JsonElement message, string name) =>
        message.TryGetProperty("headers", out var headers)
            ? headers.EnumerateArray()
                .Where(h => string.Equals(h.GetProperty("name").GetString(), name, StringComparison.OrdinalIgnoreCase))
                .Select(h => h.GetProperty("value").GetString())
                .FirstOrDefault()
            : null;

    /// <summary>A request body recorded as base64 (binary uploads): sent as these bytes, never re-encoded as text.</summary>
    private static byte[]? BinaryPostData(JsonElement request) =>
        request.TryGetProperty("postData", out var post) && post.TryGetProperty("encoding", out var encoding)
            && string.Equals(encoding.GetString(), "base64", StringComparison.OrdinalIgnoreCase) && post.TryGetProperty("text", out var text)
            ? Convert.FromBase64String(text.GetString() ?? string.Empty)
            : null;

    /// <summary>A request body's type as HAR records it (postData.mimeType), for captures without request headers.</summary>
    private static string? PostMimeType(JsonElement request) =>
        request.TryGetProperty("postData", out var post) && post.TryGetProperty("mimeType", out var mime) && mime.ValueKind == JsonValueKind.String
            && !string.IsNullOrEmpty(mime.GetString())
            ? mime.GetString()
            : null;

    private static string? Body(JsonElement content)
    {
        if (!content.TryGetProperty("text", out var text) || text.ValueKind != JsonValueKind.String)
            return null;
        var value = text.GetString()!;
        return content.TryGetProperty("encoding", out var enc) && enc.GetString() == "base64"
            ? Encoding.UTF8.GetString(Convert.FromBase64String(value))
            : value;
    }
}
