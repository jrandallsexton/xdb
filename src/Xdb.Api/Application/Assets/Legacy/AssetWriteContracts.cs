using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Xdb.Api.Application.Assets.Legacy;

/// <summary>
/// The original's Asset data contract (ordinal member order). Also the working object of asset saves, which is why it
/// carries the permission flags the save validates. AssetMembers is a relation list that always serializes empty here.
/// </summary>
[DataContract(Name = "Asset", Namespace = "")]
public sealed class AssetContract
{
    [DataMember] public bool AllowDelete { get; set; } = true;
    [DataMember] public bool AllowEdit { get; set; } = true;
    [DataMember] public DateTime? Approved { get; set; }
    [DataMember] public Guid? ApprovedBy { get; set; }
    [DataMember] public List<object> AssetMembers { get; set; } = [];
    [DataMember] public Guid AssetTypeId { get; set; }
    [DataMember] public DateTime Created { get; set; }
    [DataMember] public Guid CreatedBy { get; set; }
    [DataMember] public DateTime? Deleted { get; set; }
    [DataMember] public Guid? DeletedBy { get; set; }
    [DataMember] public string? Description { get; set; } = string.Empty;
    [DataMember] public string? DisplayValue { get; set; } = string.Empty;
    [DataMember] public Guid Id { get; set; }
    [DataMember] public Guid? InstanceOfId { get; set; }
    [DataMember] public bool IsDirty { get; set; }
    [DataMember] public bool IsInstance { get; set; }
    [DataMember] public bool IsNew { get; set; }
    [DataMember] public DateTime? LastModified { get; set; }
    [DataMember] public Guid? LastModifiedBy { get; set; }
    [DataMember] public string? Name { get; set; }
}

/// <summary>
/// The original's Bookmark: a plain class (no data contract), so every public property is serialized, ordinal order,
/// with CLR defaults for columns the reader left unset.
/// </summary>
[DataContract(Name = "Bookmark", Namespace = "")]
public sealed class BookmarkContract
{
    [DataMember] public string? Asset { get; set; }
    [DataMember] public Guid AssetId { get; set; }
    [DataMember] public string? AssetType { get; set; }
    [DataMember] public Guid AssetTypeId { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public Guid ReportId { get; set; }
    [DataMember] public string? ReportName { get; set; }
    [DataMember] public bool ShowOnHome { get; set; }
    [DataMember] public Guid ViewId { get; set; }
}

/// <summary>
/// A property value inside a submittal (the original's PropertyValue as the save path uses it). IsNew and IsDirty are
/// carried between the save's two passes: a value skipped as unchanged in the first pass stays new.
/// </summary>
public sealed class SubmittalValue
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public Guid AssetId { get; set; }
    public Guid? SubmittalGroupId { get; set; }
    public string? Value { get; set; } = string.Empty;
    public int? Order { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Approved { get; set; }
    public DateTime? Deleted { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public Guid? DeletedBy { get; set; }
    public DateTime? Rejected { get; set; }
    public Guid? RejectedBy { get; set; }
    public bool IsNew { get; set; } = true;
    public bool IsDirty { get; set; } = true;
}
