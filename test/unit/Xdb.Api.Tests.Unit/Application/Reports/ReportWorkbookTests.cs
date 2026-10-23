using System;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using FluentAssertions;

using Xdb.Api.Application.Reports.Export;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.Reports;

/// <summary>
/// Report_Export's workbook against EPPlus 2.9 goldens built from the same synthetic table (spec M8-B 4-5). Random
/// relationship ids are normalized and column widths masked (GDI+ measurement, deferred).
/// </summary>
public class ReportWorkbookTests
{
    private static readonly DateTime GoldenTime = new(2026, 10, 23, 7, 29, 21);

    private static DataTable Table()
    {
        var t = new DataTable("r");
        t.Columns.Add("rn", typeof(long));
        t.Columns.Add("AssetId", typeof(Guid));
        t.Columns.Add("Name", typeof(string));
        t.Columns.Add("Cost", typeof(decimal));
        t.Columns.Add("When", typeof(DateTime));
        t.Columns.Add("Grp", typeof(string));
        t.Columns.Add("Cnt", typeof(int));
        t.Rows.Add(1L, Guid.Empty, "syn one", 12.5m, new DateTime(2013, 1, 2), "g2", 3);
        t.Rows.Add(2L, Guid.Empty, "syn two", null, null, null, null);
        t.Rows.Add(3L, Guid.Empty, "syn & <three>", 0m, new DateTime(2013, 1, 2, 13, 5, 0), "g1", -1);
        return t;
    }

    private static string Normalize(string text) =>
        Regex.Replace(Regex.Replace(text, "R[0-9a-f]{16}", "R"), "width=\"[0-9.]+\"", "width=\"W\"");

    [Theory]
    [InlineData("report.xlsx", "")]
    [InlineData("report-grouped.xlsx", "Grp")]
    public void PartsMatchTheGolden(string golden, string groupColumn)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Application", "Reports", "Golden", golden);
        using var expected = ZipFile.OpenRead(path);
        var counter = 0;
        var parts = ReportWorkbook.Parts(Table(), "Syn Report", groupColumn, "AUTHOR", "PRODUCT", "COMPANY", GoldenTime, () => $"R{++counter:x16}");

        parts.Select(p => p.Name).Should().Equal(expected.Entries.Select(e => e.FullName));
        foreach (var (name, text, bom, _) in parts)
        {
            using var stream = expected.GetEntry(name)!.Open();
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            var bytes = buffer.ToArray();
            var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            hasBom.Should().Be(bom, name);
            Normalize(text).Should().Be(Normalize(Encoding.UTF8.GetString(bytes, hasBom ? 3 : 0, bytes.Length - (hasBom ? 3 : 0))), name);
        }
    }

    [Fact]
    public void Write_ProducesAReadablePackageInTheOriginalOrder()
    {
        var counter = 0;
        var bytes = ReportWorkbook.Write(Table(), "Syn Report", null, "AUTHOR", "PRODUCT", "COMPANY", GoldenTime, () => $"R{++counter:x16}");

        using var zip = new ZipArchive(new MemoryStream(bytes));
        zip.Entries.Select(e => e.FullName).Should().StartWith(["xl/workbook.xml", "[Content_Types].xml", "xl/sharedStrings.xml"]);
    }

    [Theory]
    [InlineData("a&b<c>\"d'", "a&amp;b&lt;c&gt;&quot;d&apos;")]
    [InlineData("x\ry", "x_x000D_y")]
    [InlineData("x\u0001y", "x_x0001_y")]
    [InlineData("_x0041_", "_x005F_x0041_")]
    [InlineData("tab\there\nline", "tab\there\nline")]
    public void SharedStringEscaping(string text, string expected) => ReportWorkbook.SharedText(text).Should().Be(expected);

    [Theory]
    [InlineData(1, "A")]
    [InlineData(26, "Z")]
    [InlineData(27, "AA")]
    [InlineData(16384, "XFD")]
    public void ColumnNames(int column, string expected) => ReportWorkbook.ColumnName(column).Should().Be(expected);
}
