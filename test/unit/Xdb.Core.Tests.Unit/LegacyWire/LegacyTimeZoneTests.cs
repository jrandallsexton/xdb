using System;

using FluentAssertions;

using Xdb.Core.LegacyWire;

using Xunit;

namespace Xdb.Core.Tests.Unit.LegacyWire;

/// <summary>The local zone is Windows' "Eastern Standard Time" on any host (bundled zone file on Linux).</summary>
public class LegacyTimeZoneTests
{
    public LegacyTimeZoneTests() => LegacyTimeZone.Apply();

    [Theory]
    [InlineData(1, 1, 15, -5)]      // year 1: standard time, not local mean time
    [InlineData(1, 7, 15, -4)]      // the pre-2007 rule applies to every earlier year
    [InlineData(1918, 3, 31, -5)]   // no historic wartime/early rules
    [InlineData(2006, 3, 20, -5)]   // first Sunday of April rule
    [InlineData(2006, 10, 30, -5)]  // last Sunday of October rule
    [InlineData(2007, 3, 20, -4)]   // second Sunday of March rule
    [InlineData(2007, 11, 2, -4)]   // first Sunday of November rule
    [InlineData(2026, 10, 23, -4)]
    [InlineData(2050, 1, 15, -5)]   // beyond the written transitions
    [InlineData(2050, 7, 15, -4)]
    public void LocalOffset(int year, int month, int day, int hours) =>
        TimeZoneInfo.Local.GetUtcOffset(new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Unspecified)).Should().Be(TimeSpan.FromHours(hours));
}
