using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xdb.Api.Infrastructure.Data.Procedures;

/// <summary>A related item as RelatedItemDal read it: NULL strings stay null, NULL ids become the empty Guid.</summary>
public sealed class RelatedItemRow
{
    public string? AssetTypeSingular { get; set; }
    public string? AssetTypePlural { get; set; }
    public Guid ViewId { get; set; }
    public Guid AssetId { get; set; }
    public string? AssetName { get; set; }
    public string? PropertyName { get; set; }
    public Guid InstanceOfId { get; set; }
    public string? InstanceOfName { get; set; }
    public string? InstanceOfDesc { get; set; }
}

public sealed record AssetHitRow(Guid AssetId, string Name, int Hits);

public sealed record RecentAssetRow(Guid AssetId, string Name, Guid AssetTypeId, Guid? DefaultViewId);

/// <summary>The original UI layer's reads (related items, hits, view asset lists, default views), with its SQL text.</summary>
public interface IUiProcedures
{
    /// <summary>RelatedItemDal.RelatedItems_Get(assetId, userId, isInstance, includeDataTypeRelations); a non-null user adds the role joins.</summary>
    Task<List<RelatedItemRow>> GetRelatedItemsAsync(Guid assetId, Guid? userId, bool isInstance, bool includeDataTypeRelations, CancellationToken cancellationToken);
    /// <summary>RelatedItemDal.RelatedItems_Get(propertyValue): the value interpolated unescaped (null as '').</summary>
    Task<List<RelatedItemRow>> GetRelatedItemsByValueAsync(string? propertyValue, CancellationToken cancellationToken);
    /// <summary>AssetTypeLayer.GetIdByName: the first type (deleted included) with the name, or the empty Guid.</summary>
    Task<Guid> GetAssetTypeIdByNameAsync(string name, CancellationToken cancellationToken);

    /// <summary>AssetHitDal.AssetHits_get: the type's live assets by hit count (0 definitions, 1 instances, else both).</summary>
    Task<IReadOnlyList<AssetHitRow>> GetAssetHitsAsync(Guid assetTypeId, int requestType, CancellationToken cancellationToken);
    /// <summary>spr_AssetHits_GetForUser: up to 10 of the member's recent assets.</summary>
    Task<IReadOnlyList<RecentAssetRow>> GetRecentAssetsAsync(Guid memberId, CancellationToken cancellationToken);

    /// <summary>AssetDal.GetDictionary: a gen table's (AssetId, Name) by name, optionally where the name contains the (unescaped) term.</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetGenTableDictionaryAsync(string tableName, string assetTypeName, string? searchTerm, CancellationToken cancellationToken);

    /// <summary>ViewDal.IsValidId: exactly one live view with the id.</summary>
    Task<bool> IsValidViewIdAsync(Guid viewId, CancellationToken cancellationToken);
    /// <summary>AssetTypeDal.GetDefaultViewId: the type's default (instance) view, or the empty Guid.</summary>
    Task<Guid> GetDefaultViewIdAsync(Guid assetTypeId, bool isInstance, CancellationToken cancellationToken);
}
