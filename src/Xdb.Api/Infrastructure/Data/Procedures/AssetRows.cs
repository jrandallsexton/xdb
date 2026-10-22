using System;

namespace Xdb.Api.Infrastructure.Data.Procedures;

/// <summary>spr_Asset_Get (deleted assets included).</summary>
public sealed class AssetRow
{
    public Guid AssetTypeId { get; init; }
    public string? Name { get; init; }
    public string? DisplayValue { get; init; }
    public string? Description { get; init; }
    public bool? IsInstance { get; init; }
    public Guid? InstanceOfId { get; init; }
    public DateTime Created { get; init; }
    public DateTime? Approved { get; init; }
    public DateTime? LastModified { get; init; }
    public Guid CreatedBy { get; init; }
    public Guid? ApprovedBy { get; init; }
    public Guid? LastModifiedBy { get; init; }
    public DateTime? Deleted { get; init; }
    public Guid? DeletedBy { get; init; }
}

/// <summary>A property value row (spr_PropertyValueList_GetForAnAsset[IncludeDeleted], spr_PropertyValue_Get).</summary>
public sealed class PropertyValueRow
{
    public Guid Id { get; init; }
    public Guid PropertyId { get; init; }
    public Guid AssetId { get; init; }
    public string? Value { get; init; }
    public int? Order { get; init; }
    public DateTime Created { get; init; }
    public Guid CreatedBy { get; init; }
    /// <summary>The lookup function's text (list procedures only).</summary>
    public string? DisplayValue { get; init; }
}

/// <summary>spr_Member_Get as MemberDal.Get maps it (missing names are "").</summary>
public sealed record MemberRow(string UserId, string LastName, string FirstName);

public sealed record DocumentRow(string? Name, string? Title);

public sealed record UrlRow(string? Name, string? Url);
