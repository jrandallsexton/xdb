using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Xdb.Api.Application.Assets.Queries;
using Xdb.Core.LegacyWire;

namespace Xdb.Api.Application.Assets.Legacy;

/// <summary>
/// A request body the original's serializer could not read. With <see cref="InnerMessage"/> (a wrapped member that
/// failed to convert) the host answered 400 naming the parameter; otherwise the 500 Request Error page.
/// </summary>
public sealed class LegacyBodyException(string message, string? innerMessage = null, string? parameter = null) : Exception(message)
{
    public string? InnerMessage { get; } = innerMessage;
    public string? Parameter { get; } = parameter;

    public IActionResult ToResult() =>
        InnerMessage is not null && Parameter is not null ? LegacyWire.FormatterError(Parameter, InnerMessage) : LegacyWire.RequestError();
}

/// <summary>
/// Reads JSON request bodies with the original serializer's (DCJS) leniency: numbers and booleans may be quoted, a
/// present null is accepted, unknown members are ignored. A member that cannot be read throws <see cref="LegacyBodyException"/>
/// carrying the serializer's message (measured against the original for Guid, Guid list, Boolean and enum members).
/// </summary>
public static class LegacyJsonBody
{
    private const string GuidListType =
        "System.Collections.Generic.List`1[[System.Guid, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]";

    /// <summary>
    /// The body as JSON, or an early result. A body the host's JSON reader rejects at its first token (a root that
    /// cannot start a value, or an object whose first member does not start with '"' or '}') is 400 with no body;
    /// any later syntax error is the 500 Request Error page (both measured).
    /// </summary>
    public static async Task<(JsonElement? Root, IActionResult? Error)> ReadAsync(HttpRequest request, CancellationToken cancellationToken, bool wrapped = false)
    {
        string body;
        using (var reader = new StreamReader(request.Body))
            body = await reader.ReadToEndAsync(cancellationToken);
        if (body.Length == 0)
            return (null, null);
        // A body without a Content-Type never reaches the operation (measured: the request-error page).
        if (string.IsNullOrEmpty(request.ContentType))
            return (null, LegacyWire.RequestError());
        // A body needs a JSON content type to be read at all; the host handed any other to its XML reader (measured
        // for wrapped and bare bodies: text/plain is the request-error page).
        if (!request.ContentType.Contains("json", StringComparison.OrdinalIgnoreCase))
            return (null, LegacyWire.RequestError());
        if (Malformed(body) is { } rejected)
            return (null, rejected);
        var root = JsonDocument.Parse(body).RootElement;
        // The host read bodies with the default reader quotas: a wrapped body holding a string longer than 8192
        // characters failed in the reader, and the host reset the connection (measured).
        if (wrapped && LongestString(root) > MaxStringContentLength)
            return (null, new LegacyConnectionReset());
        return (root.ValueKind == JsonValueKind.Null ? null : root, null);
    }

    /// <summary>The default XmlDictionaryReaderQuotas.MaxStringContentLength of the original's binding.</summary>
    private const int MaxStringContentLength = 8192;

    private static int LongestString(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString()!.Length,
        JsonValueKind.Object => element.EnumerateObject().Select(p => LongestString(p.Value)).DefaultIfEmpty(0).Max(),
        JsonValueKind.Array => element.EnumerateArray().Select(LongestString).DefaultIfEmpty(0).Max(),
        _ => 0
    };

    /// <summary>The host's answer to a body that is not valid JSON, or null when it parses.</summary>
    public static IActionResult? Malformed(string body)
    {
        try
        {
            JsonDocument.Parse(body).Dispose();
            return null;
        }
        catch (JsonException)
        {
            var i = SkipSpace(body, 0);
            var first = i < body.Length ? body[i] : '\0';
            var startsValue = first is '{' or '[' or '"' or '-' or 'n' or 't' or 'f' || char.IsAsciiDigit(first);
            var badFirstMember = first == '{' && SkipSpace(body, i + 1) is var j && (j >= body.Length || body[j] is not ('"' or '}'));
            return !startsValue || badFirstMember ? new LegacyBytesResult(400, null, []) : LegacyWire.RequestError();
        }
    }

    private static int SkipSpace(string text, int index)
    {
        while (index < text.Length && text[index] is ' ' or '\t' or '\r' or '\n')
            index++;
        return index;
    }

    public static string? Text(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Null => null,
        _ => element.GetRawText()
    };

    public static int Int(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number))
            return number;
        if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
            return number;
        throw new LegacyBodyException("Not an integer.", Cannot("System.Int32", element, "Int32"));
    }

    /// <summary>An enum member (read as its number); <paramref name="typeName"/> is the original's full enum type name.</summary>
    public static Func<JsonElement, int> Enum(string typeName) => element =>
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number))
            return number;
        if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
            return number;
        throw new LegacyBodyException("Not an enum number.", Cannot(typeName, element, "Int64"));
    };

    public static bool Bool(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.String when bool.TryParse(element.GetString()?.Trim(), out var value) => value,
        _ => throw new LegacyBodyException("Not a boolean.", Cannot("System.Boolean", element, "Boolean"))
    };

    public static Guid Guid(JsonElement element) =>
        element.ValueKind == JsonValueKind.String && System.Guid.TryParse(element.GetString(), out var id)
            ? id
            : throw new LegacyBodyException("Not a Guid.", Cannot("System.Guid", element, "Guid"));

    public static List<Guid>? GuidList(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Null:
                return null;
            case JsonValueKind.Array:
                var ids = new List<Guid>();
                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind is JsonValueKind.String or JsonValueKind.Number && System.Guid.TryParse(Text(item), out var id))
                        ids.Add(id);
                    else
                        throw new LegacyBodyException("Not a Guid.", Cannot(GuidListType, item, "Guid"));
                }
                return ids;
            case JsonValueKind.String:
                throw new LegacyBodyException("Not an array.", "Expecting state 'Element'.. Encountered 'Text'  with name '', namespace ''. ");
            default:
                throw new LegacyBodyException("Not an array.");
        }
    }

    /// <summary>A DCJS Dictionary&lt;Guid,string&gt;: [{"Key":..,"Value":..}]; a repeated or unreadable key fails (Request Error page).</summary>
    public static List<KeyValuePair<Guid, string>>? GuidDictionary(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null)
            return null;
        if (element.ValueKind != JsonValueKind.Array)
            throw new LegacyBodyException("Not an array.");
        var result = new List<KeyValuePair<Guid, string>>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("Key", out var key)
                || key.ValueKind != JsonValueKind.String || !System.Guid.TryParse(key.GetString(), out var id))
                throw new LegacyBodyException("Not a key/value pair.");
            if (result.Exists(p => p.Key == id))
                throw new LegacyBodyException("Duplicate key.");
            result.Add(new KeyValuePair<Guid, string>(id, item.TryGetProperty("Value", out var value) ? Text(value)! : null!));
        }
        return result;
    }

    /// <summary>A member of a wrapped body, or its default when absent; a failure is reported against the member's name.</summary>
    public static T Member<T>(JsonElement? root, string name, Func<JsonElement, T> read, T fallback)
    {
        if (root is not { ValueKind: JsonValueKind.Object } r || !r.TryGetProperty(name, out var member))
            return fallback;
        try
        {
            return read(member);
        }
        catch (LegacyBodyException e) when (e.InnerMessage is not null && e.Parameter is null)
        {
            throw new LegacyBodyException(e.Message, e.InnerMessage, name);
        }
    }

    private static string Cannot(string typeName, JsonElement value, string parseType) =>
        $"There was an error deserializing the object of type {typeName}. The value '{Text(value)}' cannot be parsed as the type '{parseType}'.";

    /// <summary>
    /// An AssetSearchClient (bare). Null for an empty body. Missing required members (assetTypeId, assetRequestType,
    /// filterName, filterExpression, filters; each filter's propertyId, operatorId, value, order) fail.
    /// </summary>
    public static AssetSearchClient? SearchClient(JsonElement? root)
    {
        if (root is null)
            return null;
        var r = root.Value;
        string[] required = ["assetTypeId", "assetRequestType", "filterName", "filterExpression", "filters"];
        if (r.ValueKind != JsonValueKind.Object || required.Any(name => !r.TryGetProperty(name, out _)))
            throw new LegacyBodyException("Missing a required member.");

        List<FilterInput>? filters = null;
        var filtersElement = r.GetProperty("filters");
        if (filtersElement.ValueKind == JsonValueKind.Array)
        {
            filters = [];
            foreach (var filter in filtersElement.EnumerateArray())
            {
                if (filter.ValueKind == JsonValueKind.Null)
                {
                    filters.Add(null!);
                    continue;
                }
                if (filter.ValueKind != JsonValueKind.Object || new[] { "propertyId", "operatorId", "value", "order" }.Any(name => !filter.TryGetProperty(name, out _)))
                    throw new LegacyBodyException("Missing a required filter member.");
                filters.Add(new FilterInput(Text(filter.GetProperty("propertyId")), Int(filter.GetProperty("operatorId")), Text(filter.GetProperty("value")),
                    Int(filter.GetProperty("order"))));
            }
        }
        else if (filtersElement.ValueKind != JsonValueKind.Null)
        {
            throw new LegacyBodyException("filters is not an array.");
        }

        // ChartCreationClient's optional members must still be readable when present.
        var maxRecordsValue = r.TryGetProperty("maxRecords", out var maxRecords) ? Int(maxRecords) : 0;
        var isDirtyValue = r.TryGetProperty("isDirty", out var isDirty) && Bool(isDirty);

        return new AssetSearchClient(
            r.TryGetProperty("id", out var id) ? Text(id) : null,
            Text(r.GetProperty("assetTypeId")),
            Int(r.GetProperty("assetRequestType")),
            Text(r.GetProperty("filterExpression")),
            r.TryGetProperty("propertyId", out var propertyId) ? Text(propertyId) : null,
            filters,
            Text(r.GetProperty("filterName")),
            maxRecordsValue,
            isDirtyValue);
    }

    /// <summary>A DashboardClient (bare): every member required at every depth; null for an empty or null body.</summary>
    public static DashboardBody? Dashboard(JsonElement? root)
    {
        if (root is null)
            return null;
        var r = root.Value;
        if (r.ValueKind != JsonValueKind.Object || new[] { "id", "caption", "index", "items", "memberId" }.Any(name => !r.TryGetProperty(name, out _)))
            throw new LegacyBodyException("Missing a required member.");
        List<DashboardItemBody>? items = null;
        var itemsElement = r.GetProperty("items");
        if (itemsElement.ValueKind == JsonValueKind.Array)
            items = [.. itemsElement.EnumerateArray().Select(item => DashboardItem(item)!)];
        else if (itemsElement.ValueKind != JsonValueKind.Null)
            throw new LegacyBodyException("items is not an array.");
        return new DashboardBody(Text(r.GetProperty("id")), Text(r.GetProperty("caption")), Int(r.GetProperty("index")), items, Text(r.GetProperty("memberId")));
    }

    /// <summary>A DashboardItemClient (bare or inside a dashboard): every member required; itemType read as a number.</summary>
    public static DashboardItemBody? DashboardItem(JsonElement? element)
    {
        if (element is null || element.Value.ValueKind == JsonValueKind.Null)
            return null;
        var e = element.Value;
        if (e.ValueKind != JsonValueKind.Object
            || new[] { "id", "dashboardId", "index", "itemType", "chartId", "title", "subtitle" }.Any(name => !e.TryGetProperty(name, out _)))
            throw new LegacyBodyException("Missing a required item member.");
        return new DashboardItemBody(Text(e.GetProperty("id")), Text(e.GetProperty("dashboardId")), Int(e.GetProperty("index")), Int(e.GetProperty("itemType")),
            Text(e.GetProperty("chartId")), Text(e.GetProperty("title")), Text(e.GetProperty("subtitle")));
    }

    /// <summary>
    /// A top-level KeyValuePair&lt;string,string&gt; body: the serializer's adapter requires lowercase key and value. An
    /// empty or null body is the default pair (both null).
    /// </summary>
    public static (string? Key, string? Value) KeyValue(JsonElement? root)
    {
        if (root is null)
            return (null, null);
        var r = root.Value;
        if (r.ValueKind != JsonValueKind.Object || !r.TryGetProperty("key", out var key) || !r.TryGetProperty("value", out var value))
            throw new LegacyBodyException("Missing a required member.");
        return (Text(key), Text(value));
    }
}

public sealed record AssetSearchClient(string? Id, string? AssetTypeId, int AssetRequestType, string? FilterExpression, string? PropertyId, List<FilterInput>? Filters,
    string? FilterName = null, int MaxRecords = 0, bool IsDirty = false);

public sealed record DashboardBody(string? Id, string? Caption, int Index, List<DashboardItemBody>? Items, string? MemberId);

public sealed record DashboardItemBody(string? Id, string? DashboardId, int Index, int ItemType, string? ChartId, string? Title, string? Subtitle);
