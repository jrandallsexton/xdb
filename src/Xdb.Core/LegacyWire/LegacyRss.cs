using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Xdb.Core.LegacyWire;

/// <summary>An RSS item as the original built it (SyndicationItem): optional id and title, element extensions in order.</summary>
public sealed record RssItem(string? Id, string Link, string? Title, string? Content, DateTime? PublishDate, DateTime Updated,
    IReadOnlyList<KeyValuePair<string, string>>? Extensions = null);

/// <summary>An RSS channel as the original built it (SyndicationFeed with one author and optional categories).</summary>
public sealed record RssChannel(string Title, string Link, string Description, string ManagingEditor, IReadOnlyList<RssItem> Items,
    IReadOnlyList<string>? Categories = null);

/// <summary>
/// Writes the bytes WCF wrote for an Rss20FeedFormatter with the text XmlDictionaryWriter: UTF-8 without BOM or XML
/// declaration, no whitespace, the formatter's fixed element order, its escaping rules (quotes raw, '&gt;' always
/// escaped, CR and other control characters as character references) and its date formats in the host's time zone.
/// </summary>
public static class LegacyRss
{
    static LegacyRss() => LegacyTimeZone.Apply();

    public const string ContentType = "application/xml; charset=utf-8";

    public static byte[] Write(RssChannel channel)
    {
        var sb = new StringBuilder();
        sb.Append("<rss version=\"2.0\" xmlns:a10=\"http://www.w3.org/2005/Atom\"><channel>");
        Element(sb, "title", channel.Title);
        Element(sb, "link", channel.Link);
        Element(sb, "description", channel.Description);
        Element(sb, "managingEditor", channel.ManagingEditor);
        foreach (var category in channel.Categories ?? [])
            Element(sb, "category", category);
        foreach (var item in channel.Items)
        {
            sb.Append("<item>");
            if (!string.IsNullOrEmpty(item.Id))
                sb.Append("<guid isPermaLink=\"false\">").Append(Escape(item.Id)).Append("</guid>");
            Element(sb, "link", item.Link);
            if (item.Title is not null)
                Element(sb, "title", item.Title);
            Element(sb, "description", item.Content ?? string.Empty);
            if (item.PublishDate is { } published)
                Element(sb, "pubDate", Rfc822(published));
            Element(sb, "a10:updated", Iso(item.Updated));
            foreach (var (name, value) in item.Extensions ?? [])
                Element(sb, name, value);
            sb.Append("</item>");
        }
        sb.Append("</channel></rss>");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static void Element(StringBuilder sb, string name, string text)
    {
        if (text.Length == 0)
        {
            sb.Append('<').Append(name).Append("/>");
            return;
        }
        sb.Append('<').Append(name).Append('>').Append(Escape(text)).Append("</").Append(name).Append('>');
    }

    /// <summary>XmlUTF8TextWriter text escaping.</summary>
    public static string Escape(string text)
    {
        var sb = new StringBuilder(text.Length + 8);
        foreach (var c in text)
        {
            switch (c)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '\n':
                case '\t':
                    sb.Append(c);
                    break;
                default:
                    if (c < ' ' || c == '￾' || c == '￿')
                        sb.Append("&#x").Append(((int)c).ToString("X", CultureInfo.InvariantCulture)).Append(';');
                    else
                        sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    /// <summary>The offset of a local wall-clock time; ambiguous and skipped times get the standard offset.</summary>
    private static TimeSpan Offset(DateTime local) => TimeZoneInfo.Local.GetUtcOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified));

    /// <summary>pubDate: "ddd, dd MMM yyyy HH:mm:ss" and the offset without a colon.</summary>
    public static string Rfc822(DateTime local)
    {
        var offset = Offset(local);
        var sign = offset < TimeSpan.Zero ? '-' : '+';
        var abs = offset.Duration();
        return local.ToString("ddd, dd MMM yyyy HH:mm:ss ", CultureInfo.InvariantCulture)
            + sign + abs.Hours.ToString("00", CultureInfo.InvariantCulture) + abs.Minutes.ToString("00", CultureInfo.InvariantCulture);
    }

    /// <summary>a10:updated: "yyyy-MM-ddTHH:mm:ss" (no fraction) and the offset with a colon.</summary>
    public static string Iso(DateTime local)
    {
        var offset = Offset(local);
        var sign = offset < TimeSpan.Zero ? '-' : '+';
        var abs = offset.Duration();
        return local.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture)
            + sign + abs.Hours.ToString("00", CultureInfo.InvariantCulture) + ":" + abs.Minutes.ToString("00", CultureInfo.InvariantCulture);
    }
}

/// <summary>.NET Framework 4.8's <c>new Uri(text).AbsoluteUri</c> for the absolute http links the original's feeds built.</summary>
public static class LegacyUri
{
    private const string Unreserved = "-._~!$&'()*+,;=:@/?";

    public static string AbsoluteUri(string text)
    {
        var sb = new StringBuilder(text.Length + 16);
        var fragment = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (char.IsAsciiLetterOrDigit(c) || Unreserved.Contains(c))
            {
                sb.Append(c);
            }
            else if (c == '#' && !fragment)
            {
                fragment = true;
                sb.Append(c);
            }
            else if (c == '%' && i + 2 < text.Length && char.IsAsciiHexDigit(text[i + 1]) && char.IsAsciiHexDigit(text[i + 2]))
            {
                sb.Append(c);
            }
            else
            {
                var length = char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]) ? 2 : 1;
                foreach (var b in Encoding.UTF8.GetBytes(text.Substring(i, length)))
                    sb.Append('%').Append(b.ToString("X2", CultureInfo.InvariantCulture));
                i += length - 1;
            }
        }
        return sb.ToString();
    }
}
