using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using FluentAssertions;

using Xdb.Compare;
using Xdb.Compare.Comparison;

using Xunit;

namespace Xdb.Compare.Tests.Unit.Comparison;

/// <summary>File responses (M8): binary bodies, Content-Disposition, xlsx packages and expect-different rules.</summary>
public class FileResponseTests
{
    private static ComparisonResult Compare(CapturedResponse legacy, CapturedResponse port, bool xlsx = false) =>
        ResponseComparer.Compare(legacy, port, ComparisonOptions.Strict with { Xlsx = xlsx });

    [Fact]
    public void BinaryBodies_AreReportedByLengthAndHashOnly()
    {
        var result = Compare(new CapturedResponse(200, "image/png", [1, 2, 3]), new CapturedResponse(200, "image/png", [1, 2, 4]));

        var difference = result.Differences.Should().ContainSingle().Which;
        difference.Path.Should().Be("$body(binary)");
        difference.Legacy.Should().StartWith("len=3 sha256=");
    }

    [Fact]
    public void ContentDisposition_IsCompared() =>
        Compare(new CapturedResponse(200, "text/plain", [65], null, "inline; filename=a.txt"), new CapturedResponse(200, "text/plain", [65], null, null))
            .Grade.Should().Be(ComparisonGrade.Different);

    private static byte[] Package(string workbook, string rels, string description, bool bom = false)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            void Add(string name, string text, bool withBom)
            {
                using var stream = zip.CreateEntry(name).Open();
                stream.Write((withBom ? new byte[] { 0xEF, 0xBB, 0xBF } : []).Concat(Encoding.UTF8.GetBytes(text)).ToArray());
            }
            Add("xl/workbook.xml", workbook, false);
            Add("_rels/.rels", rels, bom);
            Add("docProps/core.xml", description, true);
        }
        return buffer.ToArray();
    }

    private const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [Fact]
    public void Xlsx_RandomIdsClockAndWidthsAreMasked()
    {
        var legacy = Package("<sheet r:id=\"R0123456789abcdef\" /><col min=\"1\" width=\"13.4\" />", "Id=\"R0123456789abcdef\"",
            "<dc:description>made on 10/23/2026 7:29:21 AM</dc:description>");
        var port = Package("<sheet r:id=\"Rfedcba9876543210\" /><col min=\"1\" width=\"9.140625\" />", "Id=\"Rfedcba9876543210\"",
            "<dc:description>made on 10/24/2026 1:00:00 PM</dc:description>");

        Compare(new CapturedResponse(200, Xlsx, legacy, null, "attachment; filename=R_23Oct2026.xlsx"),
                new CapturedResponse(200, Xlsx, port, null, "attachment; filename=R_24Oct2026.xlsx"), xlsx: true)
            .Grade.Should().Be(ComparisonGrade.Equivalent);
    }

    [Fact]
    public void Xlsx_ContentDifferencesAndBomsCount()
    {
        var legacy = Package("<c><v>1</v></c>", "x", "d");
        Compare(new CapturedResponse(200, Xlsx, legacy), new CapturedResponse(200, Xlsx, Package("<c><v>2</v></c>", "x", "d")), xlsx: true)
            .Grade.Should().Be(ComparisonGrade.Different);
        Compare(new CapturedResponse(200, Xlsx, legacy), new CapturedResponse(200, Xlsx, Package("<c><v>1</v></c>", "x", "d", bom: true)), xlsx: true)
            .Grade.Should().Be(ComparisonGrade.Different);
    }

    [Fact]
    public void ExpectDifferentRule_MatchesOperationAndUrl()
    {
        var rule = new ExpectDifferentRule("Image_Upload", "(?i)^Image_Upload\\?imageId=8e8e", "D1");
        rule.Matches("image_upload", "Image_Upload?imageId=8e8e0001").Should().BeTrue();
        rule.Matches("Image_Upload", "Image_Upload?imageId=1234").Should().BeFalse();
        rule.Matches("UploadDocument", "Image_Upload?imageId=8e8e0001").Should().BeFalse();
    }
}
