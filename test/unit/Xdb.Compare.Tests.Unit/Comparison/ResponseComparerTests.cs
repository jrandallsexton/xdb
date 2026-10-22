using FluentAssertions;

using Xdb.Compare.Comparison;

using Xunit;

namespace Xdb.Compare.Tests.Unit.Comparison;

public class ResponseComparerTests
{
    private const string Json = "application/json; charset=utf-8";

    private static ComparisonResult Compare(string legacy, string port, params string[] ignore) =>
        ResponseComparer.Compare(
            CapturedResponse.FromText(200, Json, legacy),
            CapturedResponse.FromText(200, Json, port),
            new IgnoreRules(ignore));

    [Fact]
    public void SameBytes_AreIdentical()
    {
        Compare("""{"a":1}""", """{"a":1}""").Grade.Should().Be(ComparisonGrade.Identical);
    }

    [Fact]
    public void EscapedSlash_IsADifference()
    {
        var result = Compare("""{"d":"\/Date(0)\/"}""", """{"d":"/Date(0)/"}""");

        result.Grade.Should().Be(ComparisonGrade.Different);
        result.Differences.Should().ContainSingle(d => d.Path == "$.d" && d.Kind == DifferenceKind.Value);
    }

    [Fact]
    public void NumberFormatting_IsADifference()
    {
        Compare("""{"n":1.0}""", """{"n":1}""").Differences.Should().ContainSingle(d => d.Path == "$.n");
    }

    [Fact]
    public void PropertyOrder_IsADifference()
    {
        Compare("""{"a":1,"b":2}""", """{"b":2,"a":1}""").Differences.Should().ContainSingle(d => d.Kind == DifferenceKind.PropertyOrder);
    }

    [Fact]
    public void WhitespaceBetweenTokens_IsReportedAsSuch()
    {
        Compare("""{"a":1}""", """{ "a": 1 }""").Differences.Should().ContainSingle(d => d.Kind == DifferenceKind.WhitespaceOnly);
    }

    [Fact]
    public void IgnoredPath_MakesTheResultEquivalent()
    {
        var result = Compare("""{"rows":[{"cell":[1,"x",5]}]}""", """{"rows":[{"cell":[1,"x",9]}]}""", "$.rows[*].cell[2]");

        result.Grade.Should().Be(ComparisonGrade.Equivalent);
        result.Differences.Should().ContainSingle(d => d.Ignored);
    }

    [Fact]
    public void MissingAndExtraProperties_AreReported()
    {
        var result = Compare("""{"a":1,"b":2}""", """{"a":1,"c":3}""");

        result.Differences.Should().Contain(d => d.Kind == DifferenceKind.MissingProperty && d.Path == "$.b");
        result.Differences.Should().Contain(d => d.Kind == DifferenceKind.ExtraProperty && d.Path == "$.c");
    }

    [Fact]
    public void ArrayLength_IsReported()
    {
        Compare("[1,2,3]", "[1,2]").Differences.Should().ContainSingle(d => d.Kind == DifferenceKind.ArrayLength);
    }

    [Fact]
    public void StatusAndContentType_AreCompared()
    {
        var result = ResponseComparer.Compare(
            CapturedResponse.FromText(200, "text/html", "[]"),
            CapturedResponse.FromText(500, "application/json", "[]"),
            IgnoreRules.None);

        result.Differences.Should().Contain(d => d.Kind == DifferenceKind.Status);
        result.Differences.Should().Contain(d => d.Kind == DifferenceKind.ContentType);
    }

    [Fact]
    public void ContentType_IgnoresSpacingAndCase()
    {
        var result = ResponseComparer.Compare(
            CapturedResponse.FromText(200, "application/json; charset=utf-8", "1"),
            CapturedResponse.FromText(200, "application/json;charset=UTF-8", "1"),
            IgnoreRules.None);

        result.Grade.Should().Be(ComparisonGrade.Identical);
    }

    [Fact]
    public void ErrorIdHeader_PresenceIsCompared()
    {
        var result = ResponseComparer.Compare(
            CapturedResponse.FromText(500, Json, "false", "abc"),
            CapturedResponse.FromText(500, Json, "false"),
            IgnoreRules.None);

        result.Differences.Should().ContainSingle(d => d.Kind == DifferenceKind.ErrorIdHeader);
    }

    [Fact]
    public void NonJsonBodies_AreComparedAsText()
    {
        Compare("<html>a</html>", "<html>b</html>").Differences.Should().ContainSingle(d => d.Kind == DifferenceKind.NonJsonBody);
    }
}
