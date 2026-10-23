using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Xdb.Api.Application.Reports.Export;

/// <summary>
/// Report_Export's workbook (the original's DumpExcel, written with EPPlus 2.9): one sheet named after the report,
/// a gray header row with an auto filter, frozen below the header, Times New Roman 10 with thin borders, and
/// optional grouping rows. Every part's XML text, entry order and BOMs are EPPlus 2.9's (spec M8-B section 5).
/// </summary>
/// <remarks>
/// DEFERRED (Windows-only dependency, owner decision 2026-10-23): the original sized every column by measuring its
/// texts with GDI+ (System.Drawing). The port writes the default width (9.140625) for every column.
/// </remarks>
public static class ReportWorkbook
{
    private const string Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string DefaultWidth = "9.140625";
    private const int MaxColumn = 16384;
    private const int HeaderStyle = 2;
    private const int DataStyle = 1;
    private const int GroupStyle = 3;

    /// <summary>Excel's sheet-name limit; EPPlus cut longer names.</summary>
    private const int MaxSheetName = 31;

    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    /// <summary>A cell: a value (string, bool, number) or null for an empty styled cell.</summary>
    private sealed record Cell(object? Value);

    private sealed record Row(int Style, Cell[] Cells);

    /// <param name="now">The host's local time, written in the description property.</param>
    /// <param name="relationshipId">The package's relationship ids (random "R" + 16 hex in the original).</param>
    public static byte[] Write(DataTable table, string reportName, string? groupColName, string author, string productName, string company, DateTime now,
        Func<string> relationshipId)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, text, bom, stored) in Parts(table, reportName, groupColName, author, productName, company, now, relationshipId))
            {
                var entry = zip.CreateEntry(name, stored ? CompressionLevel.NoCompression : CompressionLevel.Optimal);
                using var stream = entry.Open();
                var bytes = (bom ? new UTF8Encoding(true) : new UTF8Encoding(false)).GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray();
                stream.Write(bytes);
            }
        }
        return buffer.ToArray();
    }

    /// <summary>The package parts in the original's entry order: name, text, UTF-8 BOM, stored (not deflated).</summary>
    public static IReadOnlyList<(string Name, string Text, bool Bom, bool Stored)> Parts(DataTable table, string reportName, string? groupColName, string author,
        string productName, string company, DateTime now, Func<string> relationshipId)
    {
        var (columns, rows, groupColumn) = Layout(table, groupColName);
        var strings = new List<string>();
        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            foreach (var cell in row.Cells)
            {
                if (cell.Value is string text && !index.ContainsKey(text))
                {
                    index[text] = strings.Count;
                    strings.Add(text);
                }
            }
        }

        var lastColumn = ColumnName(columns);
        var hasGroupRows = rows.Any(r => r.Style == GroupStyle);
        string sheetRel = relationshipId(), sharedRel = relationshipId(), stylesRel = relationshipId();
        string officeRel = relationshipId(), coreRel = relationshipId(), appRel = relationshipId(), customRel = relationshipId();

        return
        [
            ("xl/workbook.xml", Workbook(reportName.Length > MaxSheetName ? reportName[..MaxSheetName] : reportName, lastColumn, sheetRel), false, false),
            ("[Content_Types].xml", ContentTypes(), true, false),
            ("xl/sharedStrings.xml", SharedStrings(strings), false, false),
            ("xl/_rels/workbook.xml.rels", WorkbookRels(sharedRel, sheetRel, stylesRel), true, false),
            ("_rels/.rels", PackageRels(officeRel, coreRel, appRel, customRel), true, true),
            ("xl/worksheets/sheet1.xml", Sheet(columns, rows, groupColumn, lastColumn, index), false, false),
            ("xl/styles.xml", Styles(hasGroupRows), false, false),
            ("docProps/core.xml", Core(reportName, author, $"This was a report generated within {productName} on {now.ToString("M/d/yyyy h:mm:ss tt", EnUs)}"), true, true),
            ("docProps/app.xml", App(company), true, true),
            ("docProps/custom.xml", Custom(), true, true)
        ];
    }

    /// <summary>
    /// DumpExcel's table work: drop "rn" then "AssetId" (first column only), sort by the group column and insert a
    /// "GroupCol" column first, load the rows, then insert a group heading row before each run of equal values.
    /// </summary>
    private static (int Columns, List<Row> Rows, int GroupColumn) Layout(DataTable table, string? groupColName)
    {
        table.PrimaryKey = null;
        if (table.Columns.Count > 0 && table.Columns[0].ColumnName == "rn")
            table.Columns.RemoveAt(0);
        if (table.Columns.Count > 0 && table.Columns[0].ColumnName == "AssetId")
            table.Columns.RemoveAt(0);
        var groupColumn = -1;
        if (!string.IsNullOrEmpty(groupColName))
        {
            table.Locale = EnUs;
            table.DefaultView.Sort = groupColName;
            table = table.DefaultView.ToTable();
            table.Columns.Add("GroupCol").SetOrdinal(0);
            groupColumn = table.Columns[groupColName]!.Ordinal + 1;
        }
        if (table.Columns.Count == 0)
            throw new NullReferenceException("Object reference not set to an instance of an object.");

        var columns = table.Columns.Count;
        var rows = new List<Row> { new(HeaderStyle, table.Columns.Cast<DataColumn>().Select(c => new Cell(c.ColumnName)).ToArray()) };
        foreach (DataRow dataRow in table.Rows)
            rows.Add(new Row(DataStyle, dataRow.ItemArray.Select(v => new Cell(Encode(v ?? DBNull.Value))).ToArray()));

        if (groupColumn > 0)
        {
            var previous = string.Empty;
            for (var i = 1; i < rows.Count; i++)
            {
                var value = rows[i].Cells[groupColumn - 1].Value;
                var text = value is null || string.IsNullOrEmpty(Text(value)) ? "[No value]" : Text(value);
                if (text == previous)
                    continue;
                previous = text;
                var heading = new Cell[columns];
                heading[0] = new Cell(groupColName + ": " + text);
                for (var c = 1; c < columns; c++)
                    heading[c] = new Cell(null);
                rows.Insert(i, new Row(GroupStyle, heading));
            }
        }
        return (columns, rows, groupColumn);
    }

    /// <summary>LoadFromDataTable's cell values: strings for text, Guids, byte arrays and DBNull (""), numbers as doubles, dates as OLE dates.</summary>
    private static object Encode(object value) => value switch
    {
        DBNull => string.Empty,
        string s => s,
        bool b => b,
        DateTime d => d.ToOADate(),
        byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal => Convert.ToDouble(value, CultureInfo.InvariantCulture),
        byte[] => "System.Byte[]",
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
    };

    /// <summary>The grouping test reads the cell value's text (dates and numbers as the host culture writes them).</summary>
    private static string Text(object value) => value switch
    {
        double d => d.ToString(EnUs),
        bool b => b.ToString(),
        _ => (string)value
    };

    public static string ColumnName(int column)
    {
        var name = string.Empty;
        while (column > 0)
        {
            var remainder = (column - 1) % 26;
            name = (char)('A' + remainder) + name;
            column = (column - 1) / 26;
        }
        return name;
    }

    private static string Workbook(string sheetName, string lastColumn, string sheetRel) =>
        string.Join("\r\n",
            $"<workbook xmlns:r=\"{Relationships}\" xmlns=\"{Main}\">",
            "  <bookViews>",
            "    <workbookView />",
            "  </bookViews>",
            "  <sheets>",
            $"    <sheet name=\"{Attribute(sheetName)}\" sheetId=\"1\" r:id=\"{sheetRel}\" />",
            "  </sheets>",
            "  <definedNames>",
            $"    <definedName name=\"_xlnm._FilterDatabase\" localSheetId=\"0\" hidden=\"1\">{Element($"'{sheetName}'!$A$1:${lastColumn}$1")}</definedName>",
            "  </definedNames>",
            "</workbook>");

    private static string ContentTypes() =>
        "<?xml version=\"1.0\" encoding=\"utf-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">"
        + "<Default Extension=\"xml\" ContentType=\"application/xml\" />"
        + "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\" />"
        + "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\" />"
        + "<Override PartName=\"/xl/sharedStrings.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml\" />"
        + "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\" />"
        + "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\" />"
        + "<Override PartName=\"/docProps/core.xml\" ContentType=\"application/vnd.openxmlformats-package.core-properties+xml\" />"
        + "<Override PartName=\"/docProps/app.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.extended-properties+xml\" />"
        + "<Override PartName=\"/docProps/custom.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.custom-properties+xml\" /></Types>";

    private static string SharedStrings(List<string> strings)
    {
        var sb = new StringBuilder();
        sb.Append($"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\" ?><sst xmlns=\"{Main}\" count=\"{strings.Count}\" uniqueCount=\"{strings.Count}\">");
        foreach (var text in strings)
            sb.Append(Preserve(text) ? "<si><t xml:space=\"preserve\">" : "<si><t>").Append(SharedText(text)).Append("</t></si>");
        return sb.Append("</sst>").ToString();
    }

    /// <summary>xml:space="preserve" for a leading or trailing space, a double space or a tab (not for a line feed).</summary>
    private static bool Preserve(string text) =>
        text.Length > 0 && (text[0] == ' ' || text[^1] == ' ' || text.Contains("  ", StringComparison.Ordinal) || text.Contains('\t'));

    /// <summary>EPPlus's shared-string escaping: entities for &amp; &lt; &gt; " ', _xHHHH_ for control characters other than LF and TAB, a literal _xHHHH_ escaped as _x005F_xHHHH_.</summary>
    public static string SharedText(string text)
    {
        var sb = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            switch (c)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '"': sb.Append("&quot;"); break;
                case '\'': sb.Append("&apos;"); break;
                case '\n':
                case '\t':
                    sb.Append(c);
                    break;
                case '_' when IsEscapeSequence(text, i):
                    sb.Append("_x005F_");
                    break;
                default:
                    if (c < ' ')
                        sb.Append("_x").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture)).Append('_');
                    else
                        sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    private static bool IsEscapeSequence(string text, int i) =>
        i + 6 < text.Length && text[i + 1] == 'x' && text[i + 6] == '_' && text.Substring(i + 2, 4).All(char.IsAsciiHexDigit);

    private static string WorkbookRels(string sharedRel, string sheetRel, string stylesRel) =>
        "<?xml version=\"1.0\" encoding=\"utf-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
        + $"<Relationship Type=\"{Relationships}/sharedStrings\" Target=\"sharedStrings.xml\" Id=\"{sharedRel}\" />"
        + $"<Relationship Type=\"{Relationships}/worksheet\" Target=\"worksheets/sheet1.xml\" Id=\"{sheetRel}\" />"
        + $"<Relationship Type=\"{Relationships}/styles\" Target=\"styles.xml\" Id=\"{stylesRel}\" /></Relationships>";

    private static string PackageRels(string officeRel, string coreRel, string appRel, string customRel) =>
        "<?xml version=\"1.0\" encoding=\"utf-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
        + $"<Relationship Type=\"{Relationships}/officeDocument\" Target=\"xl/workbook.xml\" Id=\"{officeRel}\" />"
        + $"<Relationship Type=\"http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties\" Target=\"docProps/core.xml\" Id=\"{coreRel}\" />"
        + $"<Relationship Type=\"{Relationships}/extended-properties\" Target=\"docProps/app.xml\" Id=\"{appRel}\" />"
        + $"<Relationship Type=\"{Relationships}/custom-properties\" Target=\"docProps/custom.xml\" Id=\"{customRel}\" /></Relationships>";

    private static string Sheet(int columns, List<Row> rows, int groupColumn, string lastColumn, Dictionary<string, int> strings)
    {
        var sb = new StringBuilder();
        sb.Append($"<?xml version=\"1.0\" encoding=\"utf-8\"?><worksheet xmlns:r=\"{Relationships}\" xmlns=\"{Main}\">");
        sb.Append($"<dimension ref=\"A1:{lastColumn}{rows.Count}\" />");
        sb.Append("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" state=\"frozen\" activePane=\"bottomLeft\" />"
            + "<selection pane=\"bottomLeft\" activeCell=\"A1\" sqref=\"A1\" /></sheetView></sheetViews>");
        sb.Append("<cols>");
        // Columns get their own entry when a data row's width was measured; with no data rows only column 1 does.
        var measured = rows.Count > 1 ? columns : 1;
        for (var c = 1; c <= measured; c++)
            sb.Append($"<col min=\"{c}\" max=\"{c}\"{(c == groupColumn ? " hidden=\"1\"" : string.Empty)} width=\"{DefaultWidth}\" customWidth=\"1\" style=\"{DataStyle}\" />");
        if (measured < MaxColumn)
            sb.Append($"<col min=\"{measured + 1}\" max=\"{MaxColumn}\" width=\"{DefaultWidth}\" customWidth=\"1\" style=\"{DataStyle}\" />");
        sb.Append("</cols><sheetData>");
        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            sb.Append($"<row r=\"{r + 1}\" >");
            for (var c = 0; c < row.Cells.Length; c++)
            {
                var reference = ColumnName(c + 1) + (r + 1).ToString(CultureInfo.InvariantCulture);
                switch (row.Cells[c].Value)
                {
                    case null:
                        sb.Append($"<c r=\"{reference}\" s=\"{row.Style}\" />");
                        break;
                    case string text:
                        sb.Append($"<c r=\"{reference}\" s=\"{row.Style}\" t=\"s\"><v>{strings[text]}</v></c>");
                        break;
                    case bool b:
                        sb.Append($"<c r=\"{reference}\" s=\"{row.Style}\" t=\"b\"><v>{(b ? 1 : 0)}</v></c>");
                        break;
                    case double d:
                        sb.Append($"<c r=\"{reference}\" s=\"{row.Style}\"><v>{(double.IsNaN(d) ? "0" : d.ToString("g15", CultureInfo.InvariantCulture))}</v></c>");
                        break;
                }
            }
            sb.Append("</row>");
        }
        sb.Append($"</sheetData><autoFilter ref=\"A1:{lastColumn}1\" /></worksheet>");
        return sb.ToString();
    }

    private static string Styles(bool hasGroupRows)
    {
        var lines = new List<string>
        {
            $"<styleSheet xmlns:d=\"{Main}\" xmlns=\"{Main}\">",
            "  <d:numFmts count=\"0\" />",
            "  <d:fonts count=\"3\">",
            "    <font>", "      <sz val=\"11\" />", "      <name val=\"Calibri\" />", "    </font>",
            "    <font>", "      <sz val=\"10\" />", "      <name val=\"Times New Roman\" />", "    </font>",
            "    <font>", "      <sz val=\"10\" />", "      <color rgb=\"FFFFFFFF\" />", "      <name val=\"Times New Roman\" />", "    </font>",
            "  </d:fonts>",
            $"  <d:fills count=\"{(hasGroupRows ? 4 : 3)}\">",
            "    <fill>", "      <patternFill patternType=\"none\" />", "    </fill>",
            "    <fill>", "      <patternFill patternType=\"none\" />", "    </fill>",
            "    <fill>", "      <patternFill patternType=\"solid\">", "        <fgColor rgb=\"FF808080\" />", "      </patternFill>", "    </fill>"
        };
        if (hasGroupRows)
            lines.AddRange(["    <fill>", "      <patternFill patternType=\"solid\">", "        <fgColor rgb=\"FFD3D3D3\" />", "      </patternFill>", "    </fill>"]);
        lines.AddRange(
        [
            "  </d:fills>",
            "  <d:borders count=\"2\">",
            "    <border>", "      <left />", "      <right />", "      <top />", "      <bottom />", "      <diagonal />", "    </border>",
            "    <border>", "      <left style=\"thin\" />", "      <right style=\"thin\" />", "      <top style=\"thin\" />", "      <bottom style=\"thin\" />",
            "      <diagonal />", "    </border>",
            "  </d:borders>",
            "  <d:cellStyleXfs count=\"1\">", "    <xf numFmtId=\"0\" fontId=\"0\" />", "  </d:cellStyleXfs>",
            $"  <d:cellXfs count=\"{(hasGroupRows ? 4 : 3)}\">",
            "    <xf numFmtId=\"0\" fontId=\"0\" xfId=\"0\" />",
            "    <xf numFmtId=\"0\" fontId=\"1\" borderId=\"1\" xfId=\"0\" />",
            "    <xf numFmtId=\"0\" fontId=\"2\" fillId=\"2\" borderId=\"1\" xfId=\"0\" />"
        ]);
        if (hasGroupRows)
            lines.Add("    <xf numFmtId=\"0\" fontId=\"1\" fillId=\"3\" borderId=\"0\" xfId=\"0\" />");
        lines.AddRange(["  </d:cellXfs>", "  <d:cellStyles count=\"1\">", "    <cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\" />", "  </d:cellStyles>", "</styleSheet>"]);
        return string.Join("\r\n", lines);
    }

    private static string Core(string title, string author, string description) =>
        string.Join("\r\n",
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>",
            "<cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" "
            + "xmlns:dcterms=\"http://purl.org/dc/terms/\" xmlns:dcmitype=\"http://purl.org/dc/dcmitype/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">",
            Property("dc:title", title),
            Property("dc:creator", author),
            Property("dc:description", description),
            "</cp:coreProperties>");

    private static string App(string company) =>
        string.Join("\r\n",
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>",
            "<Properties xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\" xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\">",
            Property("Company", company),
            "</Properties>");

    private static string Custom() =>
        string.Join("\r\n",
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>",
            "<Properties xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\" xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/custom-properties\">",
            "</Properties>");

    /// <summary>A document property element; an empty one kept the indentation inside it (EPPlus's XmlDocument).</summary>
    private static string Property(string name, string text) =>
        text.Length == 0 ? $"  <{name}>\r\n  </{name}>" : $"  <{name}>{Element(text)}</{name}>";

    private static string Element(string text) => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static string Attribute(string text) => Element(text).Replace("\"", "&quot;");
}
