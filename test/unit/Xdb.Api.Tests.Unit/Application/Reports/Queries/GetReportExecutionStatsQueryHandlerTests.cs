using System;

using FluentAssertions;

using Xdb.Api.Application.Reports.Queries.GetReportExecutionStats;
using Xdb.Api.Infrastructure.Data.Procedures;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.Reports.Queries;

public class GetReportExecutionStatsQueryHandlerTests
{
    // Normal run: (Started - Completed) wraps to 23:59:59.x, (Completed - Started) is the elapsed time.
    private static ReportExecutionStatsRow Row(int? records, TimeSpan? elapsed, bool cached = false) =>
        new(records, cached,
            elapsed is null ? null : TimeSpan.FromDays(1) - elapsed.Value,
            elapsed);

    [Fact]
    public void NormalRun()
    {
        GetReportExecutionStatsQueryHandler.Format(Row(15, TimeSpan.FromTicks(1_233_333))).Should().Be("15 records in 00:00.123 (mm:ss:ms)");
    }

    [Fact]
    public void ThousandsSeparator()
    {
        GetReportExecutionStatsQueryHandler.Format(Row(1234, TimeSpan.FromTicks(1_233_333))).Should().StartWith("1,234 records");
    }

    [Fact]
    public void ZeroRecords_RenderAsEmpty()
    {
        GetReportExecutionStatsQueryHandler.Format(Row(0, TimeSpan.FromTicks(1_233_333))).Should().Be(" records in 00:00.123 (mm:ss:ms)");
    }

    [Fact]
    public void WholeSecond_CollapsesToOneCharacter()
    {
        GetReportExecutionStatsQueryHandler.Format(Row(5, TimeSpan.FromSeconds(1))).Should().Be("5 records in 0 (mm:ss:ms)");
    }

    [Fact]
    public void FromCache_UsesTheOtherMessage()
    {
        GetReportExecutionStatsQueryHandler.Format(Row(5, TimeSpan.FromTicks(1_233_333), cached: true))
            .Should().Be("5 records (from cache) in 00:00.123 (mm:ss:ms)");
    }

    [Fact]
    public void MissingRecordCount_RendersMinusOne()
    {
        GetReportExecutionStatsQueryHandler.Format(Row(null, TimeSpan.FromTicks(1_233_333))).Should().StartWith("-1 records");
    }

    [Fact]
    public void LogNotClosedYet_Throws()
    {
        var act = () => GetReportExecutionStatsQueryHandler.Format(new ReportExecutionStatsRow(0, false, null, null));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
