using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Xdb.Api.Application.Assets;
using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Assets.Queries;
using Xdb.Api.Application.Charts.Legacy;
using Xdb.Api.Application.Common.Identity;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects.Views;
using Xdb.Api.Application.Metadata;
using Xdb.Api.Application.Metadata.Queries.Lookups;
using Xdb.Api.Application.Views.Queries;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Charts.Commands;

public sealed record RenderChartCommand(AssetSearchClient? Options);

public interface IRenderChartCommandHandler
{
    Task<Result<BarChartClientContract>> ExecuteAsync(RenderChartCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Chart_GetViaFiltering (ChartLayer). When the chart does not exist, or the request marks it dirty (the UI always
/// does), the chart is rebuilt from the request (title from the type's labels, grouping and detail SQL, a filter-string
/// subtitle) and saved; then its data is loaded with the grouping query. An existing clean chart is only read (the
/// response then carries a new random id, as the original's did).
/// </summary>
/// <remarks>
/// DEVIATION (recorded): the original queued the save on a pool thread, where a failure (a null filter value) could
/// take down the worker process. The port saves before loading the data and logs a failure instead; the statements
/// before the failure stay committed, as in the original, and the response is the same.
/// </remarks>
public sealed class RenderChartCommandHandler(
    IChartProcedures charts,
    IMetadataProcedures metadata,
    MetadataRules rules,
    ViewInputLoader loader,
    IGetSearchFilterDisplayQueryHandler filterString,
    CurrentMember currentMember,
    IDateTimeProvider dateTimeProvider,
    IOptions<LegacyEnvironmentOptions> environment,
    ILogger<RenderChartCommandHandler> logger) : IRenderChartCommandHandler
{
    public async Task<Result<BarChartClientContract>> ExecuteAsync(RenderChartCommand command, CancellationToken cancellationToken = default)
    {
        // Helpers.GetCurrentUserId() is evaluated as the service-method argument, before the options are read.
        var userId = await currentMember.GetIdAsync(cancellationToken);
        var options = command.Options ?? throw new NullReferenceException("chartOptions is null.");
        var client = new BarChartClientContract { Id = Guid.NewGuid(), Created = dateTimeProvider.UtcNow().ToLocalTime() };
        if (!Guid.TryParse(options.Id, out var chartId))
            throw new InvalidOperationException("Invalid ChartId");

        var stored = await charts.GetChartAsync(chartId, cancellationToken);
        string? groupSql = stored?.GroupSql;
        var subtitle = stored?.Subtitle;
        if (stored is null || options.IsDirty)
        {
            var chart = await CreateAsync(options, chartId, userId, cancellationToken);
            client.Id = chartId;
            client.Name = chart.Title;
            client.Title = chart.Title;

            groupSql = ChartSql.BuildGroupCount(chart.GroupInput, chart.AssetTypeName, chart.PropertyId, chart.PropertyName);
            var fullSql = ChartSql.BuildFull(chart.FullInput, chart.AssetTypeName, chart.PropertyId);
            subtitle = chart.Subtitle;
            if (chart.Filters.Count > 0)
            {
                var display = await filterString.ExecuteAsync(new GetSearchFilterDisplayQuery(options.FilterExpression,
                    chart.Filters.Select(f => new SearchFilter(f.PropertyId, f.OperatorId, f.Value, ChartSql.OperatorSql(f.OperatorId))).ToList()), cancellationToken);
                subtitle = display is Success<string> text ? text.Value : throw new InvalidOperationException("Filter string failed.");
            }

            try
            {
                await charts.SaveChartAsync(new ChartSaveRow(chartId, chart.Title, chart.Title, subtitle, chart.SeriesName, chart.AssetTypeId, chart.IsInstance,
                    options.FilterExpression, chart.Created, userId, groupSql, fullSql, Guid.NewGuid(), chart.PropertyId, chart.PropertyName,
                    chart.Filters.Select(f => new ChartFilterSaveRow(f.PropertyId, f.OperatorId, f.Value, f.Order)).ToList()), cancellationToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogError(e, "Chart {ChartId} save failed (the original's background save would have failed too).", chartId);
            }
        }

        var data = new Dictionary<string, string>();
        var dictionary = new Dictionary<string, string>();
        foreach (var row in await charts.LoadDataAsync(groupSql, cancellationToken))
        {
            dictionary.Add(row.Key, row.Id);
            data.Add(row.Key, row.Value);
        }
        client.Data = data;
        client.DataDictionary = dictionary;
        client.RecordCount = data.Count;
        client.Subtitle = subtitle;
        return new Success<BarChartClientContract>(client);
    }

    /// <summary>ChartLayer.Create plus the inputs GetSqlString would load for the two queries.</summary>
    private async Task<ChartDefinition> CreateAsync(AssetSearchClient options, Guid chartId, Guid userId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(options.AssetTypeId, out var assetTypeId))
            throw new InvalidOperationException("Invalid AssetTypeId");
        var requestType = options.AssetRequestType;
        var isInstance = requestType == (int)AssetRequestType.Instance;
        if (!Guid.TryParse(options.PropertyId, out var propertyId))
            throw new InvalidOperationException("Invalid PropertyId");

        var plural = await metadata.GetAssetTypePluralizationAsync(assetTypeId, cancellationToken);
        var labelPlural = await metadata.GetReportingLabelPluralAsync(assetTypeId, isInstance, cancellationToken);
        if (!await rules.HasAssetsAsync(assetTypeId, (AssetRequestType)requestType, searchChildAssetTypes: false, cancellationToken))
            _ = await rules.ChildrenAsync(assetTypeId, includeAllDescendants: true, cancellationToken);
        var property = await metadata.GetPropertyAsync(propertyId, cancellationToken) is { } row ? MetadataLoader.Property(row) : null;
        var created = dateTimeProvider.UtcNow().ToLocalTime();
        var propertyName = !isInstance || (property ?? throw new NullReferenceException("Chart property not found.")).SystemType != (int)SystemType.InstanceOf
            ? await metadata.GetPropertyDisplayValueAsync(propertyId, cancellationToken)
            : await metadata.GetReportingLabelAsync(assetTypeId, forInstances: false, cancellationToken);

        var filters = new List<ChartFilter>();
        foreach (var filter in options.Filters ?? throw new NullReferenceException("filters is null."))
        {
            if (!Guid.TryParse((filter ?? throw new NullReferenceException("filter is null.")).PropertyId, out var filterPropertyId))
                throw new InvalidOperationException("Invalid PropertyId in filter");
            _ = ChartSql.OperatorSql(filter.OperatorId);
            filters.Add(new ChartFilter(filterPropertyId, filter.OperatorId, filter.Value, filter.Order));
        }

        var title = options.MaxRecords > 0 ? $"{plural} {labelPlural} by Top {options.MaxRecords} {propertyName}s" : $"{plural} {labelPlural} by {propertyName}";
        var viewFilters = filters.Select(f => new ViewFilter(f.PropertyId, f.OperatorId, ChartSql.OperatorSql(f.OperatorId), f.Value!)).ToList();

        // GetSqlString: the grouping count loads the filters and the chart property; the detail query the filters only.
        var groupInput = await loader.LoadAsync(assetTypeId, isInstance, [new ViewColumn(propertyId, null, 0, propertyName, null)], viewFilters, options.FilterExpression, cancellationToken);
        var fullInput = await loader.LoadAsync(assetTypeId, isInstance, [], viewFilters, options.FilterExpression, cancellationToken);
        var assetTypeName = await metadata.GetAssetTypeNameAsync(assetTypeId, cancellationToken);

        return new ChartDefinition(assetTypeId, isInstance, propertyId, propertyName, title, plural, environment.Value.ChartSubtitle, created, filters,
            groupInput, fullInput, assetTypeName);
    }

    private sealed record ChartFilter(Guid PropertyId, int OperatorId, string? Value, int Order);

    private sealed record ChartDefinition(
        Guid AssetTypeId, bool IsInstance, Guid PropertyId, string PropertyName, string Title, string SeriesName, string Subtitle, DateTime Created,
        IReadOnlyList<ChartFilter> Filters, ViewInput GroupInput, ViewInput FullInput, string AssetTypeName);
}
