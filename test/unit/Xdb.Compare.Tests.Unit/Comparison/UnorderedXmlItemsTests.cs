using FluentAssertions;

using Xdb.Compare.Comparison;

using Xunit;

namespace Xdb.Compare.Tests.Unit.Comparison;

public class UnorderedXmlItemsTests
{
    private const string Xml = "application/xml; charset=utf-8";
    private const string Head = "<rss><channel><title>t</title>";
    private const string Tail = "</channel></rss>";

    private static ComparisonResult Compare(string legacy, string port, bool unorderedItems) =>
        ResponseComparer.Compare(CapturedResponse.FromText(200, Xml, legacy), CapturedResponse.FromText(200, Xml, port),
            ComparisonOptions.Strict with { UnorderedXmlItems = unorderedItems });

    [Fact]
    public void SameItemsInAnotherOrder_IsEquivalent() =>
        Compare(Head + "<item>a</item><item>b</item>" + Tail, Head + "<item>b</item><item>a</item>" + Tail, unorderedItems: true)
            .Grade.Should().Be(ComparisonGrade.Equivalent);

    [Fact]
    public void WithoutTheRule_OrderCounts() =>
        Compare(Head + "<item>a</item><item>b</item>" + Tail, Head + "<item>b</item><item>a</item>" + Tail, unorderedItems: false)
            .Grade.Should().Be(ComparisonGrade.Different);

    [Fact]
    public void DifferentItems_AreDifferent() =>
        Compare(Head + "<item>a</item><item>b</item>" + Tail, Head + "<item>a</item><item>c</item>" + Tail, unorderedItems: true)
            .Grade.Should().Be(ComparisonGrade.Different);

    [Fact]
    public void DifferentChannel_IsDifferent() =>
        Compare(Head + "<item>a</item>" + Tail, "<rss><channel><title>x</title><item>a</item>" + Tail, unorderedItems: true)
            .Grade.Should().Be(ComparisonGrade.Different);
}
