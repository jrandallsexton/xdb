using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xdb.Api.Infrastructure.Data.Procedures;

/// <summary>The original's asset reads (asset editor, property values, lookups) with its SQL text and helper semantics.</summary>
public interface IAssetProcedures
{
    Task<AssetRow?> GetAssetAsync(Guid assetId, CancellationToken cancellationToken);
    /// <summary>
    /// The asset's approved values for the properties: live ones by the lookup text descending
    /// (spr_PropertyValueList_GetForAnAsset), or with history by Created (..IncludeDeleted). No ids, no query.
    /// </summary>
    Task<IReadOnlyList<PropertyValueRow>> GetAssetValuesAsync(Guid assetId, IReadOnlyList<Guid> propertyIds, bool includeDeleted, CancellationToken cancellationToken);
    Task<string> GetAssetDisplayValueTextAsync(Guid assetId, CancellationToken cancellationToken);
    Task<string> GetAssetNameTextAsync(Guid assetId, CancellationToken cancellationToken);
    Task<Guid?> GetInstanceOfIdAsync(Guid assetId, CancellationToken cancellationToken);
    /// <summary>spr_Asset_IsInstance; false when there is no row.</summary>
    Task<bool> IsInstanceAsync(Guid assetId, CancellationToken cancellationToken);
    /// <summary>The live parent-child relation's parent asset, or null.</summary>
    Task<Guid?> GetParentAssetIdAsync(Guid assetId, CancellationToken cancellationToken);
    Task<Guid?> GetAssetCreatedByAsync(Guid assetId, CancellationToken cancellationToken);
    /// <summary>The first approved live value id of the property for the asset, or the empty Guid.</summary>
    Task<Guid> GetApprovedValueIdAsync(Guid propertyId, Guid assetId, CancellationToken cancellationToken);
    /// <summary>spr_PropertyValue_Get (Value trimmed, as PropertyValueDal.Get did); null when absent.</summary>
    Task<PropertyValueRow?> GetPropertyValueAsync(Guid propertyValueId, CancellationToken cancellationToken);
    Task<MemberRow?> GetMemberAsync(Guid memberId, CancellationToken cancellationToken);
    Task<DocumentRow?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken);
    /// <summary>A picklist value's display text with the value id passed as a string (a non-Guid fails in SQL), trimmed.</summary>
    Task<string> GetPickListValueTextAsync(Guid pickListId, string valueId, CancellationToken cancellationToken);
    Task<UrlRow?> GetUrlAsync(Guid urlId, CancellationToken cancellationToken);
    Task<string> GetAssetDescriptionAsync(Guid assetId, CancellationToken cancellationToken);
    /// <summary>A currency symbol's Value, trimmed; "" when absent.</summary>
    Task<string> GetCurrencySymbolTextAsync(Guid currencySymbolId, CancellationToken cancellationToken);
    /// <summary>Assets.LastModified round-tripped through en-US text; null when absent or NULL.</summary>
    Task<DateTime?> GetAssetLastModifiedAsync(Guid assetId, CancellationToken cancellationToken);

    // Asset editor cache, hits and row security
    Task<byte[]?> GetAssetClientCacheAsync(Guid assetId, Guid viewId, Guid userId, CancellationToken cancellationToken);
    Task InsertAssetClientCacheAsync(Guid assetId, Guid viewId, Guid userId, byte[] data, CancellationToken cancellationToken);
    Task InsertAssetHitAsync(Guid assetId, Guid memberId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetRolesForUserAndViewAsync(Guid memberId, Guid viewId, CancellationToken cancellationToken);
    /// <summary>spr_Roles_GetEditable's role ids, in its order (repeats included).</summary>
    Task<IReadOnlyList<Guid>> GetEditableRoleIdsAsync(Guid memberId, CancellationToken cancellationToken);
    /// <summary>Rows for the asset in a role view; a missing view fails (SQL 208), as in the original.</summary>
    Task<int> CountInRoleViewAsync(string roleViewName, Guid assetId, CancellationToken cancellationToken);

    // Lookups by name
    /// <summary>spr_Asset_GetId: the first asset of the type whose Name or DisplayValue equals the text (deleted included).</summary>
    Task<Guid?> FindAssetIdAsync(string? assetName, Guid assetTypeId, CancellationToken cancellationToken);
    /// <summary>Asset ids with exactly that Name among the types (live only), or across all assets when no types.</summary>
    Task<IReadOnlyList<Guid>> GetAssetIdsByNameAsync(string? assetName, IReadOnlyList<Guid>? assetTypeIds, CancellationToken cancellationToken);

    // Type-level dictionaries and counts
    /// <summary>Live approved definitions of one type: (id, trimmed display value [+ " (description)"]), by name or description then name.</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetDefinitionDictionaryAsync(Guid assetTypeId, bool includeDescriptions, CancellationToken cancellationToken);
    /// <summary>The same over several types (empty list: empty result, no query).</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetDefinitionDictionaryAsync(IReadOnlyList<Guid> assetTypeIds, bool includeDescriptions, CancellationToken cancellationToken);
    /// <summary>Live approved instances (untrimmed display value), of one type or several (spr_AssetInstances_*).</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetInstanceDictionaryAsync(Guid assetTypeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetInstanceDictionaryAsync(IReadOnlyList<Guid> assetTypeIds, CancellationToken cancellationToken);
    /// <summary>Rows in a generated table (a missing table fails with SQL 208, as in the original).</summary>
    Task<int> CountRowsAsync(string tableName, CancellationToken cancellationToken);
    /// <summary>Assets of the types whose Name contains the text (LIKE, wildcards not escaped), with descriptions appended.</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> SearchByNameAsync(IReadOnlyList<Guid> assetTypeIds, int requestType, string? searchValue, CancellationToken cancellationToken);
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetAssetsReferencedByPropertyAsync(Guid propertyId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetAssetsReferencedByPropertyOfTypeAsync(Guid propertyId, Guid assetTypeId, CancellationToken cancellationToken);
    /// <summary>spr_AssetIds_Get; a null instance flag is a parameter "not supplied" (fails), as in the original.</summary>
    Task<IReadOnlyList<Guid>> GetAssetIdsAsync(Guid assetTypeId, bool? isInstance, CancellationToken cancellationToken);
    /// <summary>(picklist value or "[No Value]", asset id, display value) for the assets, by value then name.</summary>
    Task<IReadOnlyList<(string Group, Guid AssetId, string Name)>> GetPickListGroupsAsync(IReadOnlyList<Guid> assetIds, Guid propertyId, CancellationToken cancellationToken);
}
