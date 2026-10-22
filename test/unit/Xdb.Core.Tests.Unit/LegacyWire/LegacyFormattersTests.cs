using System;
using System.Collections.Generic;

using FluentAssertions;

using Xdb.Core.LegacyWire;

using Xunit;

namespace Xdb.Core.Tests.Unit.LegacyWire;

public class LegacyFormattersTests
{
    private static readonly Guid A = new("AAAAAAAA-0000-0000-0000-000000000001");
    private static readonly Guid B = new("BBBBBBBB-0000-0000-0000-000000000002");

    [Fact]
    public void FormatDictionary_EscapesOnlyQuotes_KeepsOrder_LowercaseKeys()
    {
        LegacyFormatters.FormatDictionary([new KeyValuePair<Guid, string>(B, "say \"hi\" \\ / x"), new KeyValuePair<Guid, string>(A, "a")])
            .Should().Be("[{\"key\":\"bbbbbbbb-0000-0000-0000-000000000002\",\"value\":\"say \\\"hi\\\" \\ / x\"},{\"key\":\"aaaaaaaa-0000-0000-0000-000000000001\",\"value\":\"a\"}]");
    }

    [Fact]
    public void FormatDictionary_Empty_IsBrackets()
    {
        LegacyFormatters.FormatDictionary(new List<KeyValuePair<Guid, string>>()).Should().Be("[]");
        LegacyFormatters.FormatDictionary(new List<KeyValuePair<int, string>>()).Should().Be("[]");
    }

    [Fact]
    public void FormatDictionary_IntAndGuidValues()
    {
        LegacyFormatters.FormatDictionary([new KeyValuePair<int, string>(-1, "x"), new KeyValuePair<int, string>(0, "=")])
            .Should().Be("[{\"key\":\"-1\",\"value\":\"x\"},{\"key\":\"0\",\"value\":\"=\"}]");
        LegacyFormatters.FormatDictionary([new KeyValuePair<Guid, Guid>(A, B)])
            .Should().Be("[{\"key\":\"aaaaaaaa-0000-0000-0000-000000000001\",\"value\":\"bbbbbbbb-0000-0000-0000-000000000002\"}]");
    }

    [Fact]
    public void FormatGroupedDictionary_MatchesTheOriginalLayout()
    {
        IReadOnlyList<KeyValuePair<Guid, string>> first = [new(A, "one \"q\""), new(B, "two")];
        IReadOnlyList<KeyValuePair<Guid, string>> second = [new(B, "x")];

        LegacyFormatters.FormatGroupedDictionary([new("[No Value]", first), new("k\"ey", second)])
            .Should().Be("[\r\n{\r\n\"key\":\"[No Value]\",\"values\": [\r\n"
                + "{\"key\":\"aaaaaaaa-0000-0000-0000-000000000001\",\"value\":\"one \\\"q\\\"\"},"
                + "{\"key\":\"bbbbbbbb-0000-0000-0000-000000000002\",\"value\":\"two\"}]},"
                + "{\r\n\"key\":\"k\"ey\",\"values\": [\r\n"
                + "{\"key\":\"bbbbbbbb-0000-0000-0000-000000000002\",\"value\":\"x\"}]}]");
        LegacyFormatters.FormatGroupedDictionary([]).Should().Be("[]");
    }
}
