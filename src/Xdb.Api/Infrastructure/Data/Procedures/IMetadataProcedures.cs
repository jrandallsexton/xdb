using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xdb.Api.Infrastructure.Data.Procedures;

/// <summary>
/// The original's metadata reads (asset types, properties, picklists, lookups), with the original's SQL text and
/// its scalar-helper semantics: strings trimmed and "" when there is no row or the value is NULL; Guids empty when
/// there is no row; integer reads that throw when there is no row. Lists keep the order the SQL returns.
/// </summary>
public interface IMetadataProcedures
{
    // Asset types
    Task<string> GetAssetTypeNameAsync(Guid assetTypeId, CancellationToken cancellationToken);
    Task<string> GetAssetTypePluralizationAsync(Guid assetTypeId, CancellationToken cancellationToken);
    /// <summary>The live parent relation's parent id, or null.</summary>
    Task<Guid?> GetParentAssetTypeIdAsync(Guid childAssetTypeId, CancellationToken cancellationToken);
    /// <summary>Throws when the type does not exist (the original dereferenced the missing row).</summary>
    Task<bool> GetAllowAssetsAsync(Guid assetTypeId, CancellationToken cancellationToken);
    /// <summary>spr_AssetType_HasAssets: live assets of exactly this type and definition/instance.</summary>
    Task<bool> HasAssetsAsync(Guid assetTypeId, bool isInstance, CancellationToken cancellationToken);
    /// <summary>Direct live children, by name.</summary>
    Task<IReadOnlyList<Guid>> GetDirectChildAssetTypeIdsAsync(Guid parentAssetTypeId, CancellationToken cancellationToken);
    /// <summary>spr_AssetType_GetAllDescendants: live descendants, by name.</summary>
    Task<IReadOnlyList<Guid>> GetDescendantAssetTypeIdsAsync(Guid assetTypeId, CancellationToken cancellationToken);
    Task<string> GetReportingLabelAsync(Guid assetTypeId, bool forInstances, CancellationToken cancellationToken);
    /// <summary>spr_AssetType_Get: null for a missing or deleted type.</summary>
    Task<AssetTypeRow?> GetAssetTypeAsync(Guid assetTypeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AssetTypePropertyRelationRow>> GetAssetTypePropertyRelationsAsync(Guid assetTypeId, CancellationToken cancellationToken);
    /// <summary>spr_PropertyList_GetByAssetTypeId (no ORDER BY, as the original).</summary>
    Task<IReadOnlyList<PropertyDetailRow>> GetAssetTypePropertiesAsync(Guid assetTypeId, CancellationToken cancellationToken);
    /// <summary>spr_AssetTypes_GetDictionary.</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetAssetTypesDictionaryAsync(bool includeUnused, CancellationToken cancellationToken);
    /// <summary>Types that never had a parent (no Deleted filter), by name.</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetRootAssetTypesAsync(CancellationToken cancellationToken);
    /// <summary>Default (instance) views per type; <paramref name="viewColumn"/> is the original's column expression.</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, Guid>>> GetDefaultViewsAsync(Guid? userId, string viewColumn, CancellationToken cancellationToken);
    /// <summary>spr_Asset_GetAssetTypeId: null when the asset does not exist.</summary>
    Task<Guid?> GetAssetTypeIdOfAssetAsync(Guid assetId, CancellationToken cancellationToken);

    // Processes and views
    Task<Guid?> GetProcessStepViewIdAsync(Guid processId, int stepIndex, CancellationToken cancellationToken);
    Task<Guid> GetViewAssetTypeIdAsync(Guid viewId, CancellationToken cancellationToken);
    /// <summary>spr_View_GetPropertyIds (DISTINCT, no ORDER BY).</summary>
    Task<IReadOnlyList<Guid>> GetViewPropertyIdsAsync(Guid viewId, CancellationToken cancellationToken);

    // Members
    Task<bool> IsAdminAsync(Guid memberId, Guid adminRoleId, CancellationToken cancellationToken);
    Task<bool> IsLiveAssetIdAsync(Guid assetId, CancellationToken cancellationToken);
    Task<bool> IsLiveMemberIdAsync(Guid memberId, CancellationToken cancellationToken);
    /// <summary>spr_Members_GetDictionary: live members' asset display values.</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetMembersDictionaryAsync(CancellationToken cancellationToken);

    // Properties
    /// <summary>The original's property-set query for an asset type (verbatim, no tie-breaker), rows with a NULL column skipped.</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetAssetTypePropertyNamesAsync(Guid assetTypeId, bool includeInherited,
        bool includeInheritedValue, bool includeDefinition, bool includeInstance, bool excludeUnused, bool hasChildAssetTypes,
        CancellationToken cancellationToken);
    /// <summary>spr_Properties_GetByListOfPropertyIds (deleted included, by DisplayValue).</summary>
    Task<IReadOnlyList<PropertyDetailRow>> GetPropertiesAsync(IReadOnlyCollection<Guid> propertyIds, CancellationToken cancellationToken);
    /// <summary>spr_Property_Get (deleted included); null when absent.</summary>
    Task<PropertyDetailRow?> GetPropertyAsync(Guid propertyId, CancellationToken cancellationToken);
    /// <summary>The DataTypeId; -1 when NULL. Throws when the property does not exist.</summary>
    Task<int> GetPropertyDataTypeIdAsync(Guid propertyId, CancellationToken cancellationToken);
    /// <summary>The SystemTypeId; -1 when NULL. Throws when the property does not exist.</summary>
    Task<int> GetPropertySystemTypeIdAsync(Guid propertyId, CancellationToken cancellationToken);
    Task<string> GetPropertyDisplayValueAsync(Guid propertyId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetPropertiesByPickListAsync(Guid pickListId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetPropertiesDictionaryAsync(bool includeDeleted, CancellationToken cancellationToken);

    // Picklists
    Task<PickListRow?> GetPickListAsync(Guid pickListId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PickListValueDetailRow>> GetPickListValuesAsync(Guid pickListId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PickListDependencyRow>> GetPickListDependenciesAsync(Guid childPickListId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetPickListValueDictionaryAsync(Guid pickListId, CancellationToken cancellationToken);
    Task<string> GetPickListNameAsync(Guid pickListId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetPickListsDictionaryAsync(bool includeDeleted, CancellationToken cancellationToken);

    // Lookups
    Task<IReadOnlyList<KeyValuePair<int, string>>> GetOperatorsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<KeyValuePair<int, string>>> GetPermissionsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetCurrencySymbolsAsync(CancellationToken cancellationToken);
    Task<CurrencyValueRow?> GetCurrencyValueAsync(Guid currencyValueId, CancellationToken cancellationToken);
    /// <summary>System images by name, first row per id.</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetSystemImageDictionaryAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetSystemImageIdsAsync(CancellationToken cancellationToken);
    /// <summary>spr_Image_Get (live images); null when absent or deleted.</summary>
    Task<ImageRow?> GetImageAsync(Guid imageId, CancellationToken cancellationToken);

    // Asset dictionaries and generic lookups
    /// <summary>Live, approved definition assets of one type: (id, trimmed name or display value), by name.</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetAssetDictionaryAsync(Guid assetTypeId, bool useDisplayValues, CancellationToken cancellationToken);
    /// <summary>Live, approved definition assets of several types: (id, trimmed display value), by name.</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetAssetDictionaryAsync(IReadOnlyList<Guid> assetTypeIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetDatePartsAsync(CancellationToken cancellationToken);
    /// <summary>
    /// SqlBaseDal.GetDictionary(sql): first row per key wins; a NULL key becomes the empty Guid with "NULL"; a key
    /// that is not a Guid becomes 99999999-... with its value (or "[ Empty String ]" when NULL).
    /// </summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetLegacyDictionaryAsync(string sql, CancellationToken cancellationToken);

    // Client cache (the original's ClientCache table)
    /// <summary>The stored value, or "" when there is none.</summary>
    Task<string> GetClientCacheAsync(string key, CancellationToken cancellationToken);
    Task SetClientCacheAsync(string key, string value, CancellationToken cancellationToken);
}
