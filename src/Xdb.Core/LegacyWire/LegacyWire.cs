using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

using Microsoft.AspNetCore.Mvc;

namespace Xdb.Core.LegacyWire;

/// <summary>
/// Responses shaped exactly like the original WCF REST service's, for the unmodified original UI.
/// Only legacy wire controllers use these; handlers never know the legacy wire exists.
/// </summary>
public static class LegacyWire
{
    /// <summary>Route prefix of the legacy surface. Neutral by design; the original UI's private config points here.</summary>
    public const string BaseRoute = "legacy/Service.svc";

    /// <summary>Content type of operations that returned a hand-built JSON Stream (no charset).</summary>
    public const string StreamJsonContentType = "application/json";

    /// <summary>Content type of operations whose typed return went through WCF's JSON serializer.</summary>
    public const string TypedJsonContentType = "application/json; charset=utf-8";

    /// <summary>The hosting framework's body for any unhandled exception (HTTP 500, no ErrorId header).</summary>
    public const string GenericFaultBody =
        "{\"Detail\":\"The server encountered an error processing the request. Please see the server logs for more details.\"}";

    /// <summary>
    /// A hand-built JSON string, written the way the original did: ASCII bytes (every non-ASCII character
    /// becomes '?'), Content-Type "application/json" with no charset.
    /// </summary>
    public static IActionResult JsonStream(string json) =>
        new FileContentResult(Encoding.ASCII.GetBytes(json), StreamJsonContentType);

    /// <summary>An operation that returned a null Stream: 200 with an empty body.</summary>
    public static IActionResult NullStream() => new EmptyResult();

    /// <summary>A typed return value, serialized by DataContractJsonSerializer as WCF did (UTF-8, no BOM).</summary>
    public static IActionResult Typed<T>(T value)
    {
        var serializer = new DataContractJsonSerializer(typeof(T));
        using var buffer = new MemoryStream();
        serializer.WriteObject(buffer, value);
        return new FileContentResult(buffer.ToArray(), TypedJsonContentType);
    }

    public static IActionResult Fault() =>
        new ContentResult { StatusCode = 500, ContentType = StreamJsonContentType, Content = GenericFaultBody };
}
