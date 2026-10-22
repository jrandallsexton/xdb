using FluentAssertions;

using Xdb.Compare.Comparison;

using Xunit;

namespace Xdb.Compare.Tests.Unit.Comparison;

public class ComparisonOptionsTests
{
    private const string Json = "application/json";

    private static ComparisonResult Compare(string legacy, string port, ComparisonOptions options) =>
        ResponseComparer.Compare(CapturedResponse.FromText(200, Json, legacy), CapturedResponse.FromText(200, Json, port), options);

    private static ComparisonOptions Unordered(string path) => ComparisonOptions.Strict with { Unordered = new IgnoreRules([path]) };

    private static ComparisonOptions Masked(string pattern) => ComparisonOptions.Strict with { Masks = new BodyMasks([pattern]) };

    [Fact]
    public void UnorderedArray_SameElementsDifferentOrder_IsEquivalent()
    {
        var result = Compare("""{"rows":[{"a":1},{"a":2}]}""", """{"rows":[{"a":2},{"a":1}]}""", Unordered("$.rows"));

        result.Grade.Should().Be(ComparisonGrade.Equivalent);
        result.Differences.Should().ContainSingle(d => d.Kind == DifferenceKind.ArrayOrder && d.Ignored);
    }

    [Fact]
    public void UnorderedArray_DifferentElements_IsStillDifferent()
    {
        var result = Compare("""{"rows":[{"a":1},{"a":2}]}""", """{"rows":[{"a":3},{"a":1}]}""", Unordered("$.rows"));

        result.Grade.Should().Be(ComparisonGrade.Different);
        result.Differences.Should().Contain(d => d.Path.StartsWith("$.rows(sorted)"));
    }

    [Fact]
    public void UnorderedArray_IgnoresIgnoredMembersWhenMatchingItems()
    {
        var options = ComparisonOptions.Strict with { Unordered = new IgnoreRules(["$.rows"]), Ignore = new IgnoreRules(["$.rows[*].id"]) };

        var result = Compare("""{"rows":[{"id":"x1","a":1},{"id":"x2","a":2}]}""", """{"rows":[{"id":"y9","a":2},{"id":"y8","a":1}]}""", options);

        result.Grade.Should().Be(ComparisonGrade.Equivalent);
        result.Differences.Should().OnlyContain(d => d.Ignored);
    }

    [Fact]
    public void OrderedArrays_StayStrict()
    {
        Compare("[1,2]", "[2,1]", ComparisonOptions.Strict).Grade.Should().Be(ComparisonGrade.Different);
    }

    [Fact]
    public void Mask_HidesOnlyTheCapturedGroup()
    {
        const string pattern = """^"(?<n>\d+) records in (?<mask>[0-9:.]+)"$""";

        Compare("\"15 records in 00:00.263\"", "\"15 records in 00:00.187\"", Masked(pattern)).Grade.Should().Be(ComparisonGrade.Equivalent);
        Compare("\"15 records in 00:00.263\"", "\"16 records in 00:00.187\"", Masked(pattern)).Grade.Should().Be(ComparisonGrade.Different);
        Compare("\"15 records in 00:00.263\"", "\"15 records in 00:00.263\"", Masked(pattern)).Grade.Should().Be(ComparisonGrade.Identical);
    }
}
