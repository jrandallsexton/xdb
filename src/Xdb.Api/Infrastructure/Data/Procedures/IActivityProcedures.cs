using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xdb.Api.Infrastructure.Data.Procedures;

/// <summary>An unreviewed submittal as spr_PropertyValueSubmittalList_GetDisplay lists it (the columns the list uses).</summary>
public sealed record SubmittalListRow(Guid Id, string? AssetName, DateTime Created, string? CreatedByDisplay);

/// <summary>A submittal row (spr_PropertyValueSubmittal_Get); columns the reader skips when NULL are nullable.</summary>
public sealed record SubmittalRow(Guid? Id, Guid? AssetId, string? Notes, DateTime? Created, DateTime? Reviewed, DateTime? Deleted, Guid? CreatedBy,
    Guid? ReviewedBy, Guid? DeletedBy);

/// <summary>A value row of spr_PropertyValueList_GetBySubmittalGroupId (every column the original loader read).</summary>
public sealed record SubmittalValueRow(Guid Id, Guid PropertyId, Guid AssetId, Guid? SubmittalGroupId, string? Value, int? Order, DateTime Created,
    DateTime? Approved, DateTime? Rejected, DateTime? Deleted, Guid CreatedBy, Guid? ApprovedBy, Guid? RejectedBy, Guid? DeletedBy);

/// <summary>One summary line (spr_PropertyValueSubmittal_GetSummary): property, previous and new display texts.</summary>
public sealed record SubmittalSummaryRow(string Property, string Previous, string New);

/// <summary>A home-feed submittal (SubmittalActivityHelper).</summary>
public sealed record SubmittalActivityRow(Guid AssetId, Guid AssetTypeId, string AssetCode, string Role, Guid SubmittalId, string Narrative, DateTime Created);

/// <summary>A role the member can edit an asset type through (spr_Roles_GetEditable).</summary>
public sealed record EditableRoleRow(Guid RoleId, string RoleName, Guid AssetTypeId);

public sealed record TriggerDetailRow(string Name, string Description, string IsActive, string TypeId, DateTime Created, Guid CreatedBy, DateTime? Deleted, Guid? DeletedBy);

public sealed record TriggerPropertyRow(Guid Id, Guid? PropertyId, string? Value, Guid? OperatorId, string? Threshold);

public sealed record TriggerRestrictionDetailRow(Guid Id, Guid PropertyId, Guid RestrictedPropertyId, Guid? SourcePropertyId);

public sealed record TriggerTargetPropertyRow(Guid Id, Guid TargetPropertyId, string ChangeToValue);

/// <summary>A search hit row: id, title, asset type, content (description or value), instance flag.</summary>
public sealed record SearchRow(Guid AssetId, string Title, Guid AssetTypeId, string? Content, bool IsInstance);

/// <summary>A spr_Search_PropertyValue_Asset row.</summary>
public sealed record SearchValueAssetRow(Guid AssetId, string AssetName, string Property, string AssetValue, Guid AssetTypeId, string? Description, bool IsInstance,
    Guid? InstanceOfId);

/// <summary>The original's submittal, trigger, activity-feed and search data access (spec M7-S), with its SQL text.</summary>
public interface IActivityProcedures
{
    // Submittals
    Task<IReadOnlyList<SubmittalListRow>> GetUnreviewedSubmittalsAsync(CancellationToken cancellationToken);
    Task<SubmittalRow?> GetSubmittalAsync(Guid submittalId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SubmittalValueRow>> GetSubmittalValuesAsync(Guid submittalId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SubmittalSummaryRow>> GetSubmittalSummaryAsync(Guid submittalId, CancellationToken cancellationToken);
    /// <summary>The submittal's Created round-tripped through en-US text; null when absent.</summary>
    Task<DateTime?> GetSubmittalCreatedAsync(Guid submittalId, CancellationToken cancellationToken);
    /// <summary>spr_PropertyValueSubmittal_GetAssetInfo: the asset id and display text; null when the asset is missing.</summary>
    Task<(Guid AssetId, string? Asset)?> GetSubmittalAssetInfoAsync(Guid submittalId, CancellationToken cancellationToken);

    // Activity feeds
    Task<IReadOnlyList<SubmittalActivityRow>> GetActivityAsync(string sql, CancellationToken cancellationToken);
    Task<IReadOnlyList<EditableRoleRow>> GetEditableRolesAsync(Guid memberId, CancellationToken cancellationToken);
    /// <summary>spr_AssetTypes_ByMember_Get, read to the end (its rows only feed a statement that never compiles).</summary>
    Task ReadAssetTypesForMemberAsync(Guid memberId, CancellationToken cancellationToken);
    Task RunQueryAsync(string sql, CancellationToken cancellationToken);

    // Triggers
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetTriggerDictionaryAsync(Guid memberId, CancellationToken cancellationToken);
    Task<TriggerDetailRow?> GetTriggerAsync(Guid triggerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TriggerRestrictionDetailRow>> GetTriggerRestrictionsAsync(Guid triggerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TriggerPropertyRow>> GetTriggerPropertiesAsync(Guid triggerId, CancellationToken cancellationToken);
    /// <summary>TriggersAssetTypes rows (the client sees one empty object per row).</summary>
    Task<int> CountTriggerAssetTypesAsync(Guid triggerId, CancellationToken cancellationToken);
    Task<int> CountTriggerAssetsAsync(Guid triggerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetTriggerMembersAsync(Guid triggerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetTriggerRolesAsync(Guid triggerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TriggerTargetPropertyRow>> GetTriggerTargetPropertiesAsync(Guid triggerId, CancellationToken cancellationToken);
    /// <summary>
    /// TriggerDal.Save's spr_Trigger_Save call with the original's parameter names: it sends @TriggerTypeId, which the
    /// procedure does not declare, and never its required @TypeId, so the server rejects every call (Msg 201).
    /// </summary>
    Task SaveTriggerAsync(Guid id, string? name, string? description, bool isActive, int typeId, DateTime created, Guid createdBy, CancellationToken cancellationToken);

    // Search
    Task<IReadOnlyList<SearchRow>> SearchAssetsAsync(string phrase, CancellationToken cancellationToken);
    Task<IReadOnlyList<SearchRow>> SearchPropertyValueTextAsync(string phrase, CancellationToken cancellationToken);
    Task<IReadOnlyList<SearchValueAssetRow>> SearchPropertyValueAssetsAsync(Guid memberId, string assetIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<SearchRow>> SearchUsersAsync(string phrase, CancellationToken cancellationToken);
    /// <summary>spr_SearchTag_Asset / spr_SearchTag_User: the Name column of each row (a NULL name fails).</summary>
    Task<IReadOnlyList<string>> SearchTagAsync(string procedure, string phrase, CancellationToken cancellationToken);
    Task InsertSearchAsync(string phrase, Guid userId, CancellationToken cancellationToken);
}
