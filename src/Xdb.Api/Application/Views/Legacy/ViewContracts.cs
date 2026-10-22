using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Xdb.Api.Application.Metadata.Legacy;

namespace Xdb.Api.Application.Views.Legacy;

// The original's view-related data contracts, member for member (DataContractJsonSerializer writes ordinal order).

[DataContract(Name = "PropertyTrigger", Namespace = "")]
public sealed class PropertyTriggerContract
{
    [DataMember] public Guid ParentPropertyId { get; set; }
    /// <summary>The empty Guid when the restriction has no source property.</summary>
    [DataMember] public Guid SourcePropertyId { get; set; }
    [DataMember] public Guid TargetPropertyId { get; set; }
    /// <summary>Restriction (0); the only type views produce.</summary>
    [DataMember] public int TriggerType { get; set; }
}

[DataContract(Name = "Filter", Namespace = "")]
public sealed class FilterContract
{
    [DataMember] public string? DisplayValue { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public string Operator { get; set; } = string.Empty;
    [DataMember] public int OperatorId { get; set; }
    [DataMember] public int Order { get; set; }
    [DataMember] public string Property { get; set; } = string.Empty;
    [DataMember] public Guid PropertyId { get; set; }
    [DataMember] public string Value { get; set; } = string.Empty;
}

[DataContract(Name = "ViewPropertyGroupRelation", Namespace = "")]
public sealed class ViewPropertyGroupRelationContract
{
    [DataMember] public DateTime Created { get; set; }
    [DataMember] public Guid CreatedBy { get; set; }
    [DataMember] public DateTime? Deleted { get; set; }
    [DataMember] public Guid? DeletedBy { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public bool IsDirty { get; set; }
    [DataMember] public bool IsNew { get; set; }
    [DataMember] public int Order { get; set; }
    [DataMember] public Guid PropertyGroupId { get; set; }
    [DataMember] public Guid ViewId { get; set; }
}

[DataContract(Name = "PropertyGroupPropertyRelation", Namespace = "")]
public sealed class PropertyGroupPropertyRelationContract
{
    [DataMember] public DateTime Created { get; set; }
    [DataMember] public Guid CreatedBy { get; set; }
    [DataMember] public string DefaultValue { get; set; } = string.Empty;
    [DataMember] public DateTime? Deleted { get; set; }
    [DataMember] public Guid? DeletedBy { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public bool IsDirty { get; set; }
    [DataMember] public bool IsNew { get; set; }
    [DataMember] public bool IsReadOnly { get; set; }
    [DataMember] public bool IsRequired { get; set; }
    [DataMember] public int Order { get; set; }
    [DataMember] public Guid PropertyGroupId { get; set; }
    [DataMember] public Guid PropertyId { get; set; }
}

[DataContract(Name = "PropertyGroup", Namespace = "")]
public sealed class PropertyGroupContract
{
    [DataMember] public Guid AssetTypeId { get; set; }
    [DataMember] public DateTime Created { get; set; }
    [DataMember] public Guid CreatedBy { get; set; }
    [DataMember] public DateTime? Deleted { get; set; }
    [DataMember] public Guid? DeletedBy { get; set; }
    [DataMember] public string DisplayValue { get; set; } = string.Empty;
    [DataMember] public Guid Id { get; set; }
    [DataMember] public bool IsDirty { get; set; }
    [DataMember] public bool IsInstance { get; set; }
    [DataMember] public bool IsNew { get; set; }
    [DataMember] public DateTime? LastModified { get; set; }
    [DataMember] public Guid? LastModifiedBy { get; set; }
    [DataMember] public string Name { get; set; } = string.Empty;
    [DataMember] public List<PropertyContract> Properties { get; set; } = [];
    [DataMember] public List<PropertyGroupPropertyRelationContract> PropertyMembers { get; set; } = [];
}

[DataContract(Name = "View", Namespace = "")]
public sealed class ViewContract
{
    [DataMember] public bool AllowCloning { get; set; } = true;
    [DataMember] public bool AllowNewValues { get; set; }
    [DataMember] public Guid AssetTypeId { get; set; }
    [DataMember] public string ConfirmationLabel { get; set; } = string.Empty;
    [DataMember] public DateTime Created { get; set; }
    [DataMember] public Guid CreatedBy { get; set; }
    [DataMember] public DateTime? Deleted { get; set; }
    [DataMember] public Guid? DeletedBy { get; set; }
    [DataMember] public string Description { get; set; } = string.Empty;
    [DataMember] public string DisplayValue { get; set; } = string.Empty;
    [DataMember] public string DriverCaption { get; set; } = string.Empty;
    [DataMember] public string? FilterLogic { get; set; }
    [DataMember] public List<FilterContract> Filters { get; set; } = [];
    [DataMember] public Guid Id { get; set; }
    [DataMember] public bool IsAutoApprove { get; set; }
    [DataMember] public bool IsCreation { get; set; }
    [DataMember] public bool IsDirty { get; set; }
    [DataMember] public bool IsInstance { get; set; }
    [DataMember] public bool IsNew { get; set; }
    [DataMember] public bool IsReadOnly { get; set; }
    [DataMember] public bool IsStandAlone { get; set; } = true;
    [DataMember] public DateTime? LastModified { get; set; }
    [DataMember] public Guid? LastModifiedBy { get; set; }
    [DataMember] public string Name { get; set; } = string.Empty;
    [DataMember] public string NewItemCaption { get; set; } = string.Empty;
    [DataMember] public List<ViewPropertyGroupRelationContract> PropertyGroupMembers { get; set; } = [];
    [DataMember] public List<PropertyGroupContract> PropertyGroups { get; set; } = [];
}

// The original's process client objects were plain classes (no data contract): every public property, lowercase
// names, ordinal order. Explicit lowercase member names reproduce that.

[DataContract(Name = "ProcessStepFilterClient", Namespace = "")]
public sealed class ProcessStepFilterClientContract
{
    [DataMember(Name = "id")] public string? Id { get; set; }
    [DataMember(Name = "order")] public int Order { get; set; }
    [DataMember(Name = "propertyId")] public string? PropertyId { get; set; }
}

[DataContract(Name = "ProcessStepClient", Namespace = "")]
public sealed class ProcessStepClientContract
{
    [DataMember(Name = "assetNameLabel")] public string? AssetNameLabel { get; set; }
    [DataMember(Name = "filters")] public List<ProcessStepFilterClientContract> Filters { get; set; } = [];
    [DataMember(Name = "id")] public string? Id { get; set; }
    [DataMember(Name = "isCreation")] public bool IsCreation { get; set; }
    [DataMember(Name = "isEdit")] public bool IsEdit { get; set; }
    [DataMember(Name = "isInstance")] public bool IsInstance { get; set; }
    [DataMember(Name = "name")] public string? Name { get; set; }
    [DataMember(Name = "order")] public int Order { get; set; }
    [DataMember(Name = "showAssetTypeSelector")] public bool ShowAssetTypeSelector { get; set; }
    [DataMember(Name = "viewId")] public string? ViewId { get; set; }
}

[DataContract(Name = "ProcessClient", Namespace = "")]
public sealed class ProcessClientContract
{
    [DataMember(Name = "assetTypeId")] public string? AssetTypeId { get; set; }
    [DataMember(Name = "id")] public string? Id { get; set; }
    [DataMember(Name = "name")] public string? Name { get; set; }
    [DataMember(Name = "steps")] public List<ProcessStepClientContract> Steps { get; set; } = [];
}

[DataContract(Name = "HelpSection", Namespace = "")]
public sealed class HelpSectionContract
{
    [DataMember] public string Content { get; set; } = string.Empty;
    [DataMember] public Guid Id { get; set; }
    [DataMember] public Guid ImageId { get; set; }
    [DataMember] public int Order { get; set; }
    [DataMember] public string Title { get; set; } = string.Empty;
}

[DataContract(Name = "RoleViewRelation", Namespace = "")]
public sealed class RoleViewRelationContract
{
    [DataMember] public DateTime Created { get; set; }
    [DataMember] public Guid CreatedBy { get; set; }
    [DataMember] public DateTime? Deleted { get; set; }
    [DataMember] public Guid? DeletedBy { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public bool IsAutoApprove { get; set; }
    [DataMember] public bool IsDirty { get; set; }
    [DataMember] public bool IsNew { get; set; }
    [DataMember] public Guid RoleId { get; set; }
    [DataMember] public Guid ViewId { get; set; }
}

[DataContract(Name = "PropertyClient", Namespace = "")]
public sealed class PropertyClientContract
{
    [DataMember] public bool AllowMultiValue { get; set; }
    [DataMember] public bool AllowNewValues { get; set; }
    [DataMember] public Guid? AssetTypeId { get; set; }
    [DataMember] public bool? AssetTypeIsInstance { get; set; }
    [DataMember] public int DataType { get; set; }
    [DataMember] public string DefaultValue { get; set; } = string.Empty;
    [DataMember] public string Description { get; set; } = string.Empty;
    [DataMember] public Guid Id { get; set; }
    [DataMember] public bool IsInherited { get; set; }
    [DataMember] public bool IsInheritedValue { get; set; }
    [DataMember] public bool IsInstance { get; set; }
    [DataMember] public bool IsOrdered { get; set; }
    [DataMember] public bool IsReadOnly { get; set; }
    [DataMember] public bool IsRelationship { get; set; }
    [DataMember] public bool IsRequired { get; set; }
    [DataMember] public bool IsSystem { get; set; }
    [DataMember] public string Name { get; set; } = string.Empty;
    [DataMember] public Guid? PickListId { get; set; }
    [DataMember] public short? Precision { get; set; }
    [DataMember] public Guid? RoleId { get; set; }
    [DataMember] public int SystemType { get; set; }
    [DataMember] public List<PropertyTriggerContract> Triggers { get; set; } = [];
    [DataMember] public bool UsesRole { get; set; }
}

[DataContract(Name = "PropertyGroupClient", Namespace = "")]
public sealed class PropertyGroupClientContract
{
    [DataMember] public Guid Id { get; set; }
    [DataMember] public string Name { get; set; } = string.Empty;
    [DataMember] public List<PropertyClientContract> Properties { get; set; } = [];
}

/// <summary>The cached client view model the original UI renders.</summary>
[DataContract(Name = "ViewClient", Namespace = "")]
public sealed class ViewClientContract
{
    [DataMember] public bool AllowNewValues { get; set; }
    [DataMember] public Guid AssetTypeId { get; set; }
    [DataMember] public string DriverCaption { get; set; } = string.Empty;
    [DataMember] public Guid Id { get; set; }
    [DataMember] public bool IsCreation { get; set; }
    [DataMember] public bool IsInstance { get; set; }
    [DataMember] public bool IsReadOnly { get; set; }
    [DataMember] public string Name { get; set; } = string.Empty;
    [DataMember] public string NewItemCaption { get; set; } = string.Empty;
    [DataMember] public List<PropertyGroupClientContract> PropertyGroups { get; set; } = [];
    [DataMember(Name = "filters")] public Dictionary<Guid, string> Filters { get; set; } = [];
    /// <summary>Never assigned by the original.</summary>
    [DataMember(Name = "iconId")] public string? IconId { get; set; }
    /// <summary>The view's LastModified (else Created) as en-US general date text.</summary>
    [DataMember(Name = "lastModified")] public string LastModified { get; set; } = string.Empty;
    [DataMember(Name = "prefetchAssets")] public bool PrefetchAssets { get; set; }
}
