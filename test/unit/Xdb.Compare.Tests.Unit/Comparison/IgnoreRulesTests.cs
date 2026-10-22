using FluentAssertions;

using Xdb.Compare.Comparison;

using Xunit;

namespace Xdb.Compare.Tests.Unit.Comparison;

public class IgnoreRulesTests
{
    [Theory]
    [InlineData("$.rows[*].cell[3]", "$.rows[0].cell[3]", true)]
    [InlineData("$.rows[*].cell[3]", "$.rows[12].cell[3]", true)]
    [InlineData("$.rows[*].cell[3]", "$.rows[0].cell[4]", false)]
    [InlineData("$.*.Duration", "$.Stats.Duration", true)]
    [InlineData("$.*.Duration", "$.Stats.Inner.Duration", false)]
    [InlineData("$..Duration", "$.Stats.Inner.Duration", true)]
    [InlineData("$..Duration", "$.items[3].Duration", true)]
    [InlineData("$..Duration", "$.Duration", true)]
    [InlineData("$.a", "$.ab", false)]
    public void Matches(string pattern, string path, bool expected)
    {
        new IgnoreRules([pattern]).Matches(path).Should().Be(expected);
    }
}
