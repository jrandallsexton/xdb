using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Xdb.Api.Application.Metadata.Legacy;

// The original's data contracts, member for member. DataContractJsonSerializer writes members in ordinal order and
// every member (nulls included), which is what the original's [DataMember(IsRequired = true)] types produced.
// Enum-typed members are ints: the original serialized enums as their numbers (DataType includes -1, Undefined).

[DataContract(Name = "Property", Namespace = "")]
public sealed class PropertyContract
{
    [DataMember] public bool AllowMultiValue { get; set; }
    [DataMember] public bool AllowNewValues { get; set; }
    [DataMember] public DateTime? Approved { get; set; }
    [DataMember] public Guid? ApprovedBy { get; set; }
    [DataMember] public Guid? AssetTypeId { get; set; }
    [DataMember] public bool? AssetTypeIsInstance { get; set; }
    [DataMember] public DateTime Created { get; set; }
    [DataMember] public Guid CreatedBy { get; set; }
    [DataMember] public int DataType { get; set; } = -1;
    [DataMember] public string DefaultValue { get; set; } = string.Empty;
    [DataMember] public DateTime? Deleted { get; set; }
    [DataMember] public Guid? DeletedBy { get; set; }
    [DataMember] public string Description { get; set; } = string.Empty;
    [DataMember] public string DisplayValue { get; set; } = string.Empty;
    [DataMember] public Guid Id { get; set; }
    [DataMember] public bool IsDirty { get; set; }
    [DataMember] public bool IsInherited { get; set; }
    [DataMember] public bool IsInheritedValue { get; set; }
    [DataMember] public bool IsInstance { get; set; }
    [DataMember] public bool IsNew { get; set; }
    [DataMember] public bool IsOrdered { get; set; }
    /// <summary>Always true: the original's constructor set it and nothing cleared it.</summary>
    [DataMember] public bool IsReadOnly { get; set; } = true;
    [DataMember] public bool IsRelationship { get; set; }
    [DataMember] public bool IsRequired { get; set; }
    [DataMember] public bool IsSystem { get; set; }
    [DataMember] public DateTime? LastModified { get; set; }
    [DataMember] public Guid? LastModifiedBy { get; set; }
    [DataMember] public string Name { get; set; } = string.Empty;
    [DataMember] public Guid? PickListId { get; set; }
    [DataMember] public string Plural { get; set; } = string.Empty;
    [DataMember] public short? Precision { get; set; }
    [DataMember] public List<PropertyContract> Properties { get; set; } = [];
    [DataMember] public Guid? RoleId { get; set; }
    [DataMember] public string Singular { get; set; } = string.Empty;
    [DataMember] public int SystemType { get; set; } = 11;
    /// <summary>Null unless a view trigger restriction targets the property.</summary>
    [DataMember] public List<Views.Legacy.PropertyTriggerContract>? Triggers { get; set; }
    [DataMember] public bool UsesRole { get; set; }
}

[DataContract(Name = "AssetTypePropertyRelation", Namespace = "")]
public sealed class AssetTypePropertyRelationContract
{
    [DataMember] public Guid AssetTypeId { get; set; }
    [DataMember] public DateTime Created { get; set; }
    [DataMember] public Guid CreatedBy { get; set; }
    [DataMember] public DateTime? Deleted { get; set; }
    [DataMember] public Guid? DeletedBy { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public bool IsDirty { get; set; }
    [DataMember] public bool IsInherited { get; set; }
    [DataMember] public bool IsInheritedValue { get; set; }
    [DataMember] public bool IsInstance { get; set; }
    [DataMember] public bool IsNew { get; set; }
    [DataMember] public Guid PropertyId { get; set; }
}

[DataContract(Name = "AssetType", Namespace = "")]
public sealed class AssetTypeContract
{
    [DataMember] public bool AllowAssets { get; set; }
    [DataMember] public DateTime Created { get; set; }
    [DataMember] public Guid CreatedBy { get; set; }
    [DataMember] public Guid? DefaultInstanceViewId { get; set; }
    [DataMember] public Guid? DefaultViewId { get; set; }
    [DataMember] public string? DefinitionLabel { get; set; }
    [DataMember] public DateTime? Deleted { get; set; }
    [DataMember] public Guid? DeletedBy { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public Guid? ImageId { get; set; }
    [DataMember] public string? InstanceLabel { get; set; }
    [DataMember] public bool IsDirty { get; set; }
    [DataMember] public bool IsNew { get; set; }
    [DataMember] public DateTime? LastModified { get; set; }
    [DataMember] public Guid? LastModifiedBy { get; set; }
    [DataMember] public string Name { get; set; } = string.Empty;
    [DataMember] public Guid? ParentId { get; set; }
    [DataMember] public string Pluralization { get; set; } = string.Empty;
    [DataMember] public List<PropertyContract> Properties { get; set; } = [];
    [DataMember] public List<AssetTypePropertyRelationContract> PropertyMembers { get; set; } = [];
}

[DataContract(Name = "PickListValue", Namespace = "")]
public sealed class PickListValueContract
{
    [DataMember] public DateTime? Approved { get; set; }
    [DataMember] public Guid? ApprovedBy { get; set; }
    [DataMember] public string BGColor { get; set; } = string.Empty;
    [DataMember] public DateTime Created { get; set; }
    [DataMember] public Guid CreatedBy { get; set; }
    [DataMember] public DateTime? Deleted { get; set; }
    [DataMember] public Guid? DeletedBy { get; set; }
    [DataMember] public string DisplayValue { get; set; } = string.Empty;
    [DataMember] public Guid Id { get; set; }
    [DataMember] public bool IsDirty { get; set; }
    [DataMember] public bool IsNew { get; set; }
    [DataMember] public DateTime? LastModified { get; set; }
    [DataMember] public Guid? LastModifiedBy { get; set; }
    [DataMember] public int? Order { get; set; }
    [DataMember] public Guid PickListId { get; set; }
    [DataMember] public string Value { get; set; } = string.Empty;
}

[DataContract(Name = "PickListDependency", Namespace = "")]
public sealed class PickListDependencyContract
{
    [DataMember] public Guid ChildPickListId { get; set; }
    [DataMember] public Guid ChildPickListValueId { get; set; }
    [DataMember] public DateTime Created { get; set; }
    [DataMember] public Guid CreatedBy { get; set; }
    [DataMember] public DateTime? Deleted { get; set; }
    [DataMember] public Guid? DeletedBy { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public Guid ParentPickListId { get; set; }
    [DataMember] public Guid ParentPickListValueId { get; set; }
}

[DataContract(Name = "PickList", Namespace = "")]
public sealed class PickListContract
{
    [DataMember] public bool AllowNewValues { get; set; }
    [DataMember] public DateTime? Approved { get; set; }
    [DataMember] public Guid? ApprovedBy { get; set; }
    [DataMember] public DateTime Created { get; set; }
    [DataMember] public Guid CreatedBy { get; set; }
    [DataMember] public DateTime? Deleted { get; set; }
    [DataMember] public Guid? DeletedBy { get; set; }
    [DataMember] public List<PickListDependencyContract> Dependencies { get; set; } = [];
    [DataMember] public string Description { get; set; } = string.Empty;
    [DataMember] public string DisplayValue { get; set; } = string.Empty;
    [DataMember] public Guid Id { get; set; }
    [DataMember] public bool IsDirty { get; set; }
    [DataMember] public bool IsMemberList { get; set; }
    [DataMember] public bool IsNew { get; set; }
    [DataMember] public DateTime? LastModified { get; set; }
    [DataMember] public Guid? LastModifiedBy { get; set; }
    [DataMember] public string Name { get; set; } = string.Empty;
    [DataMember] public List<PickListValueContract> PickListValues { get; set; } = [];
    [DataMember] public List<PropertyContract> Properties { get; set; } = [];
    [DataMember] public string ServiceMethod { get; set; } = string.Empty;
    /// <summary>The original exposed this stored value (empty in practice); kept for parity, flagged for the auth rework.</summary>
    [DataMember] public string ServicePassword { get; set; } = string.Empty;
    [DataMember] public string ServiceUrl { get; set; } = string.Empty;
    [DataMember] public string ServiceUsername { get; set; } = string.Empty;
}

[DataContract(Name = "CurrencyValue", Namespace = "")]
public sealed class CurrencyValueContract
{
    [DataMember] public decimal Amount { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public string SymbolAscii { get; set; } = string.Empty;
    [DataMember] public Guid SymbolId { get; set; }
    [DataMember] public string SymbolText { get; set; } = string.Empty;
}

[DataContract(Name = "Image", Namespace = "")]
public sealed class ImageContract
{
    [DataMember] public DateTime? Approved { get; set; }
    [DataMember] public Guid? ApprovedBy { get; set; }
    [DataMember] public DateTime Created { get; set; }
    [DataMember] public Guid CreatedBy { get; set; }
    [DataMember] public Guid? DeletedBy { get; set; }
    [DataMember] public string Description { get; set; } = string.Empty;
    [DataMember] public int Height { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public byte[]? ImageData { get; set; }
    [DataMember] public int ImageType { get; set; }
    [DataMember] public bool IsDirty { get; set; }
    [DataMember] public bool IsNew { get; set; }
    /// <summary>Always false: the original never read the column.</summary>
    [DataMember] public bool IsSystem { get; set; }
    [DataMember] public string Name { get; set; } = string.Empty;
    [DataMember] public short Order { get; set; }
    [DataMember] public string? RequestUrl { get; set; }
    [DataMember] public List<object> TagMembers { get; set; } = [];
    [DataMember] public int Width { get; set; }
}
