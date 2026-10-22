using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Metadata.Legacy;
using Xdb.Api.Infrastructure.Background;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;
using Xdb.Core.LegacyWire;

namespace Xdb.Api.Application.Metadata.Queries.Properties;

public sealed record GetCachedPropertiesQuery(Guid AssetTypeId, AssetRequestType RequestType, bool IncludeDefinition, bool IncludeInstance, bool ExcludeUnused);

public interface IGetCachedPropertiesQueryHandler
{
    Task<Result<string>> ExecuteAsync(GetCachedPropertiesQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Properties_Get: the property dictionary as hand-built JSON, read from the client cache when present. On a miss the
/// JSON is built and stored in the background after responding, as the original did; a hit returns the stored text
/// even if metadata has changed since.
/// </summary>
public sealed class GetCachedPropertiesQueryHandler(IMetadataProcedures procedures, MetadataRules rules, IBackgroundWorkQueue background)
    : IGetCachedPropertiesQueryHandler
{
    public async Task<Result<string>> ExecuteAsync(GetCachedPropertiesQuery query, CancellationToken cancellationToken = default)
    {
        // The original's key: lowercase id, the enum's number, then 1/0 for each flag.
        var key = $"{query.AssetTypeId:D}{(int)query.RequestType}{Bit(query.IncludeDefinition)}{Bit(query.IncludeInstance)}{Bit(query.ExcludeUnused)}";
        var cached = await procedures.GetClientCacheAsync(key, cancellationToken);
        if (!string.IsNullOrEmpty(cached))
            return new Success<string>(cached);

        var values = await rules.PropertyDictionaryAsync(query.AssetTypeId, query.RequestType, includeInherited: true, includeInheritedValue: true,
            query.IncludeDefinition, query.IncludeInstance, query.ExcludeUnused, cancellationToken);
        var json = LegacyFormatters.FormatDictionary(values);
        background.Enqueue($"client cache {key}", (services, ct) => services.GetRequiredService<IMetadataProcedures>().SetClientCacheAsync(key, json, ct));
        return new Success<string>(json);
    }

    private static string Bit(bool value) => value ? "1" : "0";
}

public sealed record GetAssetTypePropertiesQuery(Guid AssetTypeId, AssetRequestType RequestType, bool IncludeInherited);

public interface IGetAssetTypePropertiesQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetTypePropertiesQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Properties_GetByAssetTypeId: definition properties, plus instance properties unless the request is Definition.</summary>
public sealed class GetAssetTypePropertiesQueryHandler(MetadataRules rules) : IGetAssetTypePropertiesQueryHandler
{
    public async Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetTypePropertiesQuery query, CancellationToken cancellationToken = default) =>
        new Success<List<KeyValuePair<Guid, string>>>(await rules.PropertyDictionaryAsync(query.AssetTypeId, query.RequestType, query.IncludeInherited,
            includeInheritedValue: true, includeDefinition: true, includeInstance: query.RequestType != AssetRequestType.Definition, excludeUnused: false,
            cancellationToken));
}

public sealed record GetPropertiesForCustomFieldQuery(int ReportFieldType, Guid AssetTypeId, AssetRequestType RequestType, bool IncludeInherited);

public interface IGetPropertiesForCustomFieldQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetPropertiesForCustomFieldQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Properties_GetByECustomReportFieldType: the type's properties whose data type suits the custom field type.</summary>
public sealed class GetPropertiesForCustomFieldQueryHandler(IMetadataProcedures procedures, MetadataRules rules) : IGetPropertiesForCustomFieldQueryHandler
{
    private static readonly int[] Numeric = [(int)DataType.Float, (int)DataType.Int, (int)DataType.Currency];
    private static readonly int[] Concatenable = [(int)DataType.Float, (int)DataType.Int, (int)DataType.Currency, (int)DataType.String, (int)DataType.PickList];
    private static readonly int[] Dates = [(int)DataType.Date, (int)DataType.DateTime];

    public async Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetPropertiesForCustomFieldQuery query, CancellationToken cancellationToken = default)
    {
        var allowed = query.ReportFieldType switch
        {
            1 or 4 or 5 => Numeric,      // Calculated, RangeLabel, ValueLabel
            2 => Concatenable,           // Concatenation
            3 => Dates,                  // TimeElapsed
            _ => []
        };
        var result = new List<KeyValuePair<Guid, string>>();
        foreach (var item in await rules.PropertyDictionaryAsync(query.AssetTypeId, query.RequestType, query.IncludeInherited, includeInheritedValue: true,
                     includeDefinition: true, includeInstance: query.RequestType != AssetRequestType.Definition, excludeUnused: false, cancellationToken))
        {
            if (Array.IndexOf(allowed, await DataTypeAsync(procedures, item.Key, cancellationToken)) >= 0)
                result.Add(item);
        }
        return new Success<List<KeyValuePair<Guid, string>>>(result);
    }

    /// <summary>PropertyLayer.DataType: NULL is Undefined (-1); a missing property fails.</summary>
    internal static async Task<int> DataTypeAsync(IMetadataProcedures procedures, Guid propertyId, CancellationToken cancellationToken) =>
        await procedures.GetPropertyDataTypeIdAsync(propertyId, cancellationToken);
}

public sealed record GetNonFreeformPropertiesQuery(Guid AssetTypeId, AssetRequestType RequestType, bool IncludeInherited);

public interface IGetNonFreeformPropertiesQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetNonFreeformPropertiesQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Properties_GetNonFreeformByAssetTypeId: user, asset and picklist properties, the system Created/CreatedBy/
/// InstanceOf/LastModified/LastModifiedBy properties, and AssetType when the type has descendants; en-US by name.
/// </summary>
public sealed class GetNonFreeformPropertiesQueryHandler(IMetadataProcedures procedures, MetadataRules rules) : IGetNonFreeformPropertiesQueryHandler
{
    public async Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetNonFreeformPropertiesQuery query, CancellationToken cancellationToken = default)
    {
        var hasChildren = await rules.HasChildAssetTypesAsync(query.AssetTypeId, cancellationToken);
        var sorted = new SortedDictionary<string, Guid>(GeneratedObjects.LegacyCollation.EnUs);
        foreach (var (id, name) in await rules.PropertyDictionaryAsync(query.AssetTypeId, query.RequestType, query.IncludeInherited, includeInheritedValue: true,
                     includeDefinition: true, includeInstance: query.RequestType != AssetRequestType.Definition, excludeUnused: false, cancellationToken))
        {
            var systemTypeId = await procedures.GetPropertySystemTypeIdAsync(id, cancellationToken);
            var systemType = systemTypeId < 0 ? SystemType.NotApplicable : (SystemType)systemTypeId;
            switch (systemType)
            {
                case SystemType.NotApplicable:
                    if (await procedures.GetPropertyDataTypeIdAsync(id, cancellationToken) is (int)DataType.User or (int)DataType.Asset or (int)DataType.PickList)
                        sorted.Add(name, id);
                    break;
                case SystemType.AssetType:
                    if (hasChildren)
                        sorted.Add(name, id);
                    break;
                case SystemType.Created:
                case SystemType.CreatedBy:
                case SystemType.InstanceOf:
                case SystemType.LastModified:
                case SystemType.LastModifiedBy:
                    sorted.Add(name, id);
                    break;
            }
        }
        var result = new List<KeyValuePair<Guid, string>>();
        foreach (var (name, id) in sorted)
            result.Add(new KeyValuePair<Guid, string>(id, name));
        return new Success<List<KeyValuePair<Guid, string>>>(result);
    }
}

public sealed record GetPickListPropertiesQuery(Guid PickListId);

public interface IGetPickListPropertiesQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetPickListPropertiesQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetPickListPropertiesQueryHandler(IMetadataProcedures procedures) : IGetPickListPropertiesQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetPickListPropertiesQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await procedures.GetPropertiesByPickListAsync(query.PickListId, cancellationToken));
}

public sealed record GetPropertiesDictionaryQuery(bool IncludeDeleted);

public interface IGetPropertiesDictionaryQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetPropertiesDictionaryQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetPropertiesDictionaryQueryHandler(IMetadataProcedures procedures) : IGetPropertiesDictionaryQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetPropertiesDictionaryQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await procedures.GetPropertiesDictionaryAsync(query.IncludeDeleted, cancellationToken));
}

public sealed record GetPropertyQuery(Guid PropertyId);

public interface IGetPropertyQueryHandler
{
    Task<Result<PropertyContract?>> ExecuteAsync(GetPropertyQuery query, CancellationToken cancellationToken = default);
}

/// <summary>A property, deleted ones included; null when absent.</summary>
public sealed class GetPropertyQueryHandler(IMetadataProcedures procedures) : IGetPropertyQueryHandler
{
    public async Task<Result<PropertyContract?>> ExecuteAsync(GetPropertyQuery query, CancellationToken cancellationToken = default) =>
        new Success<PropertyContract?>(await procedures.GetPropertyAsync(query.PropertyId, cancellationToken) is { } row ? MetadataLoader.Property(row) : null);
}

public sealed record GetPropertyNameQuery(Guid PropertyId);

public interface IGetPropertyNameQueryHandler
{
    Task<Result<string>> ExecuteAsync(GetPropertyNameQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetPropertyNameQueryHandler(IMetadataProcedures procedures) : IGetPropertyNameQueryHandler
{
    public async Task<Result<string>> ExecuteAsync(GetPropertyNameQuery query, CancellationToken cancellationToken = default) =>
        new Success<string>(await procedures.GetPropertyDisplayValueAsync(query.PropertyId, cancellationToken));
}

public sealed record GetViewPropertyIdsQuery(Guid ViewId);

public interface IGetViewPropertyIdsQueryHandler
{
    Task<Result<IReadOnlyList<Guid>>> ExecuteAsync(GetViewPropertyIdsQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetViewPropertyIdsQueryHandler(IMetadataProcedures procedures) : IGetViewPropertyIdsQueryHandler
{
    public async Task<Result<IReadOnlyList<Guid>>> ExecuteAsync(GetViewPropertyIdsQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<Guid>>(await procedures.GetViewPropertyIdsAsync(query.ViewId, cancellationToken));
}
