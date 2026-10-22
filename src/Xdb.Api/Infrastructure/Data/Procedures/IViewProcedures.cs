using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xdb.Api.Infrastructure.Data.Procedures;

/// <summary>
/// The original's reads for views, property groups, processes, help sections and role/view relations, with its SQL
/// text and mapping semantics (see <see cref="IMetadataProcedures"/>). Pair lists keep SQL row order.
/// </summary>
public interface IViewProcedures
{
    // Views
    Task<ViewRow?> GetViewAsync(Guid viewId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ViewPropertyGroupRelationRow>> GetViewPropertyGroupRelationsAsync(Guid viewId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PropertyGroupRow>> GetViewPropertyGroupsAsync(Guid viewId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PropertyGroupPropertyRelationRow>> GetPropertyGroupRelationsAsync(Guid propertyGroupId, CancellationToken cancellationToken);
    Task<IReadOnlyList<GroupPropertyRow>> GetPropertyGroupPropertiesAsync(Guid propertyGroupId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ViewFilterRow>> GetViewFiltersAsync(Guid viewId, CancellationToken cancellationToken);
    /// <summary>Active, live triggers linked to the view (no ORDER BY).</summary>
    Task<IReadOnlyList<Guid>> GetViewTriggerIdsAsync(Guid viewId, CancellationToken cancellationToken);
    Task<TriggerRow?> GetTriggerAsync(Guid triggerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TriggerRestrictionRow>> GetTriggerRestrictionsAsync(Guid triggerId, CancellationToken cancellationToken);
    /// <summary>The Create permission count through RolesMembers (MemberLayer.MemberCanAddAssets without the admin check).</summary>
    Task<int> CountCreatePermissionsAsync(Guid userId, Guid assetTypeId, CancellationToken cancellationToken);
    /// <summary>The member's saved quick filters for the view, by name.</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetMemberFiltersAsync(Guid viewId, Guid memberId, CancellationToken cancellationToken);
    /// <summary>The first cached client view row for (view, user), or null.</summary>
    Task<byte[]?> GetViewClientCacheAsync(Guid viewId, Guid userId, CancellationToken cancellationToken);
    Task InsertViewClientCacheAsync(Guid viewId, Guid userId, byte[] data, CancellationToken cancellationToken);
    /// <summary>Views.LastModified round-tripped through en-US text, as ExecuteScalarDateTime did; null when absent or NULL.</summary>
    Task<DateTime?> GetViewLastModifiedAsync(Guid viewId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetViewsByAssetTypeAsync(Guid assetTypeId, int requestType, bool includeNonStandAlone, CancellationToken cancellationToken);
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetViewsByPropertyGroupAsync(Guid propertyGroupId, CancellationToken cancellationToken);
    /// <summary>Views as "Name (DisplayValue)", through the original's guarded dictionary reader.</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetViewsDictionaryAsync(bool includeDeleted, bool includeNonStandAlone, CancellationToken cancellationToken);
    Task<MemberViewRow?> GetMemberViewAsync(Guid memberViewId, CancellationToken cancellationToken);
    Task<bool> IsLiveViewAsync(Guid viewId, CancellationToken cancellationToken);
    Task<Guid> GetViewAssetTypeIdAsync(Guid viewId, CancellationToken cancellationToken);
    Task<bool> IsLiveInstanceViewAsync(Guid viewId, CancellationToken cancellationToken);
    /// <summary>spr_ViewDictionary_GetByUserId through the unguarded dictionary reader.</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetUserViewsAsync(Guid userId, CancellationToken cancellationToken);
    /// <summary>spr_ViewGroups_GetByUserId through the unguarded dictionary reader (asset type id, pluralization).</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetUserViewGroupsAsync(Guid userId, CancellationToken cancellationToken);

    // Processes
    Task<ProcessRow?> GetProcessAsync(Guid processId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProcessStepRow>> GetProcessStepsAsync(Guid processId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProcessStepFilterRow>> GetProcessStepFiltersAsync(Guid processStepId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetProcessesDictionaryAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetProcessesByAssetTypeAsync(Guid assetTypeId, bool isInstance, CancellationToken cancellationToken);
    /// <summary>The step's AssetNameLabel; "" when there is no step. A NULL label fails.</summary>
    Task<string> GetProcessStepAssetNameLabelAsync(Guid processId, int stepIndex, CancellationToken cancellationToken);
    /// <summary>One of the step's bit columns (IsCreation, IsEdit, IsInstance, ShowAssetTypeSelector); false when there is no step.</summary>
    Task<bool> GetProcessStepFlagAsync(Guid processId, int stepIndex, string column, CancellationToken cancellationToken);
    Task<Guid> GetProcessStepIdAsync(Guid processId, int stepIndex, CancellationToken cancellationToken);
    Task<IReadOnlyList<KeyValuePair<int, Guid>>> GetProcessStepFilterPropertiesAsync(Guid processStepId, CancellationToken cancellationToken);
    /// <summary>Picklist values used by a property (exact original SQL: its implicit conversion can fail for some properties).</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetUsedPickListValuesAsync(Guid propertyId, CancellationToken cancellationToken);
    /// <summary>Assets with an approved value equal to the literal (the value is embedded in the SQL text, as in the original).</summary>
    Task<IReadOnlyList<Guid>> GetAssetsByPropertyValueAsync(Guid propertyId, string? value, CancellationToken cancellationToken);
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetUsedPickListValuesForAssetsAsync(IReadOnlyList<Guid> assetIds, Guid propertyId, CancellationToken cancellationToken);

    // Help
    Task<HelpSectionRow?> GetHelpSectionAsync(Guid helpSectionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetHelpSectionIdsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetHelpSectionsDictionaryAsync(CancellationToken cancellationToken);

    // Property groups
    Task<PropertyGroupRow?> GetPropertyGroupAsync(Guid propertyGroupId, CancellationToken cancellationToken);
    /// <summary>Groups as "Name (DisplayValue)" by name, rows with NULLs skipped; for one asset type, or all.</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetPropertyGroupsDictionaryAsync(bool includeDeleted, Guid? assetTypeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetPropertyGroupsByPropertyAsync(Guid propertyId, CancellationToken cancellationToken);

    // Roles
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetRolesByViewAsync(Guid viewId, CancellationToken cancellationToken);
    /// <summary>The member's active roles (no DISTINCT: a duplicate membership fails, as in the original).</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetMemberRolesAsync(Guid memberId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RoleViewRelationRow>> GetRoleViewRelationsAsync(Guid roleId, CancellationToken cancellationToken);
}
