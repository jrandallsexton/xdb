using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Xdb.Compare.Comparison;

/// <summary>
/// Compares the original's response with the port's. Byte-identical is the goal; when bytes differ and both
/// bodies are JSON, differences are located token by token using raw token text, so escaping counts.
/// </summary>
public static class ResponseComparer
{
    public static ComparisonResult Compare(CapturedResponse legacy, CapturedResponse port, IgnoreRules ignore) =>
        Compare(legacy, port, ComparisonOptions.Strict with { Ignore = ignore });

    /// <summary>
    /// Masks are applied to both bodies first. When masking is what made the bodies equal, the result is reported
    /// as an ignored "$masked" difference, so the grade is Equivalent rather than Identical.
    /// </summary>
    public static ComparisonResult Compare(CapturedResponse legacy, CapturedResponse port, ComparisonOptions options)
    {
        if (options.Masks.IsEmpty)
            return CompareUnmasked(legacy, port, options);

        var masked = CompareUnmasked(legacy with { Body = options.Masks.Apply(legacy.Body) }, port with { Body = options.Masks.Apply(port.Body) }, options);
        if (legacy.Body.AsSpan().SequenceEqual(port.Body))
            return masked;

        var differences = masked.Differences.ToList();
        if (!differences.Any(IsBodyDifference))
            differences.Add(new Difference("$masked", DifferenceKind.Value, Encoding.UTF8.GetString(legacy.Body), Encoding.UTF8.GetString(port.Body), Ignored: true));
        return new ComparisonResult(differences);
    }

    private static bool IsBodyDifference(Difference d) =>
        d.Path == "$" || d.Path.StartsWith("$.", StringComparison.Ordinal) || d.Path.StartsWith("$[", StringComparison.Ordinal) || d.Path.StartsWith("$body", StringComparison.Ordinal);

    private static ComparisonResult CompareUnmasked(CapturedResponse legacy, CapturedResponse port, ComparisonOptions options)
    {
        var differences = new List<Difference>();

        if (legacy.Status != port.Status)
            differences.Add(new Difference("$status", DifferenceKind.Status, Text(legacy.Status), Text(port.Status)));

        if (!string.Equals(Normalize(legacy.ContentType), Normalize(port.ContentType), StringComparison.Ordinal))
            differences.Add(new Difference("$content-type", DifferenceKind.ContentType, legacy.ContentType, port.ContentType));

        if ((legacy.ErrorIdHeader is null) != (port.ErrorIdHeader is null))
            differences.Add(new Difference("$header.ErrorId", DifferenceKind.ErrorIdHeader, Presence(legacy.ErrorIdHeader), Presence(port.ErrorIdHeader)));

        if (options.Xlsx && !string.Equals(legacy.ContentDisposition, port.ContentDisposition, StringComparison.Ordinal)
            && string.Equals(MaskFileDate(legacy.ContentDisposition), MaskFileDate(port.ContentDisposition), StringComparison.Ordinal))
            differences.Add(new Difference("$header.Content-Disposition", DifferenceKind.ContentDisposition, legacy.ContentDisposition, port.ContentDisposition, Ignored: true));
        else if (!string.Equals(legacy.ContentDisposition, port.ContentDisposition, StringComparison.Ordinal))
            differences.Add(new Difference("$header.Content-Disposition", DifferenceKind.ContentDisposition, legacy.ContentDisposition, port.ContentDisposition));

        if (!legacy.Body.AsSpan().SequenceEqual(port.Body))
        {
            if (options.Xlsx && XlsxComparer.Compare(legacy.Body, port.Body, differences))
            {
            }
            else if (IsBinary(legacy) || IsBinary(port))
                differences.Add(new Difference("$body(binary)", DifferenceKind.NonJsonBody, Fingerprint(legacy.Body), Fingerprint(port.Body)));
            else
                CompareBodies(legacy.Body, port.Body, options, differences);
        }

        return new ComparisonResult(differences);
    }

    private static void CompareBodies(byte[] legacy, byte[] port, ComparisonOptions options, List<Difference> differences)
    {
        var legacyDoc = TryParse(legacy);
        var portDoc = TryParse(port);
        if (legacyDoc is null || portDoc is null)
        {
            legacyDoc?.Dispose();
            portDoc?.Dispose();
            if (options.UnorderedXmlItems && SameXmlItems(Encoding.UTF8.GetString(legacy), Encoding.UTF8.GetString(port)))
            {
                differences.Add(new Difference("$body(sorted items)", DifferenceKind.NonJsonBody, Encoding.UTF8.GetString(legacy), Encoding.UTF8.GetString(port), Ignored: true));
                return;
            }
            differences.Add(new Difference("$body", DifferenceKind.NonJsonBody, Encoding.UTF8.GetString(legacy), Encoding.UTF8.GetString(port)));
            return;
        }

        using (legacyDoc)
        using (portDoc)
        {
            var before = differences.Count;
            CompareElements("$", legacyDoc.RootElement, portDoc.RootElement, options, differences);
            if (differences.Count == before)
                differences.Add(new Difference("$body", DifferenceKind.WhitespaceOnly, null, null));
        }
    }

    /// <summary>The "_ddMMMyyyy.xlsx" date the original put in exported file names.</summary>
    private static string? MaskFileDate(string? disposition) =>
        disposition is null ? null : System.Text.RegularExpressions.Regex.Replace(disposition, "_[0-9]{2}[A-Z][a-z]{2}[0-9]{4}[.]xlsx$", "_DATE.xlsx");

    /// <summary>
    /// File streams (images, documents, workbooks) are never parsed or printed: they may hold real data and megabytes.
    /// A body that is not valid UTF-8 counts as binary too.
    /// </summary>
    internal static bool IsBinary(CapturedResponse response)
    {
        var type = response.ContentType?.Split(';')[0].Trim().ToLowerInvariant() ?? string.Empty;
        if (type.StartsWith("image/", StringComparison.Ordinal) || type is "application/octet-stream" or "application/pdf" or "application/msword"
            or "application/x-excel" or "application/x-msexcel" or "application/x-rtf" or "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            return true;
        try
        {
            _ = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(response.Body);
            return false;
        }
        catch (DecoderFallbackException)
        {
            return true;
        }
    }

    /// <summary>A body's length and SHA-256 (hex), the only way binary bodies are reported.</summary>
    internal static string Fingerprint(byte[] body) =>
        $"len={body.Length} sha256={Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(body)).ToLowerInvariant()}";

    /// <summary>Equal text before the first &lt;item&gt; and after the last &lt;/item&gt;, and the same items in any order.</summary>
    internal static bool SameXmlItems(string legacy, string port)
    {
        var (legacyPrefix, legacyItems, legacySuffix) = SplitItems(legacy);
        var (portPrefix, portItems, portSuffix) = SplitItems(port);
        return legacyPrefix == portPrefix && legacySuffix == portSuffix
            && legacyItems.Order(StringComparer.Ordinal).SequenceEqual(portItems.Order(StringComparer.Ordinal), StringComparer.Ordinal);
    }

    private static (string Prefix, List<string> Items, string Suffix) SplitItems(string body)
    {
        var items = new List<string>();
        var start = body.IndexOf("<item>", StringComparison.Ordinal);
        if (start < 0)
            return (body, items, string.Empty);
        var prefix = body[..start];
        var position = start;
        while (body.IndexOf("<item>", position, StringComparison.Ordinal) == position)
        {
            var end = body.IndexOf("</item>", position, StringComparison.Ordinal);
            if (end < 0)
                break;
            end += "</item>".Length;
            items.Add(body[position..end]);
            position = end;
        }
        return (prefix, items, body[position..]);
    }

    /// <summary>A path as the rules see it: the "(sorted)" markers of unordered arrays removed.</summary>
    private static string Unsorted(string path) => path.Replace("(sorted)", string.Empty, StringComparison.Ordinal);

    /// <summary>The element's JSON with every value at an ignored path replaced by null (used to order unordered arrays).</summary>
    private static string Canonical(string path, JsonElement element, IgnoreRules ignore)
    {
        if (ignore.Matches(path))
            return "null";
        return element.ValueKind switch
        {
            JsonValueKind.Object => "{" + string.Join(",", element.EnumerateObject()
                .Select(p => JsonSerializer.Serialize(p.Name) + ":" + Canonical($"{path}.{p.Name}", p.Value, ignore))) + "}",
            JsonValueKind.Array => "[" + string.Join(",", element.EnumerateArray().Select(i => Canonical($"{path}[0]", i, ignore))) + "]",
            _ => element.GetRawText()
        };
    }

    private static void CompareElements(string path, JsonElement legacy, JsonElement port, ComparisonOptions options, List<Difference> differences)
    {
        var ignored = options.Ignore.Matches(Unsorted(path));

        if (legacy.ValueKind != port.ValueKind)
        {
            differences.Add(new Difference(path, DifferenceKind.ValueKind, legacy.ValueKind.ToString(), port.ValueKind.ToString(), ignored));
            return;
        }

        switch (legacy.ValueKind)
        {
            case JsonValueKind.Object:
                CompareObjects(path, legacy, port, options, differences, ignored);
                break;

            case JsonValueKind.Array:
                CompareArrays(path, legacy, port, options, differences, ignored);
                break;

            default:
                var a = legacy.GetRawText();
                var b = port.GetRawText();
                if (!string.Equals(a, b, StringComparison.Ordinal))
                    differences.Add(new Difference(path, DifferenceKind.Value, a, b, ignored));
                break;
        }
    }

    private static void CompareArrays(string path, JsonElement legacy, JsonElement port, ComparisonOptions options, List<Difference> differences, bool ignored)
    {
        var legacyItems = legacy.EnumerateArray().ToList();
        var portItems = port.EnumerateArray().ToList();

        if (options.Unordered.Matches(Unsorted(path)))
        {
            // Items are matched on their text with ignored members blanked, so random ids do not decide the order.
            string Key(JsonElement item) => Canonical($"{Unsorted(path)}[0]", item, options.Ignore);
            var legacyTexts = legacyItems.Select(i => i.GetRawText()).ToList();
            var portTexts = portItems.Select(i => i.GetRawText()).ToList();
            if (!legacyTexts.SequenceEqual(portTexts, StringComparer.Ordinal))
            {
                var sortedLegacy = legacyItems.OrderBy(Key, StringComparer.Ordinal).ToList();
                var sortedPort = portItems.OrderBy(Key, StringComparer.Ordinal).ToList();
                if (sortedLegacy.Select(i => i.GetRawText()).SequenceEqual(sortedPort.Select(i => i.GetRawText()), StringComparer.Ordinal))
                {
                    differences.Add(new Difference(path, DifferenceKind.ArrayOrder, null, null, Ignored: true));
                    return;
                }
                legacyItems = sortedLegacy;
                portItems = sortedPort;
                path += "(sorted)";
            }
        }

        if (legacyItems.Count != portItems.Count)
            differences.Add(new Difference(path, DifferenceKind.ArrayLength, Text(legacyItems.Count), Text(portItems.Count), ignored));
        for (var i = 0; i < Math.Min(legacyItems.Count, portItems.Count); i++)
            CompareElements($"{path}[{i}]", legacyItems[i], portItems[i], options, differences);
    }

    private static void CompareObjects(string path, JsonElement legacy, JsonElement port, ComparisonOptions options, List<Difference> differences, bool ignored)
    {
        var legacyNames = legacy.EnumerateObject().Select(p => p.Name).ToList();
        var portNames = port.EnumerateObject().Select(p => p.Name).ToList();

        foreach (var missing in legacyNames.Except(portNames, StringComparer.Ordinal))
            differences.Add(new Difference($"{path}.{missing}", DifferenceKind.MissingProperty, missing, null, options.Ignore.Matches(Unsorted($"{path}.{missing}"))));
        foreach (var extra in portNames.Except(legacyNames, StringComparer.Ordinal))
            differences.Add(new Difference($"{path}.{extra}", DifferenceKind.ExtraProperty, null, extra, options.Ignore.Matches(Unsorted($"{path}.{extra}"))));

        var common = legacyNames.Intersect(portNames, StringComparer.Ordinal).ToList();
        var portOrder = portNames.Where(n => common.Contains(n, StringComparer.Ordinal)).ToList();
        if (!common.SequenceEqual(portOrder, StringComparer.Ordinal))
            differences.Add(new Difference(path, DifferenceKind.PropertyOrder, string.Join(",", legacyNames), string.Join(",", portNames), ignored));

        foreach (var name in common)
            CompareElements($"{path}.{name}", legacy.GetProperty(name), port.GetProperty(name), options, differences);
    }

    private static JsonDocument? TryParse(byte[] body)
    {
        try
        {
            return JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? Normalize(string? contentType) =>
        contentType?.Replace(" ", string.Empty).ToLowerInvariant();

    private static string Presence(string? header) => header is null ? "absent" : "present";

    private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);
}
