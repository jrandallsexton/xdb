using System;

namespace Xdb.Api.Infrastructure.Data.Procedures;

/// <summary>spr_AssetType_Get (live types only).</summary>
public sealed class AssetTypeRow
{
    public string Name { get; init; } = string.Empty;
    public string? Pluralization { get; init; }
    public Guid? DefaultViewId { get; init; }
    public Guid? DefaultInstanceViewId { get; init; }
    public string? DefinitionLabel { get; init; }
    public string? InstanceLabel { get; init; }
    public Guid? ImageId { get; init; }
    public bool? AllowAssets { get; init; }
    public DateTime Created { get; init; }
    public Guid CreatedBy { get; init; }
}

/// <summary>An AssetTypesProperties row as the relation-list procedures return it.</summary>
public sealed class AssetTypePropertyRelationRow
{
    public Guid Id { get; init; }
    public Guid AssetTypeId { get; init; }
    public Guid PropertyId { get; init; }
    public bool IsInstance { get; init; }
    public bool IsInherited { get; init; }
    public bool IsInheritedValue { get; init; }
    public DateTime Created { get; init; }
    public Guid CreatedBy { get; init; }
}

/// <summary>
/// A Properties row with every column the original's property readers map (spr_Property_Get,
/// spr_Properties_GetByListOfPropertyIds, spr_PropertyList_GetByAssetTypeId). Precision is kept raw: the original
/// unboxed the tinyint as a short and failed for any non-NULL value.
/// </summary>
public sealed class PropertyDetailRow
{
    public Guid Id { get; init; }
    public byte? DataTypeId { get; init; }
    public byte? Precision { get; init; }
    public Guid? PickListId { get; init; }
    public Guid? RoleId { get; init; }
    public bool? IsOrdered { get; init; }
    public bool? AllowMultiValue { get; init; }
    public string? Name { get; init; }
    public string? DisplayValue { get; init; }
    public string? Description { get; init; }
    public Guid? AssetTypeId { get; init; }
    public bool? AssetTypeIsInstance { get; init; }
    public DateTime Created { get; init; }
    public DateTime? Approved { get; init; }
    public DateTime? LastModified { get; init; }
    public Guid CreatedBy { get; init; }
    public Guid? ApprovedBy { get; init; }
    public Guid? LastModifiedBy { get; init; }
    public DateTime? Deleted { get; init; }
    public Guid? DeletedBy { get; init; }
    public bool? IsSystem { get; init; }
    public int? SystemTypeId { get; init; }

    /// <summary>Only spr_PropertyList_GetByAssetTypeId returns these (from the relation).</summary>
    public bool? IsInherited { get; init; }
    public bool? IsInheritedValue { get; init; }
}

/// <summary>spr_PickList_Get (deleted lists included).</summary>
public sealed class PickListRow
{
    public string? Name { get; init; }
    public string? DisplayValue { get; init; }
    public string? Description { get; init; }
    public bool? IsMemberList { get; init; }
    public bool? AllowNewValues { get; init; }
    public string? ServiceUrl { get; init; }
    public string? ServiceMethod { get; init; }
    public string? ServiceUsername { get; init; }
    public string? ServicePassword { get; init; }
    public DateTime Created { get; init; }
    public Guid CreatedBy { get; init; }
    public DateTime? Approved { get; init; }
    public Guid? ApprovedBy { get; init; }
    public DateTime? LastModified { get; init; }
    public Guid? LastModifiedBy { get; init; }
    public DateTime? Deleted { get; init; }
    public Guid? DeletedBy { get; init; }
}

/// <summary>spr_PickListValueList_GetByPickListId (live values, by [Order]).</summary>
public sealed class PickListValueDetailRow
{
    public Guid Id { get; init; }
    public string? Value { get; init; }
    public string? DisplayValue { get; init; }
    public string? BGColor { get; init; }
    public int Order { get; init; }
    public DateTime Created { get; init; }
    public DateTime? Approved { get; init; }
    public DateTime? LastModified { get; init; }
    public Guid CreatedBy { get; init; }
    public Guid? ApprovedBy { get; init; }
    public Guid? LastModifiedBy { get; init; }
    public Guid? DeletedBy { get; init; }
}

public sealed class PickListDependencyRow
{
    public Guid Id { get; init; }
    public Guid ParentPickListId { get; init; }
    public Guid ParentPickListValueId { get; init; }
    public Guid ChildPickListId { get; init; }
    public Guid ChildPickListValueId { get; init; }
    public DateTime Created { get; init; }
    public Guid CreatedBy { get; init; }
}

public sealed class CurrencyValueRow
{
    public decimal Value { get; init; }
    public Guid CurrencySymbolId { get; init; }
    public string Symbol { get; init; } = string.Empty;
    public string SymbolDisplay { get; init; } = string.Empty;
}

/// <summary>spr_Image_Get with the data column omitted.</summary>
public sealed class ImageRow
{
    public byte ImageTypeId { get; init; }
    public byte Order { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public DateTime Created { get; init; }
    public DateTime? Approved { get; init; }
    public Guid CreatedBy { get; init; }
    public Guid? ApprovedBy { get; init; }
    public Guid? DeletedBy { get; init; }
}
