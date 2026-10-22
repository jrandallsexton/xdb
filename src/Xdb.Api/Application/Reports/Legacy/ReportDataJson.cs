using System;
using System.Data;
using System.Globalization;
using System.Text;

namespace Xdb.Api.Application.Reports.Legacy;

/// <summary>
/// Report_GetData's body, transcribed literally from the original's string builder rather than emitted structurally,
/// so its quirks come for free:
///   - envelope spacing ("page" :  " has two spaces), every number written as a string;
///   - every row is emitted, whatever the caller's page size; "total"/"endingRec" still use that size;
///   - zero rows produce invalid JSON ending "rows" : ]} (the trailing-comma removal eats the '[');
///   - values are inserted into a per-row template with sequential replace-all on "{column}{index}¾" placeholders,
///     so a value containing another column's placeholder is itself substituted, exactly as before;
///   - only backslash, CR, LF and quote are handled; column names are not escaped.
/// A page size of 0 throws (division by zero), which the original reported as the generic fault.
/// </summary>
public static class ReportDataJson
{
    private const char Placeholder = '¾';

    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    public static string Build(DataTable table, int page, int rows)
    {
        var startRec = unchecked((page - 1) * rows);
        var endRec = unchecked(startRec + rows);
        var records = table.Rows.Count;
        var columns = table.Columns;

        var header = new StringBuilder();
        for (var i = 0; i < columns.Count; i++)
        {
            var caption = columns[i].Caption;
            header.Append('"').Append(caption).Append("\" : \"").Append(caption).Append(Text(i)).Append(Placeholder).Append('"');
            if (i + 1 < columns.Count)
                header.Append(',');
        }
        var template = header.ToString();

        var sb = new StringBuilder("{");
        sb.Append("\"total\" : \"").Append(Text(records / rows + (records % rows == 0 ? 0 : 1))).Append("\",");
        sb.Append("\"page\" :  \"").Append(Text(page)).Append("\",");
        sb.Append("\"startingRec\" :  \"").Append(Text(unchecked(startRec + 1))).Append("\",");
        sb.Append("\"endingRec\" :  \"").Append(Text(endRec < records ? endRec : records)).Append("\",");
        sb.Append("\"records\" :  \"").Append(Text(records)).Append("\",");
        sb.Append("\"rows\" : [");

        foreach (DataRow row in table.Rows)
        {
            var t = template;
            sb.Append('{');
            for (var j = 0; j < columns.Count; j++)
            {
                var value = Cell(row[j])
                    .Replace("\\", "\\\\")
                    .Replace("\r", string.Empty)
                    .Replace("\"", "\\\"")
                    .Replace("\n", string.Empty);
                t = t.Replace(columns[j].ColumnName + Text(j) + Placeholder, value);
            }
            sb.Append(t).Append("},");
        }

        sb.Remove(sb.Length - 1, 1);
        sb.Append(']').Append('}');
        return sb.ToString();
    }

    /// <summary>The original's object.ToString() under en-US, with patterns fixed (not taken from ICU culture data).</summary>
    public static string Cell(object value) => value switch
    {
        DBNull => string.Empty,
        string s => s,
        Guid g => g.ToString("D"),
        DateTime d => d.ToString("M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture),
        bool b => b ? "True" : "False",
        IFormattable f => f.ToString(null, EnUs),
        _ => value.ToString() ?? string.Empty
    };

    private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);
}
