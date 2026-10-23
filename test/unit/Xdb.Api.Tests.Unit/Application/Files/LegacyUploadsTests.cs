using System;
using System.Linq;
using System.Text;

using FluentAssertions;

using Xdb.Api.Application.Files;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.Files;

/// <summary>The original's multipart handling and file-name rules, from the FX measurements (spec M8-F appendix B.3/B.4).</summary>
public class LegacyUploadsTests
{
    private const string Boundary = "----m8boundary7MA4YWxkTrZu0gW";
    private const string Standard = "--" + Boundary + "\r\nContent-Disposition: form-data; name=\"files[]\"; filename=\"m8-small.png\"\r\nContent-Type: image/png\r\n\r\n";
    private const string End = "\r\n--" + Boundary + "--\r\n";

    /// <summary>806 synthetic bytes without CR, like the FX payload's length.</summary>
    private static readonly byte[] Payload = Enumerable.Range(0, 806).Select(i => (byte)(i * 7 % 256 == 13 ? 14 : i * 7 % 256)).ToArray();

    private static readonly Encoding Latin = Encoding.Latin1;

    private static byte[] Body(string head, byte[] payload, string tail) => [.. Latin.GetBytes(head), .. payload, .. Latin.GetBytes(tail)];

    private static readonly byte[] CrLfCrLf = [13, 10, 13, 10];

    [Fact]
    public void BrowserShape_StoresThePayloadPlusCrLfCrLf_G1() =>
        LegacyMultipart.GetFile(Body(Standard, Payload, End)).Should().Equal([.. Payload, .. CrLfCrLf]);

    [Fact]
    public void AllByteValues_Survive_G2()
    {
        var all = Enumerable.Range(0, 1024).Select(i => (byte)(i % 256)).ToArray();
        LegacyMultipart.GetFile(Body(Standard, all, End)).Should().Equal([.. all, .. CrLfCrLf]);
    }

    [Theory]
    [InlineData("G3 no part Content-Type", "--" + Boundary + "\r\nContent-Disposition: form-data; name=\"files[]\"; filename=\"a.png\"\r\n\r\n", End, 2)]
    [InlineData("G4 extra header", "--" + Boundary + "\r\nContent-Disposition: form-data; name=\"f\"; filename=\"a.png\"\r\nContent-Type: image/png\r\nContent-Transfer-Encoding: binary\r\n\r\n", End, 39)]
    [InlineData("G6 no closing delimiter", Standard, "", 0)]
    [InlineData("G7 closing without CRLF", Standard, "\r\n--" + Boundary + "--", 2)]
    [InlineData("G8 two parts", Standard, "\r\n--" + Boundary + "\r\nContent-Disposition: form-data; name=\"x\"\r\n\r\nv\r\n--" + Boundary + "--\r\n", 84)]
    [InlineData("G11 CRLF first", "\r\n" + Standard, End, 54)]
    [InlineData("G15 soft hyphen line end", "--" + Boundary + "\r\nContent-Disposition: form-data; name=\"f\"; filename=\"a\r\u00ad\n.png\"\r\nContent-Type: image/png\r\n\r\n", End, 29)]
    [InlineData("G16 delimiter then blank lines", "--" + Boundary + "\r\n\r\n\r\n", End, 2)]
    public void StoredLength(string label, string head, string tail, int extraBytes) =>
        LegacyMultipart.GetFile(Body(head, Payload, tail)).Length.Should().Be(Payload.Length + extraBytes, label);

    [Fact]
    public void DelimiterInsideThePayload_IsRemoved_G12() =>
        LegacyMultipart.GetFile(Body(Standard, Encoding.ASCII.GetBytes("x\r\n--" + Boundary + "--\r\ny"), End))
            .Should().Equal([.. Encoding.ASCII.GetBytes("x\r\n\r\ny"), .. CrLfCrLf]);

    [Fact]
    public void EmptyPayload_G13() => LegacyMultipart.GetFile(Body(Standard, [], End)).Should().Equal(CrLfCrLf);

    [Theory]
    [InlineData("G5 LF only")]
    [InlineData("G9 empty")]
    [InlineData("G10 one line")]
    [InlineData("G14 JSON")]
    public void Unparseable_Faults(string label)
    {
        var body = label switch
        {
            "G5 LF only" => Body("--" + Boundary + "\nContent-Disposition: form-data; name=\"f\"; filename=\"a.png\"\nContent-Type: image/png\n\n", Payload, "\n--" + Boundary + "--\n"),
            "G9 empty" => [],
            "G10 one line" => Encoding.ASCII.GetBytes("--" + Boundary + "\r\n"),
            _ => Encoding.ASCII.GetBytes("{\"a\":1}")
        };
        FluentActions.Invoking(() => LegacyMultipart.GetFile(body)).Should().Throw<ArgumentOutOfRangeException>(label);
    }

    [Theory]
    [InlineData("a.png", 4)]
    [InlineData("A.PNG", 4)]
    [InlineData("a.Png", 4)]
    [InlineData("a.jpeg", 3)]
    [InlineData("a.jpg", 3)]
    [InlineData("a.jpe", -1)]
    [InlineData("a.gif", 1)]
    [InlineData("a.bmp", 0)]
    [InlineData("a.ico", 2)]
    [InlineData("a.tif", 5)]
    [InlineData("a.tiff", -1)]
    [InlineData("a.png ", -1)]
    [InlineData("dir.png\\a", -1)]
    [InlineData("noext", -1)]
    public void ImageType(string name, int expected) => LegacyFileNames.ImageType(name).Should().Be(expected);

    [Theory]
    [InlineData("a\".png")]
    [InlineData("a<.png")]
    [InlineData("a|.png")]
    [InlineData("a\u0001.png")]
    public void IllegalPathCharacters_Throw(string name) =>
        FluentActions.Invoking(() => LegacyFileNames.ImageType(name)).Should().Throw<ArgumentException>();

    [Fact]
    public void NullName_IsANullReference() => FluentActions.Invoking(() => LegacyFileNames.DocumentType(null)).Should().Throw<NullReferenceException>();

    [Theory]
    [InlineData("C:\\fakepath\\a.png", "a.png")]
    [InlineData("x\\", "")]
    [InlineData("a/b.png", "a/b.png")]
    public void FormatNameTitle(string text, string expected) => LegacyFileNames.FormatNameTitle(text).Should().Be(expected);

    [Fact]
    public void NameAndTitleTruncation()
    {
        LegacyFileNames.Name(new string('n', 50)).Should().HaveLength(50);
        LegacyFileNames.Name(new string('n', 51)).Should().HaveLength(49);
        LegacyFileNames.Title(new string('t', 256)).Should().HaveLength(254);
    }

    [Fact]
    public void UploadJson_IsTheOriginalText() =>
        LegacyUploadJson.Success("a.txt", 12, Guid.Empty, "u").Should().Be(
            "[\r\n{\r\n\"name\": \"a.txt\",\r\n\"size\": 12,\r\n\"url\": \"\",\r\n\"thumbnail_url\": \"\",\r\n\"delete_url\": \"//example.org/upload-handler?file=picture1.jpg\",\r\n"
            + "\"delete_type\": \"DELETE\",\r\n\"error\": \"\",\r\n\"propertyId\": \"00000000-0000-0000-0000-000000000000\",\r\n\"uploadId\": \"u\"\r\n}\r\n]\r\n");
}
