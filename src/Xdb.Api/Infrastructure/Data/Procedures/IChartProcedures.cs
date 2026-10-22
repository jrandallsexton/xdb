using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xdb.Api.Infrastructure.Data.Procedures;

/// <summary>The parts of a stored chart (spr_Chart_Get) that reach any response.</summary>
public sealed record ChartRow(string Name, string? Title, string? Subtitle, string? LegendText, bool LegendVisible, string? GroupSql);

/// <summary>What ChartDal.Save writes for a chart built by Chart_GetViaFiltering (one grouping property).</summary>
public sealed record ChartSaveRow(
    Guid Id, string Name, string? Title, string? Subtitle, string SeriesName, Guid AssetTypeId, bool IsInstance, string? FilterExpression,
    DateTime Created, Guid CreatedBy, string? GroupSql, string? FullSql,
    Guid PropertyRowId, Guid PropertyId, string? PropertyLabel, IReadOnlyList<ChartFilterSaveRow> Filters);

public sealed record ChartFilterSaveRow(Guid PropertyId, int OperatorId, string? Value, int Order);

/// <summary>A chart data row: raw id, display key and count, as text (NULL as "").</summary>
public sealed record ChartDataRow(string Id, string Key, string Value);

/// <summary>The original's chart reads (ChartDal, ChartDataDal) with its SQL text and reader semantics.</summary>
public interface IChartProcedures
{
    /// <summary>
    /// ChartDal.Get: the chart, its properties and filters (deleted charts included); null when absent. Reads every
    /// column as the original did, so a NULL property id or filter operator fails.
    /// </summary>
    Task<ChartRow?> GetChartAsync(Guid chartId, CancellationToken cancellationToken);
    /// <summary>
    /// ChartDal.Save, statement by statement without a transaction: spr_Chart_Save (insert or update), the chart's
    /// properties replaced by the one grouping property, its filters replaced (each with a new id). A failure leaves
    /// the statements before it committed.
    /// </summary>
    Task SaveChartAsync(ChartSaveRow chart, CancellationToken cancellationToken);
    /// <summary>ChartDataDal.LoadData: the stored grouping query's rows (guarded); a null or empty query fails.</summary>
    Task<IReadOnlyList<ChartDataRow>> LoadDataAsync(string? sql, CancellationToken cancellationToken);
    /// <summary>The chart's stored detail query, trimmed; "" when absent or NULL.</summary>
    Task<string> GetFullSqlAsync(Guid chartId, CancellationToken cancellationToken);
    /// <summary>GetDictionaryStringString(sql, @Val): (text, text) pairs, first key wins, guarded; a null value is "not supplied".</summary>
    Task<IReadOnlyList<KeyValuePair<string, string>>> GetSupportDataAsync(string sql, string? value, CancellationToken cancellationToken);
}
