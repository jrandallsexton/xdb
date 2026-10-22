using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Dapper;

namespace Xdb.Api.Infrastructure.Data.Procedures;

public sealed class ChartProcedures(XdbDataContext db) : LegacyGatewayBase(db), IChartProcedures
{
    private const int CustomFieldNotApplicable = 0;

    public async Task<ChartRow?> GetChartAsync(Guid chartId, CancellationToken cancellationToken)
    {
        ChartRow chart;
        var customProperties = new List<Guid>();
        await using (var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Procedure("spr_Chart_Get", new { Id = chartId }, cancellationToken)))
        {
            if (!reader.HasRows)
                return null;
            await reader.ReadAsync(cancellationToken);
            string? Text(int i) => reader.IsDBNull(i) ? null : reader.GetString(i);
            var name = reader.GetString(0);
            var legendVisible = !reader.IsDBNull(7) && (bool)reader.GetValue(7);
            _ = reader.GetDateTime(14);
            _ = reader.GetGuid(15);
            chart = new ChartRow(name, Text(3), Text(4), Text(6), legendVisible, Text(20));

            // spr_ChartProperties_Get: read as the original mapped it (a NULL property id fails).
            await reader.NextResultAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetGuid(0);
                _ = reader.GetGuid(1);
                _ = reader.GetInt32(3);
                _ = (bool)reader.GetValue(4);
                if (!reader.IsDBNull(5) && reader.GetInt32(5) != CustomFieldNotApplicable)
                    customProperties.Add(id);
            }
        }

        // Custom chart fields read a table that no longer exists (SQL 208), as in the original; no stored chart has one.
        foreach (var id in customProperties)
            await (await OpenAsync(cancellationToken)).QueryAsync<Guid>(Inline(
                $"SELECT [PropertyId] FROM [ChartsPropertiesProperties] WHERE [ChartsPropertiesId] = '{id:D}' ORDER BY [Order]", null, cancellationToken));

        await using (var filters = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(Lines(
            "SELECT CF.[Id], CF.[PropertyId], IsNull(P.[DisplayValue], P.[Name]) AS [Property],",
            "CF.[OperatorId], O.[Operator], CF.[Value], CF.[Order]",
            "FROM [ChartsFilters] CF",
            "INNER JOIN [Properties] P ON P.[Id] = CF.[PropertyId]",
            "INNER JOIN [lkOperators] O ON O.[Id] = CF.[OperatorId]",
            "WHERE CF.[ChartId] = @ChartId",
            "ORDER BY CF.[Order]"), new { ChartId = chartId }, cancellationToken)))
        {
            while (await filters.ReadAsync(cancellationToken))
            {
                _ = filters.GetGuid(0);
                _ = filters.GetGuid(1);
                _ = filters.GetString(2);
                _ = filters.GetInt32(3);
                _ = filters.GetString(4);
                _ = filters.GetString(5);
                _ = filters.GetInt32(6);
            }
        }
        return chart;
    }

    public async Task SaveChartAsync(ChartSaveRow chart, CancellationToken cancellationToken)
    {
        var connection = await OpenAsync(cancellationToken);
        // Null values are the original's "not supplied" parameters: the procedure defaults (NULL) apply.
        await connection.ExecuteAsync(Procedure("spr_Chart_Save", new
        {
            chart.Id,
            chart.Name,
            PaletteName = (string?)null,
            AppearanceName = (string?)null,
            chart.Title,
            chart.Subtitle,
            ChartTitle = (string?)null,
            LegendText = (string?)null,
            LegendVisible = false,
            chart.SeriesName,
            ViewType = (string?)null,
            chart.AssetTypeId,
            chart.IsInstance,
            IsPublic = false,
            RecordCount = 0,
            FilterExpression = string.IsNullOrEmpty(chart.FilterExpression) ? null : chart.FilterExpression,
            chart.Created,
            chart.CreatedBy,
            Deleted = (DateTime?)null,
            DeletedBy = (Guid?)null,
            GroupSql = string.IsNullOrEmpty(chart.GroupSql) ? null : chart.GroupSql,
            FullSql = string.IsNullOrEmpty(chart.FullSql) ? null : chart.FullSql
        }, cancellationToken));

        await connection.ExecuteAsync(Inline("DELETE FROM [ChartsProperties] WHERE [ChartId] = @ChartId" + NewLine, new { ChartId = chart.Id }, cancellationToken));
        await connection.ExecuteAsync(Procedure("spr_ChartProperty_Insert", new
        {
            Id = chart.PropertyRowId,
            ChartId = chart.Id,
            PropertyId = chart.PropertyId == Guid.Empty ? (Guid?)null : chart.PropertyId,
            SubPropertyId = (Guid?)null,
            Order = 0,
            IsGrouping = true,
            CustomReportFieldTypeId = 0,
            TimeElapsedTypeId = 1,
            TimeElapsedFormatId = 0,
            TimeElapsedSpecificDate = (DateTime?)null,
            Label = string.IsNullOrEmpty(chart.PropertyLabel) ? null : chart.PropertyLabel,
            Logic = (string?)null
        }, cancellationToken));

        await connection.ExecuteAsync(Inline("DELETE FROM [ChartsFilters] WHERE [ChartId] = @ChartId", new { ChartId = chart.Id }, cancellationToken));
        foreach (var filter in chart.Filters)
        {
            // A null value was "not supplied" for the inline statement (SQL 8178); the port fails the same statement.
            if (filter.Value is null)
                throw new InvalidOperationException("The parameterized query expects the parameter '@Value', which was not supplied.");
            await connection.ExecuteAsync(Inline(
                "INSERT INTO [ChartsFilters] ([Id], [ChartId], [PropertyId], [OperatorId], [Value], [Order])" + NewLine
                + "VALUES (@Id, @ChartId, @PropertyId, @OperatorId, @Value, @Order)" + NewLine,
                new { Id = Guid.NewGuid(), ChartId = chart.Id, filter.PropertyId, filter.OperatorId, filter.Value, filter.Order }, cancellationToken));
        }
    }

    public async Task<IReadOnlyList<ChartDataRow>> LoadDataAsync(string? sql, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(sql))
            throw new InvalidOperationException("ExecuteReader: CommandText property has not been initialized");
        var rows = new List<ChartDataRow>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(sql, null, cancellationToken));
        if (!reader.HasRows)
            return rows;
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(new ChartDataRow(LegacyText(reader.GetValue(0)), LegacyText(reader.GetValue(1)), LegacyText(reader.GetValue(2))));
        return rows;
    }

    public Task<string> GetFullSqlAsync(Guid chartId, CancellationToken cancellationToken) =>
        ScalarStringAsync("SELECT [FullSql] FROM [Charts] WHERE [Id] = @Id", new { Id = chartId }, cancellationToken);

    public async Task<IReadOnlyList<KeyValuePair<string, string>>> GetSupportDataAsync(string sql, string? value, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(sql))
            throw new InvalidOperationException("ExecuteReader: CommandText property has not been initialized");
        if (value is null)
            throw new InvalidOperationException("The parameterized query expects the parameter '@Val', which was not supplied.");
        var result = new List<KeyValuePair<string, string>>();
        var seen = new HashSet<string>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(sql, new { Val = value }, cancellationToken));
        if (!reader.HasRows)
            return result;
        while (await reader.ReadAsync(cancellationToken))
        {
            var key = LegacyText(reader.GetValue(0));
            if (seen.Add(key))
                result.Add(new KeyValuePair<string, string>(key, LegacyText(reader.GetValue(1))));
        }
        return result;
    }
}
