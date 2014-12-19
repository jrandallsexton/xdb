
using System;

namespace XDB.Common.Interfaces
{

    public interface IXObjectTypeProperty
    {
        Guid AssetTypeId { get; set; }
        bool IsInherited { get; set; }
        bool IsInheritedValue { get; set; }
        bool IsInstance { get; set; }
        Guid PropertyId { get; set; }
    }

}