using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Xdb.Api.Application.Files;

/// <summary>
/// The original's multipart handling (Service.getFile, spec M8-F 0.4) as the byte transformation it was: it decoded
/// the body with Windows-1252 (lossless), cut three header lines found with a culture-sensitive search (a line end is
/// CR, any number of soft-hyphen bytes 0xAD, LF), removed four more characters after the third line, and deleted the
/// closing delimiter wherever it appears. The request's Content-Type and boundary are never read.
/// </summary>
public static class LegacyMultipart
{
    public static byte[] GetFile(byte[] body)
    {
        var rest = (ReadOnlySpan<byte>)body;
        var first = rest[..LineEnd(rest)].ToArray();
        rest = Skip(rest, first.Length + 2);
        var second = LineEnd(rest);
        rest = Skip(rest, second + 2);
        var third = LineEnd(rest);
        rest = Skip(rest, third + 4);

        var delimiter = new byte[first.Length + 2];
        first.CopyTo(delimiter, 0);
        delimiter[^2] = (byte)'-';
        delimiter[^1] = (byte)'-';
        return RemoveAll(rest, delimiter);
    }

    private static int LineEnd(ReadOnlySpan<byte> s)
    {
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] != 0x0D)
                continue;
            var j = i + 1;
            while (j < s.Length && s[j] == 0xAD)
                j++;
            if (j < s.Length && s[j] == 0x0A)
                return i;
        }
        // Substring(0, IndexOf(...) = -1): ArgumentOutOfRangeException in the original.
        throw new ArgumentOutOfRangeException(nameof(s), "Length cannot be less than zero.");
    }

    private static ReadOnlySpan<byte> Skip(ReadOnlySpan<byte> s, int count) =>
        count <= s.Length ? s[count..] : throw new ArgumentOutOfRangeException(nameof(count), "Index and count must refer to a location within the string.");

    /// <summary>string.Replace(value, ""): every non-overlapping occurrence, left to right.</summary>
    private static byte[] RemoveAll(ReadOnlySpan<byte> s, ReadOnlySpan<byte> value)
    {
        var result = new List<byte>(s.Length);
        var i = 0;
        while (i < s.Length)
        {
            if (i + value.Length <= s.Length && s.Slice(i, value.Length).SequenceEqual(value))
            {
                i += value.Length;
                continue;
            }
            result.Add(s[i]);
            i++;
        }
        return result.ToArray();
    }
}

/// <summary>The original's file-name rules (ImageLayer.Image_GetType, DocumentLayer.Document_GetType, the setters).</summary>
public static class LegacyFileNames
{
    /// <summary>.NET Framework's Path.GetExtension rejected these characters ("Illegal characters in path.").</summary>
    private static bool IsInvalidPathChar(char c) => c is '"' or '<' or '>' or '|' || c < ' ';

    /// <summary>Path.GetExtension on .NET Framework (Windows separators '\', '/', ':'), then lower-cased.</summary>
    public static string Extension(string? name)
    {
        if (name is null)
            throw new NullReferenceException("Object reference not set to an instance of an object.");
        foreach (var c in name)
        {
            if (IsInvalidPathChar(c))
                throw new ArgumentException("Illegal characters in path.");
        }
        for (var i = name.Length - 1; i >= 0; i--)
        {
            var c = name[i];
            if (c == '.')
                return i == name.Length - 1 ? string.Empty : name[i..].ToLowerInvariant();
            if (c is '\\' or '/' or ':')
                break;
        }
        return string.Empty;
    }

    /// <summary>EImageType: Undefined = -1, Bitmap, GIF, ICO, JPEG, PNG, TIF.</summary>
    public static int ImageType(string? name) => Extension(name) switch
    {
        ".bmp" => 0,
        ".gif" => 1,
        ".ico" => 2,
        ".jpg" or ".jpeg" => 3,
        ".png" => 4,
        ".tif" => 5,
        _ => -1
    };

    /// <summary>EDocumentType: Undefined = -1, CSV, DOC, DOCX, PDF, RTF, TXT, XLS, XLSX, XML.</summary>
    public static int DocumentType(string? name) => Extension(name) switch
    {
        ".csv" => 0,
        ".doc" => 1,
        ".docx" => 2,
        ".pdf" => 3,
        ".rtf" => 4,
        ".txt" => 5,
        ".xls" => 6,
        ".xlsx" => 7,
        ".xml" => 8,
        _ => -1
    };

    public static string ImageTypeName(int value) => value switch
    {
        0 => "Bitmap",
        1 => "GIF",
        2 => "ICO",
        3 => "JPEG",
        4 => "PNG",
        5 => "TIF",
        _ => "Undefined"
    };

    public static string DocumentTypeName(int value) => value switch
    {
        0 => "CSV",
        1 => "DOC",
        2 => "DOCX",
        3 => "PDF",
        4 => "RTF",
        5 => "TXT",
        6 => "XLS",
        7 => "XLSX",
        8 => "XML",
        _ => "Undefined"
    };

    /// <summary>The Name setters: longer than 50 characters keeps 49.</summary>
    public static string? Name(string? name) => name is { Length: > 50 } ? name[..49] : name;

    /// <summary>The document Title setter: longer than 255 characters keeps 254.</summary>
    public static string? Title(string? title) => title is { Length: > 255 } ? title[..254] : title;

    /// <summary>formatNameTitle: the text after the last backslash.</summary>
    public static string FormatNameTitle(string? text)
    {
        if (text is null)
            throw new NullReferenceException("Object reference not set to an instance of an object.");
        return text[(text.LastIndexOf('\\') + 1)..];
    }
}

/// <summary>
/// The uploads' progress counters (the original kept them in the ASP.NET cache of its worker process): bytes read
/// so far per upload id, read back by getUploadStatus. Keys are ordinal; a null key fails as the cache did.
/// </summary>
public sealed class UploadProgress
{
    private readonly ConcurrentDictionary<string, int> _counts = new(StringComparer.Ordinal);

    public void Set(string? key, int count) => _counts[key ?? throw new ArgumentNullException(nameof(key))] = count;

    public int Get(string? key) =>
        _counts.TryGetValue(key ?? throw new ArgumentNullException(nameof(key)), out var count)
            ? count
            : throw new NullReferenceException("Object reference not set to an instance of an object.");
}

/// <summary>The upload operations' response texts.</summary>
public static class LegacyUploadJson
{
    /// <summary>The uploads' hand-built answer (AppendLine: CRLF after every line), sent as ASCII text/html.</summary>
    public static string Success(string? name, int size, Guid propertyId, string uploadId)
    {
        var sb = new StringBuilder();
        foreach (var line in new[]
                 {
                     "[", "{", "\"name\": \"" + name + "\",", "\"size\": " + size + ",", "\"url\": \"\",", "\"thumbnail_url\": \"\",",
                     "\"delete_url\": \"//example.org/upload-handler?file=picture1.jpg\",", "\"delete_type\": \"DELETE\",", "\"error\": \"\",",
                     "\"propertyId\": \"" + propertyId + "\",", "\"uploadId\": \"" + uploadId + "\"", "}", "]"
                 })
            sb.Append(line).Append("\r\n");
        return sb.ToString();
    }
}
