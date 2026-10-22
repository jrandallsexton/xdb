using System;
using System.Data;

using FluentAssertions;

using Xdb.Api.Application.Reports.Legacy;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.Reports.Legacy;

public class ReportDataJsonTests
{
    private static DataTable Table(params string[] columns)
    {
        var table = new DataTable();
        foreach (var column in columns)
            table.Columns.Add(column, typeof(object));
        return table;
    }

    [Fact]
    public void Envelope_HasTheOriginalSpacingAndStringNumbers()
    {
        var table = Table("AssetId", "Name");
        table.Rows.Add(Guid.Parse("13c637c6-4924-42fc-9b88-f55b622324c3"), "Pump");

        ReportDataJson.Build(table, page: 1, rows: 25).Should().Be(
            "{\"total\" : \"1\",\"page\" :  \"1\",\"startingRec\" :  \"1\",\"endingRec\" :  \"1\",\"records\" :  \"1\",\"rows\" : [" +
            "{\"AssetId\" : \"13c637c6-4924-42fc-9b88-f55b622324c3\",\"Name\" : \"Pump\"}]}");
    }

    [Fact]
    public void AllRowsAreEmitted_WhileTotalAndEndingRecUseTheCallersPageSize()
    {
        var table = Table("A");
        for (var i = 0; i < 56; i++)
            table.Rows.Add(i);

        var json = ReportDataJson.Build(table, page: 1, rows: 25);

        json.Should().StartWith("{\"total\" : \"3\",\"page\" :  \"1\",\"startingRec\" :  \"1\",\"endingRec\" :  \"25\",\"records\" :  \"56\",");
        json.Split("{\"A\" :").Length.Should().Be(57);
    }

    [Fact]
    public void ZeroRows_ProducesTheOriginalsInvalidJson()
    {
        ReportDataJson.Build(Table("A"), page: 1, rows: 25).Should().Be(
            "{\"total\" : \"0\",\"page\" :  \"1\",\"startingRec\" :  \"1\",\"endingRec\" :  \"0\",\"records\" :  \"0\",\"rows\" : ]}");
    }

    [Fact]
    public void Values_EscapeBackslashAndQuote_DropCrLf_KeepTabs()
    {
        var table = Table("A");
        table.Rows.Add("a\\b \"q\"\r\nline\tx");

        ReportDataJson.Build(table, 1, 25).Should().EndWith("[{\"A\" : \"a\\\\b \\\"q\\\"line\tx\"}]}");
    }

    [Fact]
    public void NullsDatesAndNumbers_FormatAsTheOriginal()
    {
        var table = Table("N", "D", "L");
        table.Rows.Add(DBNull.Value, new DateTime(2020, 1, 5, 15, 7, 0), 42L);

        ReportDataJson.Build(table, 1, 25).Should().EndWith("[{\"N\" : \"\",\"D\" : \"1/5/2020 3:07:00 PM\",\"L\" : \"42\"}]}");
    }

    [Fact]
    public void SequentialReplace_SubstitutesPlaceholdersFoundInEarlierValues()
    {
        // The value of column 0 contains column 1's placeholder ("B1" + U+00BE); the original's sequential
        // replace-all substitutes it, so the port must too.
        var table = Table("A", "B");
        table.Rows.Add("x B1¾ y", "Z");

        ReportDataJson.Build(table, 1, 25).Should().EndWith("[{\"A\" : \"x Z y\",\"B\" : \"Z\"}]}");
    }

    [Fact]
    public void PageSizeZero_Throws()
    {
        var act = () => ReportDataJson.Build(Table("A"), 1, 0);

        act.Should().Throw<DivideByZeroException>();
    }
}
