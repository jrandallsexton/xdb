using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects.Views;
using Xdb.Api.Application.Metadata;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Assets.Queries;

/// <summary>A filter as the client sent it (FilterClient), before the original's conversion.</summary>
public sealed record FilterInput(string? PropertyId, int OperatorId, string? Value);

/// <summary>
/// AssetLayer.Assets_GetMatchingByAssetTypeId: the gen-table assets of the type and kind (any request type but
/// Instance means definitions) whose single column is the name, filtered by the caller's expression over the
/// filters, ordered by the type's label; read with GetDictionary(sql) (first key wins, NULL and non-Guid keys folded).
/// </summary>
public sealed class FilterEngine(ViewInputLoader loader, IMetadataProcedures metadata)
{
    private const string Operator = "=";
    private const int MaxOperatorId = 6;

    public async Task<List<KeyValuePair<Guid, string>>> MatchAsync(Guid assetTypeId, int requestType, string? filterLogic, IReadOnlyList<ViewFilter> filters,
        CancellationToken cancellationToken)
    {
        var isInstance = requestType == (int)AssetRequestType.Instance;
        var columns = new[] { new ViewColumn(SystemPropertyIds.AssetName, null, 0, "Asset Name", null) };
        var input = await loader.LoadAsync(assetTypeId, isInstance, columns, filters, filterLogic, cancellationToken);
        var sql = ViewSql.BuildSelect(input with { IncludeOrderBy = true });
        return [.. await metadata.GetLegacyDictionaryAsync(sql, cancellationToken)];
    }

    /// <summary>
    /// The FilterClient → Filter loop: Guid.Parse of the property id, then the operator id checked against 0..6
    /// (both fail the call); the operator text is always "=".
    /// </summary>
    public static List<ViewFilter> Convert(IReadOnlyList<FilterInput>? filters)
    {
        var result = new List<ViewFilter>();
        foreach (var filter in filters ?? throw new NullReferenceException("filters is null."))
        {
            var propertyId = Guid.Parse(filter.PropertyId ?? throw new ArgumentNullException(nameof(filters), "propertyId is null."));
            if (filter.OperatorId is < 0 or > MaxOperatorId)
                throw new InvalidOperationException("Invalid value for EFilterOperator");
            result.Add(new ViewFilter(propertyId, filter.OperatorId, Operator, filter.Value!));
        }
        return result;
    }

    public static ViewFilter EqualTo(Guid propertyId, string? value) => new(propertyId, 0, Operator, value!);
}

public sealed record MatchAssetsQuery(string? AssetTypeId, int RequestType, string? FilterExpression, IReadOnlyList<FilterInput>? Filters);

public interface IMatchAssetsQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(MatchAssetsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Assets_GetViaFiltering: the engine with the client's type, kind, expression and filters.</summary>
public sealed class MatchAssetsQueryHandler(FilterEngine engine) : IMatchAssetsQueryHandler
{
    public async Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(MatchAssetsQuery query, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(query.AssetTypeId, out var assetTypeId))
            throw new InvalidOperationException("Invalid Asset Type Id");
        var filters = FilterEngine.Convert(query.Filters);
        return new Success<List<KeyValuePair<Guid, string>>>(await engine.MatchAsync(assetTypeId, query.RequestType, query.FilterExpression, filters, cancellationToken));
    }
}

public sealed record MatchAssetsByValueQuery(Guid AssetTypeId, int RequestType, Guid PropertyId, string? PropertyValue);

public interface IMatchAssetsByValueQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(MatchAssetsByValueQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Assets_GetMatchingByAssetTypeId / ..ByPropertyValue: one EqualTo filter with the expression "({0})".</summary>
public sealed class MatchAssetsByValueQueryHandler(FilterEngine engine) : IMatchAssetsByValueQueryHandler
{
    public async Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(MatchAssetsByValueQuery query, CancellationToken cancellationToken = default) =>
        new Success<List<KeyValuePair<Guid, string>>>(await engine.MatchAsync(query.AssetTypeId, query.RequestType, "({0})",
            [FilterEngine.EqualTo(query.PropertyId, query.PropertyValue)], cancellationToken));
}

public sealed record MatchAssetsBySavedFilterQuery(string? FilterId);

public interface IMatchAssetsBySavedFilterQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(MatchAssetsBySavedFilterQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Assets_GetByViewFilter: a saved quick filter (anyone's) run over its view's asset type and kind.</summary>
public sealed class MatchAssetsBySavedFilterQueryHandler(FilterEngine engine, IFilterProcedures procedures) : IMatchAssetsBySavedFilterQueryHandler
{
    public async Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(MatchAssetsBySavedFilterQuery query, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(query.FilterId, out var filterId))
            throw new InvalidOperationException("Invalid filter id");
        var saved = await procedures.GetMemberFilterAsync(filterId, cancellationToken) ?? throw new InvalidOperationException("QuickFilter not found.");
        var filters = FilterEngine.Convert(saved.Filters.Select(f => new FilterInput(f.PropertyId.ToString("D"), f.OperatorId, f.Value)).ToList());
        return new Success<List<KeyValuePair<Guid, string>>>(await engine.MatchAsync(saved.AssetTypeId, saved.IsInstance ? 1 : 0, saved.FilterExpression,
            filters, cancellationToken));
    }
}

public sealed record GetPotentialValuesForAssetsQuery(IReadOnlyList<Guid>? AssetIds, Guid PropertyId, Guid AssetTypeId, int RequestType);

public interface IGetPotentialValuesForAssetsQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetPotentialValuesForAssetsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// PropertyValueLayer.PropertyValues_GetPotentialByAssetIds: the values the property takes on the assets, decoded for
/// asset, user and picklist properties (and the system InstanceOf and Asset Type); every other kind returns [] (the
/// original built its dictionary and then returned a new empty one).
/// </summary>
public sealed class GetPotentialValuesForAssetsQueryHandler(
    IFilterProcedures procedures,
    IMetadataProcedures metadata,
    IAssetProcedures assets,
    IGeneratedObjectProcedures generated) : IGetPotentialValuesForAssetsQueryHandler
{
    public async Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetPotentialValuesForAssetsQuery query, CancellationToken cancellationToken = default) =>
        new Success<List<KeyValuePair<Guid, string>>>(await PotentialAsync(query, cancellationToken));

    private async Task<List<KeyValuePair<Guid, string>>> PotentialAsync(GetPotentialValuesForAssetsQuery query, CancellationToken cancellationToken)
    {
        var assetIds = query.AssetIds;
        if (assetIds is null || assetIds.Count == 0)
            return [];
        var row = await metadata.GetPropertyAsync(query.PropertyId, cancellationToken) ?? throw new NullReferenceException("Property not found.");
        var property = MetadataLoader.Property(row);

        IReadOnlyList<string> values;
        if (!property.IsSystem)
        {
            var relation = await RelationAsync(query.AssetTypeId, query.PropertyId, cancellationToken);
            if (relation is null)
                return [];
            values = !relation.IsInstance && query.RequestType == (int)AssetRequestType.Instance
                ? await procedures.GetPotentialValuesAsync(await procedures.GetInstanceOfIdsAsync(assetIds, cancellationToken), query.PropertyId, cancellationToken)
                : await procedures.GetPotentialValuesAsync(assetIds, query.PropertyId, cancellationToken);
        }
        else
        {
            values = await procedures.GetPotentialSystemValuesAsync(assetIds, property.SystemType, cancellationToken);
        }
        if (values.Count == 0)
            return [];

        switch (property.DataType)
        {
            case (int)LegacyDataType.Asset:
                if (property.IsSystem)
                {
                    if (property.SystemType != (int)SystemType.InstanceOf)
                        return [];
                    var definitions = new List<Guid>();
                    foreach (var assetId in assetIds)
                    {
                        if (await assets.GetInstanceOfIdAsync(assetId, cancellationToken) is { } definition && !definitions.Contains(definition))
                            definitions.Add(definition);
                    }
                    return [.. await procedures.GetAssetsAsync(definitions, cancellationToken)];
                }
                return [.. await procedures.GetAssetsAsync(ToGuids(values), cancellationToken)];
            case (int)LegacyDataType.PickList:
                return [.. await procedures.GetMatchingPickListValuesAsync(ToGuids(values), property.PickListId!.Value, cancellationToken)];
            case (int)LegacyDataType.String:
                if (property.IsSystem && property.SystemType == (int)SystemType.AssetType)
                {
                    var types = new List<KeyValuePair<Guid, string>>();
                    foreach (var value in values)
                    {
                        var id = new Guid(value);
                        if (!types.Exists(t => t.Key == id))
                            types.Add(new KeyValuePair<Guid, string>(id, await metadata.GetAssetTypeNameAsync(id, cancellationToken)));
                    }
                    return types;
                }
                return [];
            case (int)LegacyDataType.User:
                return [.. await procedures.GetAssetsAsync(ToGuids(values), cancellationToken)];
            default:
                return [];
        }
    }

    /// <summary>The type's live relation for the property, else the first ancestor's.</summary>
    private async Task<PropertyRelationRow?> RelationAsync(Guid assetTypeId, Guid propertyId, CancellationToken cancellationToken)
    {
        var relation = await generated.GetRelationAsync(assetTypeId, propertyId, cancellationToken);
        var type = assetTypeId;
        while (relation is null)
        {
            if (await metadata.GetParentAssetTypeIdAsync(type, cancellationToken) is not { } parent || parent == Guid.Empty)
                return null;
            type = parent;
            relation = await generated.GetRelationAsync(type, propertyId, cancellationToken);
        }
        return relation;
    }

    /// <summary>Helpers.ListOfStringsToGuid: the parseable values, without repeats, in order.</summary>
    private static List<Guid> ToGuids(IReadOnlyList<string> values)
    {
        var result = new List<Guid>();
        foreach (var value in values)
        {
            if (Guid.TryParse(value, out var id) && !result.Contains(id))
                result.Add(id);
        }
        return result;
    }
}

public sealed record GetPotentialValuesViaFilteringQuery(string? AssetTypeId, string? PropertyId, int RequestType, string? FilterExpression,
    IReadOnlyList<FilterInput>? Filters);

public interface IGetPotentialValuesViaFilteringQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetPotentialValuesViaFilteringQuery query, CancellationToken cancellationToken = default);
}

/// <summary>PropertyValues_GetViaFiltering: the property's potential values over the assets the filters match.</summary>
public sealed class GetPotentialValuesViaFilteringQueryHandler(FilterEngine engine, IGetPotentialValuesForAssetsQueryHandler potential)
    : IGetPotentialValuesViaFilteringQueryHandler
{
    public async Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetPotentialValuesViaFilteringQuery query, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(query.AssetTypeId, out var assetTypeId))
            throw new InvalidOperationException("Invalid Asset Type Id");
        if (!Guid.TryParse(query.PropertyId, out var propertyId))
            throw new InvalidOperationException("Invalid Property Id");
        var filters = FilterEngine.Convert(query.Filters);
        var matches = await engine.MatchAsync(assetTypeId, query.RequestType, query.FilterExpression, filters, cancellationToken);
        return await potential.ExecuteAsync(new GetPotentialValuesForAssetsQuery(matches.Select(m => m.Key).ToList(), propertyId, assetTypeId, query.RequestType),
            cancellationToken);
    }
}

public sealed record GetMatchingAssetsQuery(IReadOnlyList<Guid>? AssetIds, Guid PropertyId, string? PropertyValue, bool IncludeDescriptions);

public interface IGetMatchingAssetsQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetMatchingAssetsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Assets_GetMatching: which of the assets have the property value. The first asset decides the type and kind;
/// system properties match by LIKE with descriptions (InstanceOf ignores the list); others need a direct relation.
/// </summary>
public sealed class GetMatchingAssetsQueryHandler(IFilterProcedures procedures, IMetadataProcedures metadata, IAssetProcedures assets, IGeneratedObjectProcedures generated)
    : IGetMatchingAssetsQueryHandler
{
    public async Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetMatchingAssetsQuery query, CancellationToken cancellationToken = default) =>
        new Success<List<KeyValuePair<Guid, string>>>([.. await MatchAsync(query, cancellationToken)]);

    private async Task<IReadOnlyList<KeyValuePair<Guid, string>>> MatchAsync(GetMatchingAssetsQuery query, CancellationToken cancellationToken)
    {
        var assetIds = query.AssetIds;
        if (assetIds is null || assetIds.Count == 0)
            return [];
        if (await metadata.GetPropertyAsync(query.PropertyId, cancellationToken) is not { } row)
            return [];
        var property = MetadataLoader.Property(row);
        var asset = await assets.GetAssetAsync(assetIds[0], cancellationToken);
        var value = query.PropertyValue;

        if (property.IsSystem)
        {
            var isInstance = (asset ?? throw new NullReferenceException("Asset not found.")).IsInstance ?? false;
            switch (property.SystemType)
            {
                case (int)SystemType.AssetType:
                    return await procedures.MatchByAssetTypeAsync(assetIds, new Guid(value!), includeDescriptions: true, cancellationToken);
                case (int)SystemType.InstanceOf:
                    return await procedures.GetInstancesOfAsync(new Guid(value!), cancellationToken);
                case (int)SystemType.CreatedBy:
                case (int)SystemType.DeletedBy:
                case (int)SystemType.LastModifiedBy:
                    return await procedures.MatchBySystemUserAsync(assetIds, property.SystemType, value, cancellationToken);
                default:
                    return await procedures.MatchByValueAsync(assetIds, query.PropertyId, value, includeDescriptions: true, isInstance, exactMatch: false, cancellationToken);
            }
        }

        var assetTypeId = (asset ?? throw new NullReferenceException("Asset not found.")).AssetTypeId;
        var relation = await generated.GetRelationAsync(assetTypeId, property.Id, cancellationToken);
        if (relation is null)
            return [];
        var useInstanceOfValues = !relation.IsInstance && (asset.IsInstance ?? false);
        return await procedures.MatchByValueAsync(assetIds, query.PropertyId, value, query.IncludeDescriptions, useInstanceOfValues, exactMatch: true, cancellationToken);
    }
}

public sealed record GetAssetsByPropertyValuesQuery(Guid AssetTypeId, IReadOnlyList<KeyValuePair<Guid, string>>? PropertyValues);

public interface IGetAssetsByPropertyValuesQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetsByPropertyValuesQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetAssetsByPropertyValuesQueryHandler(IFilterProcedures procedures) : IGetAssetsByPropertyValuesQueryHandler
{
    public async Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetsByPropertyValuesQuery query, CancellationToken cancellationToken = default) =>
        new Success<List<KeyValuePair<Guid, string>>>([.. await procedures.GetByPropertyValuesAsync(query.AssetTypeId,
            query.PropertyValues ?? throw new NullReferenceException("propertyIdsAndValues is null."), cancellationToken)]);
}
