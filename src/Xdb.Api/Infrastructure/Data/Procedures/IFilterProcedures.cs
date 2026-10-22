using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xdb.Api.Infrastructure.Data.Procedures;

/// <summary>A saved quick filter (spr_MemberView_Get): its view's asset type and kind, expression and filters.</summary>
public sealed record MemberFilterRow(Guid AssetTypeId, bool IsInstance, string FilterExpression, IReadOnlyList<MemberFilterItemRow> Filters);

public sealed record MemberFilterItemRow(Guid PropertyId, int OperatorId, string Value, int Order);

/// <summary>The SQL behind the original's filter-engine operations (potential values and matching assets), verbatim.</summary>
public interface IFilterProcedures
{
    /// <summary>spr_MemberView_Get read as ViewDal.MemberFilter_Get did (a NULL name or expression fails); null when absent.</summary>
    Task<MemberFilterRow?> GetMemberFilterAsync(Guid memberViewId, CancellationToken cancellationToken);

    /// <summary>Distinct approved live non-blank values of the property for the assets (no order); an empty list fails (IN ()).</summary>
    Task<IReadOnlyList<string>> GetPotentialValuesAsync(IReadOnlyList<Guid> assetIds, Guid propertyId, CancellationToken cancellationToken);
    /// <summary>Distinct values of an Assets column for the system type; other system types produce no SELECT and fail.</summary>
    Task<IReadOnlyList<string>> GetPotentialSystemValuesAsync(IReadOnlyList<Guid> assetIds, int systemTypeId, CancellationToken cancellationToken);
    /// <summary>spr_AssetIds_GetByInstanceOfIds: the definitions of the instances (a NULL one fails).</summary>
    Task<IReadOnlyList<Guid>> GetInstanceOfIdsAsync(IReadOnlyList<Guid> assetIds, CancellationToken cancellationToken);
    /// <summary>AssetDal.Assets_Get(list): live approved assets by display name; an empty list fails.</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetAssetsAsync(IReadOnlyList<Guid> assetIds, CancellationToken cancellationToken);
    /// <summary>PickListValueDal.GetMatching: live values of the picklist among the ids, by display text; an empty list fails.</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetMatchingPickListValuesAsync(IReadOnlyList<Guid> valueIds, Guid pickListId, CancellationToken cancellationToken);

    // Assets_GetMatching variants
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> MatchByAssetTypeAsync(IReadOnlyList<Guid> assetIds, Guid assetTypeId, bool includeDescriptions, CancellationToken cancellationToken);
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetInstancesOfAsync(Guid instanceOfId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> MatchBySystemUserAsync(IReadOnlyList<Guid> assetIds, int systemTypeId, string? userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> MatchByValueAsync(IReadOnlyList<Guid> assetIds, Guid propertyId, string? value, bool includeDescriptions,
        bool useInstanceOfValues, bool exactMatch, CancellationToken cancellationToken);

    /// <summary>Assets_GetByPropertyValues: assets of exactly the type with any of the (property, value) pairs; raw names.</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetByPropertyValuesAsync(Guid assetTypeId, IReadOnlyList<KeyValuePair<Guid, string>> propertyValues,
        CancellationToken cancellationToken);

    /// <summary>
    /// PickListValueDal.PickListValues_Get: the child picklist's live values that depend on the parent value (soft-deleted
    /// dependencies still count), by [Order], as display text else value; a repeated value id fails.
    /// </summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetDependentPickListValuesAsync(Guid childPickListId, Guid parentPickListId, Guid parentValueId,
        CancellationToken cancellationToken);

    // AssetFilterClient_Get
    /// <summary>
    /// PropertyDal.Properties_GetNonFreeEntry: the live non-free-entry properties related to any of the types, by name
    /// (definition-level only for Definition requests).
    /// </summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetNonFreeEntryPropertiesAsync(IReadOnlyList<Guid> assetTypeIds, int requestType, CancellationToken cancellationToken);
    /// <summary>
    /// ChartDal.AssetFilterItems_Get: runs the batch and reads each result set as (display, raw, enabled) rows; when the
    /// first result set has no rows (or there is none) nothing is read.
    /// </summary>
    Task<IReadOnlyList<IReadOnlyList<FilterItemRow>>> GetFilterItemsAsync(string sql, CancellationToken cancellationToken);
}

/// <summary>An AssetFilterItem: the display text, the raw value (both "NULL" for NULL, en-US text otherwise), and whether it is in the selection.</summary>
public sealed record FilterItemRow(string Key, string Value, bool Enabled);
