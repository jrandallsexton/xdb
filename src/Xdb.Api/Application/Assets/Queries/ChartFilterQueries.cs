using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects;
using Xdb.Api.Application.GeneratedObjects.Views;
using Xdb.Api.Application.Metadata;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Assets.Queries;

public sealed record GetChartFilterQuery(string? ChartId, string? AssetTypeId, int RequestType, string? PropertyId, string? FilterExpression,
    IReadOnlyList<FilterInput>? Filters);

public interface IGetChartFilterQueryHandler
{
    Task<Result<AssetFilterClientContract>> ExecuteAsync(GetChartFilterQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// AssetFilterClient_Get (ChartLayer): the chart type's matching assets go into session temp tables; then, for each
/// non-free-entry property related directly to the type, its values outside (disabled) and inside (enabled) the
/// selection. The result sets come back in SQL name order but are labelled by the culture-sorted list of all
/// properties (inherited included), so values can land under the wrong property, as they did in the original.
/// </summary>
public sealed class GetChartFilterQueryHandler(
    ViewInputLoader loader,
    IFilterProcedures filters,
    IMetadataProcedures metadata,
    IGeneratedObjectProcedures generated,
    MetadataRules rules) : IGetChartFilterQueryHandler
{
    public async Task<Result<AssetFilterClientContract>> ExecuteAsync(GetChartFilterQuery query, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(query.ChartId, out var chartId))
            throw new InvalidOperationException("Invalid ChartId");
        var (assetTypeId, requestType, chartFilters) = await CreateAsync(query, cancellationToken);

        // Base query: definitions always, instances too for an Instance chart.
        var assetTypeName = await metadata.GetAssetTypeNameAsync(assetTypeId, cancellationToken);
        var tempName = chartId.ToString("N");
        var definitions = await loader.LoadAsync(assetTypeId, isInstance: false, [], chartFilters, query.FilterExpression, cancellationToken);
        var baseQuery = ChartSql.BuildInto(definitions, assetTypeName, tempName + "_D");
        if (requestType == (int)AssetRequestType.Instance)
        {
            var instances = await loader.LoadAsync(assetTypeId, isInstance: true, [], chartFilters, query.FilterExpression, cancellationToken);
            baseQuery += ChartSql.NewLine + ChartSql.BuildInto(instances, assetTypeName, tempName + "_I");
        }

        // Properties: the type's and its ancestors', sorted by display name (a repeated name fails).
        var typeIds = new List<Guid> { assetTypeId };
        var parent = await metadata.GetParentAssetTypeIdAsync(assetTypeId, cancellationToken);
        while (parent is { } parentId)
        {
            if (parentId != assetTypeId)
                typeIds.Add(parentId);
            parent = await metadata.GetParentAssetTypeIdAsync(parentId, cancellationToken);
        }
        var properties = await filters.GetNonFreeEntryPropertiesAsync(typeIds, requestType, cancellationToken);
        var sorted = new SortedDictionary<string, Guid>(LegacyCollation.EnUs);
        foreach (var (id, name) in properties)
            sorted.Add(name, id);
        var byIndex = sorted.Values.ToList();

        var relations = new Dictionary<Guid, PropertyRelationRow>();
        foreach (var relation in await generated.GetRelationsAsync(assetTypeId, properties.Select(p => p.Key).ToList(), cancellationToken))
            relations.Add(relation.PropertyId, relation);

        var sql = ChartSql.BuildValues(baseQuery, assetTypeId, requestType, tempName, properties, relations);
        var values = new Dictionary<Guid, List<AssetFilterItemContract>>();
        var sets = await filters.GetFilterItemsAsync(sql, cancellationToken);
        for (var i = 0; i < sets.Count; i++)
        {
            var propertyId = i < byIndex.Count ? byIndex[i] : throw new KeyNotFoundException($"The given key '{i}' was not present in the dictionary.");
            values.Add(propertyId, [.. sets[i].Select(row => new AssetFilterItemContract { Key = row.Key, Value = row.Value, Enabled = row.Enabled })]);
        }
        return new Success<AssetFilterClientContract>(new AssetFilterClientContract { Properties = sorted, Values = values });
    }

    /// <summary>ChartLayer.Create: the parts that can fail (ids, the chart property, the filters); the chart itself is unused.</summary>
    private async Task<(Guid AssetTypeId, int RequestType, List<ViewFilter> Filters)> CreateAsync(GetChartFilterQuery query, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(query.AssetTypeId, out var assetTypeId))
            throw new InvalidOperationException("Invalid AssetTypeId");
        var requestType = query.RequestType;
        if (!Guid.TryParse(query.PropertyId, out var propertyId))
            throw new InvalidOperationException("Invalid PropertyId");

        // HasAssets (and, when false, the child list) were read and discarded; they cannot fail.
        if (!await rules.HasAssetsAsync(assetTypeId, (AssetRequestType)requestType, searchChildAssetTypes: false, cancellationToken))
            _ = await rules.ChildrenAsync(assetTypeId, includeAllDescendants: true, cancellationToken);
        var row = await metadata.GetPropertyAsync(propertyId, cancellationToken);
        var property = row is null ? null : MetadataLoader.Property(row);
        if (requestType == (int)AssetRequestType.Instance && property is null)
            throw new NullReferenceException("Chart property not found.");

        var result = new List<ViewFilter>();
        foreach (var filter in query.Filters ?? throw new NullReferenceException("filters is null."))
        {
            if (!Guid.TryParse((filter ?? throw new NullReferenceException("filter is null.")).PropertyId, out var filterPropertyId))
                throw new InvalidOperationException("Invalid PropertyId in filter");
            result.Add(new ViewFilter(filterPropertyId, filter.OperatorId, ChartSql.OperatorSql(filter.OperatorId), filter.Value!));
        }
        return (assetTypeId, requestType, result);
    }
}
