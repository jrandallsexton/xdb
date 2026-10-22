using System;
using System.Collections.Generic;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Infrastructure.Data.Procedures;

namespace Xdb.Api.Application.GeneratedObjects.GenTables;

/// <summary>
/// A gen table's columns, chosen and ordered as the original's GetDictionaryByAssetTypeId and Helpers.Sort did:
/// first row per id in the query's order; the system AssetName, InstanceOf and InstanceOfDescription properties
/// renamed to the type's labels; then, when any of those three is present, sorted by name under en-US and
/// de-duplicated by name, keeping the first one met.
/// </summary>
public static class GenPropertySet
{
    /// <param name="rows">The property-set query's rows, in its order.</param>
    /// <param name="properties">Metadata for every id in <paramref name="rows"/>.</param>
    /// <param name="assetNameLabel">ReportingLabel(type, this pass is Instance).</param>
    /// <param name="instanceOfLabel">ReportingLabel(type, this pass is Definition).</param>
    public static IReadOnlyList<Guid> Order(
        IReadOnlyList<GenPropertyNameRow> rows,
        IReadOnlyDictionary<Guid, PropertyMetaRow> properties,
        string assetNameLabel,
        string instanceOfLabel)
    {
        var names = new List<KeyValuePair<Guid, string>>();
        var positions = new Dictionary<Guid, int>();
        foreach (var row in rows)
        {
            if (positions.ContainsKey(row.Id))
                continue;
            positions.Add(row.Id, names.Count);
            names.Add(new KeyValuePair<Guid, string>(row.Id, row.PropertyName));
        }

        Guid? assetName = null, instanceOf = null, instanceOfDescription = null;
        foreach (var (id, _) in names)
        {
            if (!properties.TryGetValue(id, out var property) || !property.IsSystem)
                continue;
            switch (property.SystemTypeId)
            {
                case (int)SystemType.AssetName: assetName = id; break;
                case (int)SystemType.InstanceOf: instanceOf = id; break;
                case (int)SystemType.InstanceOfDescription: instanceOfDescription = id; break;
            }
        }

        if (assetName is { } a && !string.IsNullOrEmpty(assetNameLabel))
            names[positions[a]] = new KeyValuePair<Guid, string>(a, assetNameLabel);
        if (instanceOf is { } i && !string.IsNullOrEmpty(instanceOfLabel))
            names[positions[i]] = new KeyValuePair<Guid, string>(i, instanceOfLabel);
        if (instanceOfDescription is { } d && !string.IsNullOrEmpty(instanceOfLabel))
            names[positions[d]] = new KeyValuePair<Guid, string>(d, instanceOfLabel + " Description");

        if (assetName is null && instanceOf is null && instanceOfDescription is null)
            return names.ConvertAll(n => n.Key);

        var sorted = new SortedDictionary<string, Guid>(LegacyCollation.EnUs);
        foreach (var (id, name) in names)
            sorted.TryAdd(name, id);
        return [.. sorted.Values];
    }
}
