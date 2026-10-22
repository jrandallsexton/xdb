using System;
using System.Collections.Generic;
using System.Text;

namespace Xdb.Core.LegacyWire;

/// <summary>
/// The original's hand-built dictionary JSON (Formatters.FormatDictionary), reproduced exactly:
/// [{"key":"...","value":"..."},...] with no whitespace, keys always quoted, only '"' escaped in values
/// (backslashes and control characters written raw), entries in insertion order, and "[]" for an empty dictionary.
/// The original took Dictionary&lt;,&gt; and enumerated it in insertion order; here the order is the list's order.
/// </summary>
public static class LegacyFormatters
{
    public static string FormatDictionary(IReadOnlyList<KeyValuePair<Guid, string>> values)
    {
        if (values.Count == 0)
            return "[]";
        var sb = new StringBuilder("[");
        foreach (var (key, value) in values)
            sb.Append("{\"key\":\"").Append(key.ToString("D")).Append("\",\"value\":\"").Append(value.Replace("\"", "\\\"")).Append("\"},");
        sb.Length--;
        return sb.Append(']').ToString();
    }

    public static string FormatDictionary(IReadOnlyList<KeyValuePair<Guid, Guid>> values)
    {
        if (values.Count == 0)
            return "[]";
        var sb = new StringBuilder("[");
        foreach (var (key, value) in values)
            sb.Append("{\"key\":\"").Append(key.ToString("D")).Append("\",\"value\":\"").Append(value.ToString("D")).Append("\"},");
        sb.Length--;
        return sb.Append(']').ToString();
    }

    public static string FormatDictionary(IReadOnlyList<KeyValuePair<int, string>> values)
    {
        if (values.Count == 0)
            return "[]";
        var sb = new StringBuilder("[");
        foreach (var (key, value) in values)
            sb.Append("{\"key\":\"").Append(key.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append("\",\"value\":\"").Append(value.Replace("\"", "\\\"")).Append("\"},");
        sb.Length--;
        return sb.Append(']').ToString();
    }

    /// <summary>
    /// The grouped form (Dictionary&lt;string, Dictionary&lt;Guid,string&gt;&gt;): CRLF after "[", each "{" and each
    /// "values": [", group keys NOT escaped, values escaping only '"'.
    /// </summary>
    public static string FormatGroupedDictionary(IReadOnlyList<KeyValuePair<string, IReadOnlyList<KeyValuePair<Guid, string>>>> groups)
    {
        if (groups.Count == 0)
            return "[]";
        var sb = new StringBuilder("[\r\n");
        for (var g = 0; g < groups.Count; g++)
        {
            var (key, values) = groups[g];
            sb.Append("{\r\n").Append("\"key\":\"").Append(key).Append("\",").Append("\"values\": [\r\n");
            for (var i = 0; i < values.Count; i++)
            {
                sb.Append("{\"key\":\"").Append(values[i].Key.ToString("D")).Append("\",\"value\":\"").Append(values[i].Value.Replace("\"", "\\\"")).Append('"');
                sb.Append(i == values.Count - 1 ? "}" : "},");
            }
            sb.Append(g == groups.Count - 1 ? "]}" : "]},");
        }
        return sb.Append(']').ToString();
    }
}
