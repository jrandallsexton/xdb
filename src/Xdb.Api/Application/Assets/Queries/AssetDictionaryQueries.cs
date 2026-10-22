using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects;
using Xdb.Api.Application.Metadata;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Assets.Queries;

/// <summary>The original's AssetLayer dictionary rules shared by the type-level reads.</summary>
public sealed class AssetDictionaries(IAssetProcedures assets, IMetadataProcedures metadata, MetadataRules rules)
{
    /// <summary>
    /// Assets_GetDictionary: members for the User type; the type's own definitions when it has any (unapproved
    /// counted); else, when searching children, the definitions of its descendants and itself; else nothing.
    /// </summary>
    public async Task<List<KeyValuePair<Guid, string>>> DefinitionsAsync(Guid assetTypeId, bool includeDescriptions, bool searchChildren,
        CancellationToken cancellationToken)
    {
        if (assetTypeId == SystemAssetTypeIds.User)
            return [.. await metadata.GetMembersDictionaryAsync(cancellationToken)];
        if (await rules.HasAssetsAsync(assetTypeId, AssetRequestType.Both, searchChildAssetTypes: false, cancellationToken))
            return [.. await assets.GetDefinitionDictionaryAsync(assetTypeId, includeDescriptions, cancellationToken)];
        if (searchChildren)
            return [.. await assets.GetDefinitionDictionaryAsync(await rules.ChildrenAsync(assetTypeId, includeAllDescendants: true, cancellationToken),
                includeDescriptions, cancellationToken)];
        return [];
    }

    /// <summary>AssetInstances_Get: the instances of the type, or of its descendants and itself.</summary>
    public async Task<List<KeyValuePair<Guid, string>>> InstancesAsync(Guid assetTypeId, bool includeChildren, CancellationToken cancellationToken) =>
        includeChildren
            ? [.. await assets.GetInstanceDictionaryAsync(await rules.ChildrenAsync(assetTypeId, includeAllDescendants: true, cancellationToken), cancellationToken)]
            : [.. await assets.GetInstanceDictionaryAsync(assetTypeId, cancellationToken)];

    /// <summary>Assets_Get: definitions, instances, or (anything else) definitions with descriptions then instances.</summary>
    public async Task<List<KeyValuePair<Guid, string>>> AssetsAsync(Guid assetTypeId, AssetRequestType requestType, bool includeChildren,
        CancellationToken cancellationToken)
    {
        switch (requestType)
        {
            case AssetRequestType.Definition:
                return await DefinitionsAsync(assetTypeId, includeDescriptions: false, includeChildren, cancellationToken);
            case AssetRequestType.Instance:
                return await InstancesAsync(assetTypeId, includeChildren, cancellationToken);
            default:
                var result = await DefinitionsAsync(assetTypeId, includeDescriptions: true, includeChildren, cancellationToken);
                var seen = result.Select(p => p.Key).ToHashSet();
                foreach (var pair in await InstancesAsync(assetTypeId, includeChildren, cancellationToken))
                {
                    if (!seen.Add(pair.Key))
                        throw new ArgumentException($"An item with the same key has already been added: {pair.Key:D}");
                    result.Add(pair);
                }
                return result;
        }
    }
}

public sealed record GetAssetsQuery(Guid AssetTypeId, AssetRequestType RequestType, bool IncludeChildren);

public interface IGetAssetsQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Assets_Get and Assets_GetByAssetTypeId (without children).</summary>
public sealed class GetAssetsQueryHandler(AssetDictionaries dictionaries) : IGetAssetsQueryHandler
{
    public async Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetsQuery query, CancellationToken cancellationToken = default) =>
        new Success<List<KeyValuePair<Guid, string>>>(await dictionaries.AssetsAsync(query.AssetTypeId, query.RequestType, query.IncludeChildren, cancellationToken));
}

public sealed record GetAssetDefinitionsQuery(Guid AssetTypeId, bool IncludeDescriptions);

public interface IGetAssetDefinitionsQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetDefinitionsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Assets_GetDictionary: the type's definitions (no child search).</summary>
public sealed class GetAssetDefinitionsQueryHandler(AssetDictionaries dictionaries) : IGetAssetDefinitionsQueryHandler
{
    public async Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetDefinitionsQuery query, CancellationToken cancellationToken = default) =>
        new Success<List<KeyValuePair<Guid, string>>>(await dictionaries.DefinitionsAsync(query.AssetTypeId, query.IncludeDescriptions, searchChildren: false, cancellationToken));
}

public sealed record GetAssetInstancesQuery(Guid AssetTypeId, bool IncludeChildren);

public interface IGetAssetInstancesQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetInstancesQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetAssetInstancesQueryHandler(AssetDictionaries dictionaries) : IGetAssetInstancesQueryHandler
{
    public async Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetInstancesQuery query, CancellationToken cancellationToken = default) =>
        new Success<List<KeyValuePair<Guid, string>>>(await dictionaries.InstancesAsync(query.AssetTypeId, query.IncludeChildren, cancellationToken));
}

public sealed record CountAssetsQuery(Guid AssetTypeId, bool Instances);

public interface ICountAssetsQueryHandler
{
    Task<Result<int>> ExecuteAsync(CountAssetsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Assets_GetCount / Assets_GetInstanceCount: rows in the type's gen table (stale for adopted tables; missing table fails).</summary>
public sealed class CountAssetsQueryHandler(IAssetProcedures assets) : ICountAssetsQueryHandler
{
    public async Task<Result<int>> ExecuteAsync(CountAssetsQuery query, CancellationToken cancellationToken = default) =>
        new Success<int>(await assets.CountRowsAsync(GeneratedObjectNames.Table(query.AssetTypeId, query.Instances, isRaw: false), cancellationToken));
}

public sealed record SearchAssetsByNameQuery(Guid AssetTypeId, int RequestType, string? SearchValue);

public interface ISearchAssetsByNameQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(SearchAssetsByNameQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Assets_SearchByName: over the type's descendants and itself; includeDesc is ignored (descriptions always appended).</summary>
public sealed class SearchAssetsByNameQueryHandler(IAssetProcedures assets, MetadataRules rules) : ISearchAssetsByNameQueryHandler
{
    public async Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(SearchAssetsByNameQuery query, CancellationToken cancellationToken = default)
    {
        var types = await rules.ChildrenAsync(query.AssetTypeId, includeAllDescendants: true, cancellationToken);
        return new Success<List<KeyValuePair<Guid, string>>>([.. await assets.SearchByNameAsync(types, query.RequestType, query.SearchValue, cancellationToken)]);
    }
}

public sealed record GetAssetsFromPropertyValueQuery(Guid PropertyId);

public interface IGetAssetsFromPropertyValueQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetsFromPropertyValueQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Assets_GetFromPropertyValue: assets referenced by the property's values; [] for an unknown or untyped property.</summary>
public sealed class GetAssetsFromPropertyValueQueryHandler(IAssetProcedures assets, IMetadataProcedures metadata) : IGetAssetsFromPropertyValueQueryHandler
{
    public async Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetsFromPropertyValueQuery query, CancellationToken cancellationToken = default)
    {
        if (await metadata.GetPropertyAsync(query.PropertyId, cancellationToken) is not { } row || MetadataLoader.Property(row).AssetTypeId is null)
            return new Success<List<KeyValuePair<Guid, string>>>([]);
        return new Success<List<KeyValuePair<Guid, string>>>([.. await assets.GetAssetsReferencedByPropertyAsync(query.PropertyId, cancellationToken)]);
    }
}

public sealed record GetAssetsInPropertyValueQuery(Guid AssetTypeId, Guid PropertyId);

public interface IGetAssetsInPropertyValueQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetsInPropertyValueQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetAssetsInPropertyValueQueryHandler(IAssetProcedures assets) : IGetAssetsInPropertyValueQueryHandler
{
    public async Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetsInPropertyValueQuery query, CancellationToken cancellationToken = default) =>
        new Success<List<KeyValuePair<Guid, string>>>([.. await assets.GetAssetsReferencedByPropertyOfTypeAsync(query.PropertyId, query.AssetTypeId, cancellationToken)]);
}

public sealed record GetAssetsGroupedQuery(Guid AssetTypeId, Guid PropertyId, bool IncludeChildAssetTypes, AssetRequestType RequestType);

public interface IGetAssetsGroupedQueryHandler
{
    Task<Result<List<KeyValuePair<string, IReadOnlyList<KeyValuePair<Guid, string>>>>>> ExecuteAsync(GetAssetsGroupedQuery query,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Assets_GetGrouped: the types' assets (definitions or instances; Both fails in spr_AssetIds_Get) grouped by their
/// picklist value text, "[No Value]" first; [] when the property is not a picklist. No assets fails (IN ()).
/// </summary>
public sealed class GetAssetsGroupedQueryHandler(IAssetProcedures assets, IMetadataProcedures metadata, MetadataRules rules) : IGetAssetsGroupedQueryHandler
{
    public async Task<Result<List<KeyValuePair<string, IReadOnlyList<KeyValuePair<Guid, string>>>>>> ExecuteAsync(GetAssetsGroupedQuery query,
        CancellationToken cancellationToken = default)
    {
        var types = new List<Guid> { query.AssetTypeId };
        if (query.IncludeChildAssetTypes)
        {
            foreach (var child in await rules.ChildrenAsync(query.AssetTypeId, includeAllDescendants: true, cancellationToken))
            {
                if (!types.Contains(child))
                    types.Add(child);
            }
        }
        bool? isInstance = query.RequestType switch
        {
            AssetRequestType.Definition => false,
            AssetRequestType.Instance => true,
            _ => null
        };
        var assetIds = new List<Guid>();
        foreach (var type in types)
        {
            foreach (var id in await assets.GetAssetIdsAsync(type, isInstance, cancellationToken))
            {
                if (!assetIds.Contains(id))
                    assetIds.Add(id);
            }
        }

        var result = new List<KeyValuePair<string, IReadOnlyList<KeyValuePair<Guid, string>>>>();
        if (await metadata.GetPropertyAsync(query.PropertyId, cancellationToken) is not { } row
            || MetadataLoader.Property(row).DataType != (int)LegacyDataType.PickList)
            return new Success<List<KeyValuePair<string, IReadOnlyList<KeyValuePair<Guid, string>>>>>(result);

        var groups = new List<(string Key, List<KeyValuePair<Guid, string>> Values)>();
        foreach (var (group, assetId, name) in await assets.GetPickListGroupsAsync(assetIds, query.PropertyId, cancellationToken))
        {
            var index = groups.FindIndex(g => g.Key == group);
            if (index < 0)
            {
                groups.Add((group, []));
                index = groups.Count - 1;
            }
            if (!groups[index].Values.Exists(v => v.Key == assetId))
                groups[index].Values.Add(new KeyValuePair<Guid, string>(assetId, name));
        }
        result.AddRange(groups.Select(g => new KeyValuePair<string, IReadOnlyList<KeyValuePair<Guid, string>>>(g.Key, g.Values)));
        return new Success<List<KeyValuePair<string, IReadOnlyList<KeyValuePair<Guid, string>>>>>(result);
    }
}
