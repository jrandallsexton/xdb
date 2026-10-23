using System;
using System.Text;

using FluentAssertions;

using Xdb.Core.LegacyWire;

using Xunit;

namespace Xdb.Core.Tests.Unit.LegacyWire;

/// <summary>Feed bytes and link escaping against the original's formatter (FX F, D and U vectors; Eastern host time zone).</summary>
public class LegacyRssTests
{
    private const string Head = "<rss version=\"2.0\" xmlns:a10=\"http://www.w3.org/2005/Atom\"><channel><title>F</title><link>http://h/svc/SystemActivity</link><description>d</description><managingEditor>ed@example.com</managingEditor>";
    private static readonly DateTime Now = new(2026, 10, 23, 4, 56, 39);

    private static string Feed(params RssItem[] items) =>
        Encoding.UTF8.GetString(LegacyRss.Write(new RssChannel("F", "http://h/svc/SystemActivity", "d", "ed@example.com", items)));

    [Fact]
    public void EmptyFeed_F1() =>
        Encoding.UTF8.GetString(LegacyRss.Write(new RssChannel("Product X (DEV)", "http://h/svc/ServiceModuleLogs", "d", "ed@example.com", [])))
            .Should().Be("<rss version=\"2.0\" xmlns:a10=\"http://www.w3.org/2005/Atom\"><channel><title>Product X (DEV)</title><link>http://h/svc/ServiceModuleLogs</link><description>d</description><managingEditor>ed@example.com</managingEditor></channel></rss>");

    [Fact]
    public void EmptyContent_F2() =>
        Feed(new RssItem("11111111-1111-1111-1111-111111111111", "http://h/svc/ServiceModuleLogs?logGroupId=11111111-1111-1111-1111-111111111111", "t", "", null, Now))
            .Should().Be(Head + "<item><guid isPermaLink=\"false\">11111111-1111-1111-1111-111111111111</guid><link>http://h/svc/ServiceModuleLogs?logGroupId=11111111-1111-1111-1111-111111111111</link><title>t</title><description/><a10:updated>2026-10-23T04:56:39-04:00</a10:updated></item></channel></rss>");

    [Fact]
    public void PublishDate_F3() =>
        Feed(new RssItem("22222222-2222-2222-2222-222222222222", "http://h/svc/SystemActivityHomeDetails?userId=11111111-1111-1111-1111-111111111111&submittalId=22222222-2222-2222-2222-222222222222",
                "t", "Admin", Now, new DateTime(2013, 1, 5, 18, 45, 0)))
            .Should().Be(Head + "<item><guid isPermaLink=\"false\">22222222-2222-2222-2222-222222222222</guid><link>http://h/svc/SystemActivityHomeDetails?userId=11111111-1111-1111-1111-111111111111&amp;submittalId=22222222-2222-2222-2222-222222222222</link><title>t</title><description>Admin</description><pubDate>Fri, 23 Oct 2026 04:56:39 -0400</pubDate><a10:updated>2013-01-05T18:45:00-05:00</a10:updated></item></channel></rss>");

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void NoId_F4_F5(string? id) =>
        Feed(new RssItem(id, "http://h/x", "t", "c", null, Now))
            .Should().Be(Head + "<item><link>http://h/x</link><title>t</title><description>c</description><a10:updated>2026-10-23T04:56:39-04:00</a10:updated></item></channel></rss>");

    [Fact]
    public void Escapes_F6() =>
        Feed(new RssItem("i&d<", "http://h/x", "a<b>&\"'\r\n\t]]>", "x changed from 'a&b' to '<c>'.\r\n", null, Now))
            .Should().Be(Head + "<item><guid isPermaLink=\"false\">i&amp;d&lt;</guid><link>http://h/x</link><title>a&lt;b&gt;&amp;\"'&#xD;\n\t]]&gt;</title><description>x changed from 'a&amp;b' to '&lt;c&gt;'.&#xD;\n</description><a10:updated>2026-10-23T04:56:39-04:00</a10:updated></item></channel></rss>");

    [Theory]
    [InlineData("a\u0001b", "<title>a&#x1;b</title>")]   // F7
    [InlineData("é€中", "<title>é€中</title>")]           // F8
    [InlineData("", "<title/>")]                           // F9
    [InlineData("😀", "<title>😀</title>")]               // F13
    public void TitleText(string title, string expected) =>
        Feed(new RssItem("i", "http://h/x", title, "c", null, Now)).Should().Contain("<link>http://h/x</link>" + expected + "<description>c</description>");

    [Fact]
    public void NullTitle_F10() =>
        Feed(new RssItem("i", "http://h/x", null, "c", null, Now)).Should().Contain("<link>http://h/x</link><description>c</description>");

    [Fact]
    public void NonCharacter_F13() =>
        Feed(new RssItem("i", "http://h/x", "t", "￾", null, Now)).Should().Contain("<description>&#xFFFE;</description>");

    [Fact]
    public void Extensions_F11() =>
        Feed(new RssItem("i", "http://h/x", "t", "Role", Now, Now,
                [new("assetTypeId", "11111111-1111-1111-1111-111111111111"), new("submittalDate", "Thu 22 Oct 2026 06:45:00"), new("x", "a<b&c")]))
            .Should().EndWith("<pubDate>Fri, 23 Oct 2026 04:56:39 -0400</pubDate><a10:updated>2026-10-23T04:56:39-04:00</a10:updated><assetTypeId>11111111-1111-1111-1111-111111111111</assetTypeId><submittalDate>Thu 22 Oct 2026 06:45:00</submittalDate><x>a&lt;b&amp;c</x></item></channel></rss>");

    [Fact]
    public void Categories_F12() =>
        Encoding.UTF8.GetString(LegacyRss.Write(new RssChannel("t", "http://h/svc/ServiceModuleActivity", "d", "ed@example.com", [],
                ["11111111-1111-1111-1111-111111111111", "a<b"])))
            .Should().Be("<rss version=\"2.0\" xmlns:a10=\"http://www.w3.org/2005/Atom\"><channel><title>t</title><link>http://h/svc/ServiceModuleActivity</link><description>d</description><managingEditor>ed@example.com</managingEditor><category>11111111-1111-1111-1111-111111111111</category><category>a&lt;b</category></channel></rss>");

    [Theory]
    [InlineData(2026, 11, 1, 1, 30, 0, "Sun, 01 Nov 2026 01:30:00 -0500", "2026-11-01T01:30:00-05:00")]   // ambiguous: standard
    [InlineData(2026, 3, 8, 2, 30, 0, "Sun, 08 Mar 2026 02:30:00 -0500", "2026-03-08T02:30:00-05:00")]    // skipped: standard
    [InlineData(2013, 1, 5, 6, 7, 8, "Sat, 05 Jan 2013 06:07:08 -0500", "2013-01-05T06:07:08-05:00")]
    [InlineData(2013, 7, 5, 6, 7, 8, "Fri, 05 Jul 2013 06:07:08 -0400", "2013-07-05T06:07:08-04:00")]
    public void Dates_D(int y, int mo, int d, int h, int mi, int s, string rfc822, string iso)
    {
        var local = new DateTime(y, mo, d, h, mi, s);
        LegacyRss.Rfc822(local).Should().Be(rfc822);
        LegacyRss.Iso(local).Should().Be(iso);
    }

    [Fact]
    public void Iso_DropsTheFraction() => LegacyRss.Iso(new DateTime(2026, 10, 23, 4, 56, 39, 123)).Should().Be("2026-10-23T04:56:39-04:00");

    [Theory]
    [InlineData("{0}", "%7B0%7D")]
    [InlineData("a b", "a%20b")]
    [InlineData("a{b}c", "a%7Bb%7Dc")]
    [InlineData("x&y", "x&y")]
    [InlineData("a\"b", "a%22b")]
    [InlineData("a<b>c", "a%3Cb%3Ec")]
    [InlineData("a\\b", "a%5Cb")]
    [InlineData("a^b", "a%5Eb")]
    [InlineData("a|b", "a%7Cb")]
    [InlineData("a`b", "a%60b")]
    [InlineData("50%", "50%25")]
    [InlineData("%41", "%41")]
    [InlineData("%zz", "%25zz")]
    [InlineData("a#b", "a#b")]
    [InlineData("é", "%C3%A9")]
    [InlineData("中", "%E4%B8%AD")]
    [InlineData("a'b", "a'b")]
    [InlineData("[x]", "%5Bx%5D")]
    [InlineData("a+b", "a+b")]
    [InlineData("a;b=c", "a;b=c")]
    [InlineData("\t", "%09")]
    [InlineData("a%20b", "a%20b")]
    [InlineData("~!$()*,:@", "~!$()*,:@")]
    [InlineData("", "")]
    public void AbsoluteUri_U(string value, string expected) =>
        LegacyUri.AbsoluteUri($"http://h/svc/SystemActivityHome?submittalId={value}&maxItems=100")
            .Should().Be($"http://h/svc/SystemActivityHome?submittalId={expected}&maxItems=100");

    [Fact]
    public void AbsoluteUri_ChannelLink() =>
        LegacyUri.AbsoluteUri("http://h/svc/SystemActivityHome?submittalId={0}&maxItems=100/SystemActivity")
            .Should().Be("http://h/svc/SystemActivityHome?submittalId=%7B0%7D&maxItems=100/SystemActivity");
}
