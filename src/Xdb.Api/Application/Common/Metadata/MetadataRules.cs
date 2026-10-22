using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.GeneratedObjects;
using Xdb.Api.Infrastructure.Data.Procedures;

namespace Xdb.Api.Application.Common.Metadata;

/// <summary>
/// The original layer methods that several metadata reads share (AssetTypeLayer, PropertyLayer), with their
/// ordering and de-duplication rules. Dictionaries are lists of pairs in the original's insertion order.
/// </summary>
public sealed class MetadataRules(IMetadataProcedures procedures)
{
    /// <summary>
    /// AssetTypeDal.Children: all live descendants (by name) then the type itself when
    /// <paramref name="includeAllDescendants"/>; otherwise the direct live children.
    /// </summary>
    public async Task<List<Guid>> ChildrenAsync(Guid assetTypeId, bool includeAllDescendants, CancellationToken cancellationToken)
    {
        if (!includeAllDescendants)
            return [.. await procedures.GetDirectChildAssetTypeIdsAsync(assetTypeId, cancellationToken)];
        var list = (await procedures.GetDescendantAssetTypeIdsAsync(assetTypeId, cancellationToken)).ToList();
        list.Add(assetTypeId);
        return list;
    }

    /// <summary>True when the type has at least one live descendant.</summary>
    public async Task<bool> HasChildAssetTypesAsync(Guid assetTypeId, CancellationToken cancellationToken) =>
        (await ChildrenAsync(assetTypeId, includeAllDescendants: true, cancellationToken)).Count != 1;

    /// <summary>AssetTypeLayer.HasAssets. "Both" counts definitions.</summary>
    public async Task<bool> HasAssetsAsync(Guid assetTypeId, AssetRequestType requestType, bool searchChildAssetTypes, CancellationToken cancellationToken)
    {
        var isInstance = requestType == AssetRequestType.Instance;
        var found = await procedures.HasAssetsAsync(assetTypeId, isInstance, cancellationToken);
        if (!found && searchChildAssetTypes)
        {
            foreach (var child in await ChildrenAsync(assetTypeId, includeAllDescendants: true, cancellationToken))
            {
                found = await procedures.HasAssetsAsync(child, isInstance, cancellationToken);
                if (found)
                    break;
            }
        }
        return found;
    }

    /// <summary>
    /// PropertyLayer.GetDictionaryByAssetTypeId: the type's property set; the system AssetName, InstanceOf and
    /// InstanceOfDescription properties renamed to the type's labels; then, when any of those is present, sorted by
    /// name under en-US and de-duplicated by name (first in query order wins).
    /// </summary>
    public async Task<List<KeyValuePair<Guid, string>>> PropertyDictionaryAsync(Guid assetTypeId, AssetRequestType requestType,
        bool includeInherited, bool includeInheritedValue, bool includeDefinition, bool includeInstance, bool excludeUnused,
        CancellationToken cancellationToken)
    {
        var hasChildren = await HasChildAssetTypesAsync(assetTypeId, cancellationToken);
        var names = (await procedures.GetAssetTypePropertyNamesAsync(assetTypeId, includeInherited, includeInheritedValue,
            includeDefinition, includeInstance, excludeUnused, hasChildren, cancellationToken)).ToList();

        Guid? assetName = null, instanceOf = null, instanceOfDescription = null;
        foreach (var property in await procedures.GetPropertiesAsync(names.Select(n => n.Key).ToList(), cancellationToken))
        {
            if (property.IsSystem != true)
                continue;
            switch (property.SystemTypeId)
            {
                case (int)SystemType.AssetName: assetName = property.Id; break;
                case (int)SystemType.InstanceOf: instanceOf = property.Id; break;
                case (int)SystemType.InstanceOfDescription: instanceOfDescription = property.Id; break;
            }
        }

        if (assetName is { } a && await procedures.GetReportingLabelAsync(assetTypeId, requestType == AssetRequestType.Instance, cancellationToken) is { Length: > 0 } nameLabel)
            Rename(names, a, nameLabel);
        if (instanceOf is { } i && await procedures.GetReportingLabelAsync(assetTypeId, requestType == AssetRequestType.Definition, cancellationToken) is { Length: > 0 } ofLabel)
            Rename(names, i, ofLabel);
        if (instanceOfDescription is { } d && await procedures.GetReportingLabelAsync(assetTypeId, requestType == AssetRequestType.Definition, cancellationToken) is { Length: > 0 } descLabel)
            Rename(names, d, descLabel + " Description");

        return assetName is null && instanceOf is null && instanceOfDescription is null ? names : SortByName(names);
    }

    /// <summary>Helpers.Sort: en-US sorted by text, de-duplicated by text, first one met wins.</summary>
    public static List<KeyValuePair<Guid, string>> SortByName(IEnumerable<KeyValuePair<Guid, string>> values)
    {
        var sorted = new SortedDictionary<string, Guid>(LegacyCollation.EnUs);
        foreach (var (id, name) in values)
            sorted.TryAdd(name, id);
        return sorted.Select(s => new KeyValuePair<Guid, string>(s.Value, s.Key)).ToList();
    }

    private static void Rename(List<KeyValuePair<Guid, string>> names, Guid id, string name)
    {
        var index = names.FindIndex(n => n.Key == id);
        if (index >= 0)
            names[index] = new KeyValuePair<Guid, string>(id, name);
    }
}

/// <summary>The original's EAssetRequestType. Values outside the range bind (any integer) and match no case.</summary>
public enum AssetRequestType
{
    Definition = 0,
    Instance = 1,
    Both = 2
}
