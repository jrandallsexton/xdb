using System;

namespace Xdb.Api.Infrastructure.Data.Procedures;

/// <summary>spr_View_Get (deleted views included).</summary>
public sealed class ViewRow
{
    public string? Name { get; init; }
    public string? DisplayValue { get; init; }
    public string? Description { get; init; }
    public bool? IsInstance { get; init; }
    public Guid AssetTypeId { get; init; }
    public string? DriverCaption { get; init; }
    public string? NewItemCaption { get; init; }
    public string? ConfirmationLabel { get; init; }
    public bool? IsReadOnly { get; init; }
    public bool? IsStandAlone { get; init; }
    public bool? IsCreation { get; init; }
    public string? FilterLogic { get; init; }
    public DateTime Created { get; init; }
    public DateTime? LastModified { get; init; }
    public DateTime? Deleted { get; init; }
    public Guid CreatedBy { get; init; }
    public Guid? LastModifiedBy { get; init; }
    public Guid? DeletedBy { get; init; }
}

/// <summary>spr_ViewPropertyGroupRelationList_GetByViewId.</summary>
public sealed class ViewPropertyGroupRelationRow
{
    public Guid Id { get; init; }
    public Guid ViewId { get; init; }
    public Guid PropertyGroupId { get; init; }
    public int Order { get; init; }
    public DateTime Created { get; init; }
    public Guid CreatedBy { get; init; }
}

/// <summary>A property group as spr_PropertyGroupList_GetByViewId or spr_PropertyGroup_Get returns it.</summary>
public sealed class PropertyGroupRow
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public string? DisplayValue { get; init; }
    public Guid? AssetTypeId { get; init; }
    public bool? IsInstance { get; init; }
    public DateTime? Created { get; init; }
    public DateTime? LastModified { get; init; }
    public Guid? CreatedBy { get; init; }
    public Guid? LastModifiedBy { get; init; }
    public DateTime? Deleted { get; init; }
    public Guid? DeletedBy { get; init; }
}

/// <summary>spr_PropertyGroupPropertyRelationList_GetByPropertyGroupId.</summary>
public sealed class PropertyGroupPropertyRelationRow
{
    public Guid Id { get; init; }
    public Guid PropertyGroupId { get; init; }
    public Guid PropertyId { get; init; }
    public int Order { get; init; }
    public bool? IsRequired { get; init; }
    public bool? IsReadOnly { get; init; }
    public string? DefaultValue { get; init; }
    public DateTime? Created { get; init; }
    public Guid CreatedBy { get; init; }
    public Guid? DeletedBy { get; init; }
}

/// <summary>spr_PropertyList_GetByPropertyGroupId: property columns plus the group relation's IsRequired/IsReadOnly/DefaultValue.</summary>
public sealed class GroupPropertyRow
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
    public bool? IsRequired { get; init; }
    public bool? IsReadOnly { get; init; }
    public string? DefaultValue { get; init; }
}

/// <summary>A view's saved filter (ViewFilters joined to Properties and Operators).</summary>
public sealed class ViewFilterRow
{
    public Guid Id { get; init; }
    public Guid PropertyId { get; init; }
    public string Property { get; init; } = string.Empty;
    public int OperatorId { get; init; }
    public string Operator { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public int Order { get; init; }
}

public sealed class TriggerRow
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string IsActive { get; init; } = string.Empty;
    public string TypeId { get; init; } = string.Empty;
}

public sealed class TriggerRestrictionRow
{
    public Guid Id { get; init; }
    public Guid PropertyId { get; init; }
    public Guid RestrictedPropertyId { get; init; }
    public Guid? SourcePropertyId { get; init; }
}

public sealed class ProcessRow
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? DisplayValue { get; init; }
    public Guid? AssetTypeId { get; init; }
}

public sealed class ProcessStepRow
{
    public Guid Id { get; init; }
    public int Order { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid? ViewId { get; init; }
    public bool IsInstance { get; init; }
    public bool IsCreation { get; init; }
    public bool IsEdit { get; init; }
    public bool ShowAssetTypeSelector { get; init; }
    public string? AssetNameLabel { get; init; }
}

public sealed class ProcessStepFilterRow
{
    public Guid Id { get; init; }
    public Guid PropertyId { get; init; }
    public int Order { get; init; }
}

public sealed class HelpSectionRow
{
    public string Title { get; init; } = string.Empty;
    public Guid ImageId { get; init; }
    public int Order { get; init; }
    public string Content { get; init; } = string.Empty;
}

public sealed class RoleViewRelationRow
{
    public Guid Id { get; init; }
    public Guid RoleId { get; init; }
    public Guid ViewId { get; init; }
    public bool? IsAutoApprove { get; init; }
    public DateTime? Created { get; init; }
    public Guid CreatedBy { get; init; }
    public Guid? DeletedBy { get; init; }
}

/// <summary>The first result set of spr_MemberView_Get.</summary>
public sealed class MemberViewRow
{
    public Guid MemberId { get; init; }
    public Guid ViewId { get; init; }
    public Guid AssetTypeId { get; init; }
    public bool IsInstance { get; init; }
    public string Name { get; init; } = string.Empty;
    public string FilterExpression { get; init; } = string.Empty;
    public string? FilterString { get; init; }
}
