using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Newtonsoft.Json;

namespace Xdb.Api.Application.Activity.Legacy;

/// <summary>A submittal as Submittal_Get returns it (Core PropertyValueSubmittal; the display members are never loaded).</summary>
[DataContract(Name = "PropertyValueSubmittal", Namespace = "")]
public sealed class SubmittalContract
{
    [DataMember] public Guid AssetId { get; set; }
    [DataMember] public string? AssetName { get; set; }
    [DataMember] public DateTime Created { get; set; }
    [DataMember] public Guid CreatedBy { get; set; }
    [DataMember] public string? CreatedByDisplay { get; set; }
    [DataMember] public DateTime? Deleted { get; set; }
    [DataMember] public Guid? DeletedBy { get; set; }
    [DataMember] public string? DeletedByDisplay { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public bool IsDirty { get; set; }
    [DataMember] public bool IsNew { get; set; }
    [DataMember] public string? Notes { get; set; } = string.Empty;
    [DataMember] public string? PickListValue { get; set; }
    [DataMember] public List<SubmittalValueContract> PropertyValues { get; set; } = [];
    [DataMember] public DateTime? Reviewed { get; set; }
    [DataMember] public Guid? ReviewedBy { get; set; }
    [DataMember] public string? ReviewedByDisplay { get; set; }
}

/// <summary>A value of a submittal (Core PropertyValue with every data member).</summary>
[DataContract(Name = "PropertyValue", Namespace = "")]
public sealed class SubmittalValueContract
{
    [DataMember] public DateTime? Approved { get; set; }
    [DataMember] public Guid? ApprovedBy { get; set; }
    [DataMember] public Guid AssetId { get; set; }
    [DataMember] public DateTime Created { get; set; }
    [DataMember] public Guid CreatedBy { get; set; }
    [DataMember] public DateTime? Deleted { get; set; }
    [DataMember] public Guid? DeletedBy { get; set; }
    [DataMember] public string? DisplayValue { get; set; }
    [DataMember] public string? DisplayValueHtml { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public bool IsDirty { get; set; }
    [DataMember] public bool IsNew { get; set; }
    [DataMember] public int? Order { get; set; }
    [DataMember] public string? Property { get; set; }
    [DataMember] public Guid PropertyId { get; set; }
    [DataMember] public DateTime? Rejected { get; set; }
    [DataMember] public Guid? RejectedBy { get; set; }
    [DataMember] public Guid? SubmittalGroupId { get; set; }
    [DataMember] public string? Value { get; set; }
}

/// <summary>The home page's submittal summary (UI SubmittalSummaryClient): values keyed by property display name.</summary>
[DataContract(Name = "SubmittalSummaryClient", Namespace = "")]
public sealed class SubmittalSummaryContract
{
    [DataMember(Name = "assetId")] public string? AssetId { get; set; }
    [DataMember(Name = "assetName")] public string? AssetName { get; set; }
    [DataMember(Name = "assetTypeId")] public string? AssetTypeId { get; set; }
    [DataMember(Name = "created")] public string? Created { get; set; }
    [DataMember(Name = "values")] public Dictionary<string, KeyValuePair<string, string>> Values { get; set; } = [];
    [DataMember(Name = "viewId")] public string? ViewId { get; set; }
}

/// <summary>A trigger (Core Trigger); Type is the enum's number.</summary>
[DataContract(Name = "Trigger", Namespace = "")]
public sealed class TriggerContract
{
    [DataMember] public List<EmptyContract> AssetTypes { get; set; } = [];
    [DataMember] public List<EmptyContract> Assets { get; set; } = [];
    [DataMember] public DateTime Created { get; set; }
    [DataMember] public Guid CreatedBy { get; set; }
    [DataMember] public DateTime? Deleted { get; set; }
    [DataMember] public Guid? DeletedBy { get; set; }
    [DataMember] public string? Description { get; set; }
    [DataMember] public string? FilterLogic { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public bool IsActive { get; set; }
    [DataMember] public bool IsDirty { get; set; }
    [DataMember] public bool IsNew { get; set; }
    [DataMember] public List<Guid> MemberIds { get; set; } = [];
    [DataMember] public string? Name { get; set; }
    [DataMember] public List<TriggerPropertyContract> Properties { get; set; } = [];
    [DataMember] public List<TriggerRestrictionContract> Restrictions { get; set; } = [];
    [DataMember] public List<Guid> RoleIds { get; set; } = [];
    [DataMember] public List<TriggerTargetPropertyContract> TargetProperties { get; set; } = [];
    [DataMember] public int Type { get; set; }
}

/// <summary>TriggerAssetType and TriggerAsset: data contracts without data members, so each row serializes as {}.</summary>
[DataContract(Name = "TriggerAssetType", Namespace = "")]
public sealed class EmptyContract;

[DataContract(Name = "TriggerProperty", Namespace = "")]
public sealed class TriggerPropertyContract
{
    [DataMember] public Guid Id { get; set; }
    [DataMember] public Guid? OperatorId { get; set; }
    [DataMember] public Guid PropertyId { get; set; }
    [DataMember] public string? Threshold { get; set; } = string.Empty;
    [DataMember] public Guid TriggerId { get; set; }
    [DataMember] public string? Value { get; set; } = string.Empty;
}

[DataContract(Name = "TriggerRestriction", Namespace = "")]
public sealed class TriggerRestrictionContract
{
    [DataMember] public Guid Id { get; set; }
    [DataMember] public Guid PropertyId { get; set; }
    [DataMember] public Guid RestrictedPropertyId { get; set; }
    [DataMember] public Guid SourcePropertyId { get; set; }
    [DataMember] public Guid TriggerId { get; set; }
}

[DataContract(Name = "TriggerTargetProperty", Namespace = "")]
public sealed class TriggerTargetPropertyContract
{
    [DataMember] public string? ChangeToValue { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public Guid TargetPropertyId { get; set; }
    [DataMember] public Guid TriggerId { get; set; }
}

/// <summary>A search hit (UI SearchResult), serialized by Json.NET in declaration order.</summary>
public sealed class SearchResultContract
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Category { get; set; }
    public int Index { get; set; }
    public string? Url { get; set; }
    public int VisitCount { get; set; }
    public DateTime? LastVisit { get; set; }
    public Guid AssetId { get; set; }
    public string? AssetTypeId { get; set; }
}

/// <summary>A tag suggestion (UI TagClient), serialized by Json.NET in declaration order.</summary>
public sealed class TagContract
{
    [JsonProperty("id")] public int Id { get; set; }
    [JsonProperty("name")] public string? Name { get; set; }
    [JsonProperty("img")] public string? Img { get; set; }
    [JsonProperty("type")] public string? Type { get; set; }
}

/// <summary>A TriggerCreate body (UI TriggerClient) as bound: filterLogic is not a data member, so it is never read.</summary>
public sealed record TriggerBody(string? Id, string? Name, string? Description, int Type, List<string?>? AssetTypeIds, bool Specific);
