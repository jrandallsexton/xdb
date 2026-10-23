using System;
using System.Collections.Generic;
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

    /// <summary>Content type of the original's other hand-built stream helper, used by most metadata reads.</summary>
    public const string StreamHtmlContentType = "text/html";

    /// <summary>
    /// The host's page when a typed query parameter cannot be converted (unparseable, empty, duplicated, or an
    /// absent enum): HTTP 500, text/html, exactly 1047 ASCII bytes.
    /// </summary>
    public static readonly string RequestErrorPage = ErrorPage("InternalServerError",
        "The server encountered an error processing the request. Please see the server logs for more details.");

    /// <summary>
    /// The host's XHTML error page: status name and details (XML-escaped), then the fixed 512-character padding comment.
    /// </summary>
    public static string ErrorPage(string statusName, string details) =>
        "<html version=\"-//W3C//DTD XHTML 2.0//EN\" xml:lang=\"en\" xsi:schemaLocation=\"http://www.w3.org/1999/xhtml http://www.w3.org/MarkUp/SCHEMA/xhtml2.xsd\" xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><HEAD><TITLE>Request Error</TITLE></HEAD><BODY><DIV id=\"content\"><P class=\"heading1\"><B>Error Status Code:</B> '"
        + statusName + "'</P><P><B>Details: </B>" + details.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + "</P>"
        + "<!-- Padding " + new string('x', 512) + "--></DIV></BODY></html>";

    /// <summary>
    /// The host's page for a known operation called with the wrong HTTP verb (status 405, Content-Type text/html). It
    /// links the service help page by the original machine's address, which is configuration.
    /// </summary>
    public static string MethodNotAllowedPage(string helpUrl) =>
        "<html version=\"-//W3C//DTD XHTML 2.0//EN\" xml:lang=\"en\" xsi:schemaLocation=\"http://www.w3.org/1999/xhtml http://www.w3.org/MarkUp/SCHEMA/xhtml2.xsd\" xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><HEAD><TITLE>Request Error</TITLE></HEAD><BODY><DIV id=\"content\"><P class=\"heading1\"><B>Error Description:</B> 'HTTP Method not allowed'</P><P><B>This may be because an invalid URI or HTTP method was specified. Please see the <A HREF=\""
        + helpUrl + "\">service help page</A> for constructing valid requests to the service.</B></P>"
        + "<!-- Padding " + new string('x', 512) + "--></DIV></BODY></html>";

    /// <summary>
    /// The host's response when a wrapped request body member cannot be deserialized: HTTP 400 with the formatter's
    /// message naming the parameter and the inner serializer message.
    /// </summary>
    public static IActionResult FormatterError(string parameter, string innerMessage) =>
        new LegacyBytesResult(400, StreamHtmlContentType, Encoding.ASCII.GetBytes(ErrorPage("BadRequest",
            "The formatter threw an exception while trying to deserialize the message: There was an error while trying to deserialize parameter http://tempuri.org/:"
            + parameter + ". The InnerException message was '" + innerMessage + "'.  Please see InnerException for more details.")));

    /// <summary>The hosting framework's body for any unhandled exception (HTTP 500, no ErrorId header).</summary>
    public const string GenericFaultBody =
        "{\"Detail\":\"The server encountered an error processing the request. Please see the server logs for more details.\"}";

    /// <summary>
    /// A hand-built JSON string, written the way the original did: ASCII bytes (every non-ASCII character
    /// becomes '?'), Content-Type "application/json" with no charset.
    /// </summary>
    public static IActionResult JsonStream(string json) =>
        new LegacyBytesResult(200, StreamJsonContentType, Encoding.ASCII.GetBytes(json));

    /// <summary>A hand-built string written with the original's other helper: ASCII bytes, Content-Type text/html.</summary>
    public static IActionResult HtmlStream(string text) =>
        new LegacyBytesResult(200, StreamHtmlContentType, Encoding.ASCII.GetBytes(text));

    /// <summary>
    /// The one service-level helper that labelled ASCII bytes as UTF-8 JSON ("application/json; charset=utf-8").
    /// </summary>
    public static IActionResult JsonStreamLabelledUtf8(string json) =>
        new LegacyBytesResult(200, TypedJsonContentType, Encoding.ASCII.GetBytes(json));

    /// <summary>A typed operation that returned null: 200, empty body, no Content-Type (WCF did not serialize it).</summary>
    public static IActionResult NullTyped() => new LegacyBytesResult(200, null, []);

    /// <summary>A typed value, or <see cref="NullTyped"/> when it is null.</summary>
    public static IActionResult TypedOrNull<T>(T? value) where T : class => value is null ? NullTyped() : Typed(value);

    /// <summary>The host's response when a typed query parameter cannot be converted.</summary>
    public static IActionResult RequestError() =>
        new LegacyBytesResult(500, StreamHtmlContentType, Encoding.ASCII.GetBytes(RequestErrorPage));

    /// <summary>
    /// The original's HandleLogicalException response (measured): 500, Content-Type exactly "application/json", the
    /// saved error's id in an ErrorId header, and the operation's typed false as the body.
    /// </summary>
    public static IActionResult LogicalFailure(string errorId) =>
        new LegacyBytesResult(500, StreamJsonContentType, "false"u8.ToArray(), new Dictionary<string, string> { ["ErrorId"] = errorId });

    /// <summary>An operation that returned a null Stream: 200 with an empty body.</summary>
    /// <remarks>Measured byte- and header-identical to <see cref="NullTyped"/>.</remarks>
    public static IActionResult NullStream() => NullTyped();

    /// <summary>An Rss20FeedFormatter return (see <see cref="LegacyRss"/>).</summary>
    public static IActionResult Rss(RssChannel channel) => new LegacyBytesResult(200, LegacyRss.ContentType, LegacyRss.Write(channel));

    /// <summary>A typed return value, serialized by DataContractJsonSerializer as WCF did (UTF-8, no BOM).</summary>
    public static IActionResult Typed<T>(T value) => TypedBytesResult(TypedBytes(value));

    /// <summary>The exact body <see cref="Typed{T}"/> sends, for callers that also store it.</summary>
    public static byte[] TypedBytes<T>(T value)
    {
        var serializer = new DataContractJsonSerializer(typeof(T));
        using var buffer = new MemoryStream();
        serializer.WriteObject(buffer, value);
        return Encoding.UTF8.GetBytes(LongControlEscapes(Encoding.UTF8.GetString(buffer.ToArray())));
    }

    /// <summary>A body produced earlier by <see cref="TypedBytes{T}"/>, sent as a typed response.</summary>
    public static IActionResult TypedBytesResult(byte[] body) => new LegacyBytesResult(200, TypedJsonContentType, body);

    /// <summary>
    /// .NET Framework's serializer wrote every control character as \u00XX (lowercase hex); .NET 10's writes
    /// \b \t \n \f \r as short escapes. Rewrites those five; any other escape (including "\\") is copied as a pair.
    /// </summary>
    internal static string LongControlEscapes(string json)
    {
        if (json.IndexOf('\\') < 0)
            return json;
        var sb = new StringBuilder(json.Length + 16);
        for (var i = 0; i < json.Length; i++)
        {
            var c = json[i];
            if (c != '\\' || i + 1 >= json.Length)
            {
                sb.Append(c);
                continue;
            }
            var next = json[++i];
            sb.Append(next switch
            {
                'b' => "\\u0008",
                't' => "\\u0009",
                'n' => "\\u000a",
                'f' => "\\u000c",
                'r' => "\\u000d",
                _ => string.Concat("\\", next.ToString())
            });
        }
        return sb.ToString();
    }

    public static IActionResult Fault() =>
        new LegacyBytesResult(500, StreamJsonContentType, Encoding.ASCII.GetBytes(GenericFaultBody));
}
