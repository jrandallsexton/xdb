using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Xdb.Compare.Comparison;

/// <summary>
/// Compares two xlsx packages part by part (zip metadata is never compared): the same entries in the same order,
/// the same UTF-8 BOM use, and the same text after normalizing what the original itself varies on every call or
/// what a deferred dependency produces: random relationship ids, the generation time in the description property
/// and column widths (measured with GDI+ in the original). Bodies are reported by part name only, never printed.
/// </summary>
public static class XlsxComparer
{
    private static readonly Regex RelationshipId = new("R[0-9a-f]{16}", RegexOptions.Compiled);
    private static readonly Regex Width = new("(<col [^>]*?width=\")[^\"]*(\")", RegexOptions.Compiled);
    private static readonly Regex Description = new("(<dc:description>.* on )[^<]*(</dc:description>)", RegexOptions.Compiled);

    public static bool IsZip(byte[] body) => body.Length >= 4 && body[0] == 'P' && body[1] == 'K' && body[2] == 3 && body[3] == 4;

    /// <summary>Adds differences (ignored ones for masked values) and returns true when both bodies were compared as packages.</summary>
    public static bool Compare(byte[] legacy, byte[] port, List<Difference> differences)
    {
        if (!IsZip(legacy) || !IsZip(port))
            return false;
        var a = Parts(legacy);
        var b = Parts(port);
        if (!a.Select(p => p.Name).SequenceEqual(b.Select(p => p.Name)))
        {
            differences.Add(new Difference("$xlsx(entries)", DifferenceKind.NonJsonBody, string.Join(",", a.Select(p => p.Name)), string.Join(",", b.Select(p => p.Name))));
            return true;
        }
        var masked = false;
        for (var i = 0; i < a.Count; i++)
        {
            var (name, legacyBom, legacyText) = a[i];
            var (_, portBom, portText) = b[i];
            if (legacyBom != portBom)
                differences.Add(new Difference($"$xlsx({name}).bom", DifferenceKind.NonJsonBody, legacyBom.ToString(), portBom.ToString()));
            if (legacyText == portText)
                continue;
            var legacyNormal = Normalize(legacyText);
            var portNormal = Normalize(portText);
            if (legacyNormal == portNormal)
            {
                masked = true;
                continue;
            }
            var at = FirstDifference(legacyNormal, portNormal);
            differences.Add(new Difference($"$xlsx({name})@{at}", DifferenceKind.NonJsonBody, Context(legacyNormal, at), Context(portNormal, at)));
        }
        if (masked)
            differences.Add(new Difference("$xlsx(masked)", DifferenceKind.Value, null, null, Ignored: true));
        return true;
    }

    private static List<(string Name, bool Bom, string Text)> Parts(byte[] body)
    {
        using var zip = new ZipArchive(new MemoryStream(body), ZipArchiveMode.Read);
        var parts = new List<(string, bool, string)>();
        foreach (var entry in zip.Entries)
        {
            using var stream = entry.Open();
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            var bytes = buffer.ToArray();
            var bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            parts.Add((entry.FullName, bom, Encoding.UTF8.GetString(bytes, bom ? 3 : 0, bytes.Length - (bom ? 3 : 0))));
        }
        return parts;
    }

    private static string Normalize(string text) =>
        Description.Replace(Width.Replace(RelationshipId.Replace(text, "R"), "${1}W${2}"), "${1}T${2}");

    private static int FirstDifference(string a, string b)
    {
        var n = Math.Min(a.Length, b.Length);
        for (var i = 0; i < n; i++)
        {
            if (a[i] != b[i])
                return i;
        }
        return n;
    }

    /// <summary>The markup around a difference with element text replaced by its length (parts hold report data).</summary>
    private static string Context(string text, int at)
    {
        var start = Math.Max(0, at - 40);
        var slice = text.Substring(start, Math.Min(80, text.Length - start));
        return Regex.Replace(slice, ">([^<]+)<", m => $">#{m.Groups[1].Value.Length}<");
    }
}
