
using System;

using XDB.Common.Enumerations;

namespace XDB.Common.Interfaces
{

    public interface IXProperty
    {
        bool AllowMultiValue { get; set; }
        bool AllowNewValues { get; set; }
        Guid? AssetTypeId { get; set; }
        bool? AssetTypeIsInstance { get; set; }
        EDataType DataType { get; set; }
        string DefaultValue { get; set; }
        Guid? DependentPropertyId { get; set; }
        bool IsInherited { get; set; }
        bool IsInheritedValue { get; set; }
        bool IsInstance { get; set; }
        bool IsOrdered { get; set; }
        bool IsReadOnly { get; set; }
        bool IsRelationship { get; set; }
        bool IsRequired { get; set; }
        bool IsSystem { get; set; }
        Guid? PickListId { get; set; }
        short? Precision { get; set; }
        Guid? RoleId { get; set; }
        ESystemType SystemType { get; set; }
        bool UsesRole { get; }
    }

}