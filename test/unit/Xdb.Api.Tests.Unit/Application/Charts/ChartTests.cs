using System;
using System.Text;
using System.Text.RegularExpressions;

using FluentAssertions;

using Xdb.Api.Application.Charts.Legacy;
using Xdb.Api.Application.Charts.Queries;
using Xdb.Core.LegacyWire;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.Charts;

public class ChartTests
{
    [Fact]
    public void ChartTypes_MatchTheOriginalBody()
    {
        LegacyFormatters.FormatDictionary(ChartTypes.All).Should().Be(
            "[{\"key\":\"0\",\"value\":\"Area\"},{\"key\":\"1\",\"value\":\"AreaStacked\"},{\"key\":\"2\",\"value\":\"Bar\"},{\"key\":\"3\",\"value\":\"BarStacked\"},"
            + "{\"key\":\"4\",\"value\":\"Column\"},{\"key\":\"5\",\"value\":\"ColumnStacked\"},{\"key\":\"6\",\"value\":\"Donut\"},{\"key\":\"7\",\"value\":\"Line\"},"
            + "{\"key\":\"8\",\"value\":\"Pie\"},{\"key\":\"9\",\"value\":\"ScatterPlot\"}]")
            .And.HaveLength(301);
    }

    [Fact]
    public void NewChart_SerializesInTheOriginalMemberOrder()
    {
        var user = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
        var chart = new ChartContract { Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002"), CreatedBy = user, Created = new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Local), IsNew = true };

        var json = Regex.Replace(Encoding.UTF8.GetString(LegacyWire.TypedBytes(chart)), @"\\/Date\(\d+[-+]\d{4}\)\\/", "DATE");

        json.Should().Be(
            "{\"AppearanceName\":null,\"AssetRequestType\":0,\"AssetType\":null,\"AssetTypeId\":\"00000000-0000-0000-0000-000000000000\",\"ChartTitle\":null,"
            + "\"ChartType\":0,\"Created\":\"DATE\",\"CreatedBy\":\"aaaaaaaa-0000-0000-0000-000000000001\",\"Data\":[],\"DataDictionary\":null,\"Deleted\":null,"
            + "\"DeletedBy\":null,\"FilterLogic\":null,\"Filters\":[],\"FullSql\":null,\"GroupColumns\":[],\"GroupSql\":null,"
            + "\"Id\":\"bbbbbbbb-0000-0000-0000-000000000002\",\"IsInstance\":false,\"IsNew\":true,\"IsPublic\":false,\"LegendText\":null,\"LegendVisible\":false,"
            + "\"Name\":null,\"PaletteName\":null,\"Properties\":[],\"PropertyIds\":[],\"PropertyNames\":[],\"RecordCount\":0,\"ReportingLabel\":null,"
            + "\"SeriesName\":null,\"Subtitle\":null,\"Title\":null,\"ViewType\":null,\"yAxisLabel\":null}");
    }
}
