using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects.Views;
using Xdb.Api.Infrastructure.Background;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;
using Xdb.Core.LegacyWire;

namespace Xdb.Api.Application.Metadata.Queries.Values;

public sealed record GetPotentialValuesQuery(Guid AssetTypeId, Guid PropertyId, AssetRequestType RequestType);

public interface IGetPotentialValuesQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetPotentialValuesQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Property_GetAllValues (PropertyValues_GetPotential): the distinct (raw, decoded) values of one property across the
/// type's generated tables, by value. Reads the existing generated tables, so a type without them, a column that no
/// longer exists, or a property not related to the type fails as it did in the original.
/// </summary>
public sealed class GetPotentialValuesQueryHandler(IMetadataProcedures procedures, ViewInputLoader loader) : IGetPotentialValuesQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetPotentialValuesQuery query, CancellationToken cancellationToken = default)
    {
        var name = await procedures.GetPropertyDisplayValueAsync(query.PropertyId, cancellationToken);
        var isInstance = query.RequestType == AssetRequestType.Instance;
        var input = await loader.LoadAsync(query.AssetTypeId, isInstance, [new ViewColumn(query.PropertyId, null, 0, name, null)], [], string.Empty, cancellationToken);
        var sql = ViewSql.BuildSelect(input with { ForSingleProp = true, IncludeOrderBy = true });
        return new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await procedures.GetLegacyDictionaryAsync(sql, cancellationToken));
    }
}

/// <summary>DictionaryValues_GetForProperty's response: the JSON and whether it came from the cache (which picks the writer).</summary>
public sealed record DictionaryValues(string Json, bool FromCache);

public sealed record GetDictionaryValuesQuery(Guid PropertyId, Guid AssetTypeId);

public interface IGetDictionaryValuesQueryHandler
{
    Task<Result<DictionaryValues>> ExecuteAsync(GetDictionaryValuesQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// The values a property can take, cached by property id only: whichever call fills the cache fixes the answer for
/// every later asset type. A missing property returns [] without caching. Otherwise the values depend on the property:
/// asset types, assets of a type (raw names, searching descendants), the asset type's parent's assets (display
/// values), picklist values, members, or date parts; anything else is empty. Missing data the original dereferenced
/// (a picklist or asset-type id that is NULL, an unknown asset type for relations) fails before anything is cached.
/// </summary>
public sealed class GetDictionaryValuesQueryHandler(
    IMetadataProcedures procedures,
    MetadataRules rules,
    MetadataLoader loader,
    IBackgroundWorkQueue background) : IGetDictionaryValuesQueryHandler
{
    public async Task<Result<DictionaryValues>> ExecuteAsync(GetDictionaryValuesQuery query, CancellationToken cancellationToken = default)
    {
        var key = query.PropertyId.ToString("D");
        var cached = await procedures.GetClientCacheAsync(key, cancellationToken);
        if (!string.IsNullOrEmpty(cached))
            return new Success<DictionaryValues>(new DictionaryValues(cached, FromCache: true));

        var row = await procedures.GetPropertyAsync(query.PropertyId, cancellationToken);
        if (row is null)
            return new Success<DictionaryValues>(new DictionaryValues("[]", FromCache: true)); // the original used the text/html writer here

        var property = MetadataLoader.Property(row);
        IReadOnlyList<KeyValuePair<Guid, string>> values = [];
        if (property.IsSystem)
        {
            values = property.SystemType switch
            {
                (int)SystemType.AssetType => await procedures.GetAssetTypesDictionaryAsync(includeUnused: true, cancellationToken),
                (int)SystemType.InstanceOf => await AssetsAsync(query.AssetTypeId, useDisplayValues: false, cancellationToken),
                _ => values
            };
        }
        else
        {
            switch (property.DataType)
            {
                case (int)LegacyDataType.Date:
                    values = await procedures.GetDatePartsAsync(cancellationToken);
                    break;
                case (int)LegacyDataType.PickList:
                    values = await procedures.GetPickListValueDictionaryAsync(property.PickListId!.Value, cancellationToken);
                    break;
                case (int)LegacyDataType.Asset:
                case (int)LegacyDataType.Dependency:
                    values = await AssetsAsync(property.AssetTypeId!.Value, useDisplayValues: false, cancellationToken);
                    break;
                case (int)LegacyDataType.Relation_ChildParent:
                case (int)LegacyDataType.Relation_ParentChild:
                    var assetType = await loader.AssetTypeAsync(query.AssetTypeId, cancellationToken)
                        ?? throw new NullReferenceException("Asset type not found.");
                    if (assetType.ParentId is { } parentId)
                        values = await AssetsAsync(parentId, useDisplayValues: true, cancellationToken);
                    break;
                case (int)LegacyDataType.User:
                    values = await AssetsAsync(SystemAssetTypeIds.User, useDisplayValues: true, cancellationToken);
                    break;
            }
        }

        var json = LegacyFormatters.FormatDictionary(values);
        background.Enqueue($"client cache {key}", (services, ct) => services.GetRequiredService<IMetadataProcedures>().SetClientCacheAsync(key, json, ct));
        return new Success<DictionaryValues>(new DictionaryValues(json, FromCache: false));
    }

    /// <summary>
    /// AssetLayer.Assets_GetDictionary(type, searchChildren: true): members for the User type; the type's own live
    /// definitions when it has any; otherwise the definitions of its descendants and itself (display values).
    /// </summary>
    private async Task<IReadOnlyList<KeyValuePair<Guid, string>>> AssetsAsync(Guid assetTypeId, bool useDisplayValues, CancellationToken cancellationToken)
    {
        if (assetTypeId == SystemAssetTypeIds.User)
            return await procedures.GetMembersDictionaryAsync(cancellationToken);
        if (await rules.HasAssetsAsync(assetTypeId, AssetRequestType.Both, searchChildAssetTypes: false, cancellationToken))
            return await procedures.GetAssetDictionaryAsync(assetTypeId, useDisplayValues, cancellationToken);
        return await procedures.GetAssetDictionaryAsync(await rules.ChildrenAsync(assetTypeId, includeAllDescendants: true, cancellationToken), cancellationToken);
    }
}
