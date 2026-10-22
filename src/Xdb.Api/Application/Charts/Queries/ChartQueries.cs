using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Charts.Legacy;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Charts.Queries;

/// <summary>The original's chart types (EChartType), keyed by their numbers.</summary>
public static class ChartTypes
{
    public static readonly IReadOnlyList<KeyValuePair<int, string>> All =
    [
        new(0, "Area"), new(1, "AreaStacked"), new(2, "Bar"), new(3, "BarStacked"), new(4, "Column"),
        new(5, "ColumnStacked"), new(6, "Donut"), new(7, "Line"), new(8, "Pie"), new(9, "ScatterPlot")
    ];
}

public sealed record CreateChartQuery(Guid UserId);

public interface ICreateChartQueryHandler
{
    Task<Result<ChartContract>> ExecuteAsync(CreateChartQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Chart_Create: a new, unsaved chart (new id, created now by the user); nothing is written.</summary>
public sealed class CreateChartQueryHandler(IDateTimeProvider dateTimeProvider) : ICreateChartQueryHandler
{
    public Task<Result<ChartContract>> ExecuteAsync(CreateChartQuery query, CancellationToken cancellationToken = default) =>
        Task.FromResult<Result<ChartContract>>(new Success<ChartContract>(new ChartContract
        {
            Id = Guid.NewGuid(),
            Created = dateTimeProvider.UtcNow().ToLocalTime(),
            CreatedBy = query.UserId,
            IsNew = true
        }));
}

public sealed record GetDashboardsQuery(string? UserId);

public interface IGetDashboardsQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetDashboardsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Dashboards_Get: the member's live dashboards by caption.</summary>
public sealed class GetDashboardsQueryHandler(IDashboardProcedures dashboards) : IGetDashboardsQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetDashboardsQuery query, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(query.UserId, out var memberId))
            throw new InvalidOperationException("Invalid user id.");
        return new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await dashboards.GetDashboardsAsync(memberId, cancellationToken));
    }
}

public sealed record GetUnmappedQuery;

public interface IGetUnmappedQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetUnmappedQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Mappings_GetUnmapped: mappings without a value, by key.</summary>
public sealed class GetUnmappedQueryHandler(IDashboardProcedures dashboards) : IGetUnmappedQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetUnmappedQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await dashboards.GetUnmappedAsync(cancellationToken));
}

public sealed record GetDashboardChartQuery(string? ChartId);

public interface IGetDashboardChartQueryHandler
{
    Task<Result<BarChartClientContract>> ExecuteAsync(GetDashboardChartQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Chart_GetForDashboard: the stored chart run with its stored grouping query. An unknown chart returns the client's
/// untouched constructor state (a new random id); a repeated display key fails, as Dictionary.Add did.
/// </summary>
public sealed class GetDashboardChartQueryHandler(IChartProcedures charts, IDateTimeProvider dateTimeProvider) : IGetDashboardChartQueryHandler
{
    public async Task<Result<BarChartClientContract>> ExecuteAsync(GetDashboardChartQuery query, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(query.ChartId, out var chartId))
            throw new InvalidOperationException("Invalid ChartId");
        var client = new BarChartClientContract { Id = Guid.NewGuid(), Created = dateTimeProvider.UtcNow().ToLocalTime() };
        if (await charts.GetChartAsync(chartId, cancellationToken) is not { } chart)
            return new Success<BarChartClientContract>(client);

        var data = new Dictionary<string, string>();
        var dictionary = new Dictionary<string, string>();
        foreach (var row in await charts.LoadDataAsync(chart.GroupSql, cancellationToken))
        {
            dictionary.Add(row.Key, row.Id);
            data.Add(row.Key, row.Value);
        }
        client.Id = chartId;
        client.Name = chart.Name;
        client.Title = chart.Title;
        client.Subtitle = chart.Subtitle;
        client.LegendText = chart.LegendText;
        client.LegendVisible = chart.LegendVisible;
        client.Data = data;
        client.DataDictionary = dictionary;
        client.RecordCount = data.Count;
        return new Success<BarChartClientContract>(client);
    }
}

public sealed record GetChartSupportDataQuery(Guid ChartId, string? Value);

public interface IGetChartSupportDataQueryHandler
{
    Task<Result<Dictionary<string, string>>> ExecuteAsync(GetChartSupportDataQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Chart_GetSupportData: the assets behind one chart category (the stored detail query with @Val = x).</summary>
public sealed class GetChartSupportDataQueryHandler(IChartProcedures charts) : IGetChartSupportDataQueryHandler
{
    public async Task<Result<Dictionary<string, string>>> ExecuteAsync(GetChartSupportDataQuery query, CancellationToken cancellationToken = default)
    {
        var sql = await charts.GetFullSqlAsync(query.ChartId, cancellationToken);
        var result = new Dictionary<string, string>();
        foreach (var (key, value) in await charts.GetSupportDataAsync(sql, query.Value, cancellationToken))
            result.Add(key, value);
        return new Success<Dictionary<string, string>>(result);
    }
}
