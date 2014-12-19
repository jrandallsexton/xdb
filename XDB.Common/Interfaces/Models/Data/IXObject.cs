
using System;

namespace XDB.Common.Interfaces
{

    public interface IXObject
    {
        Guid AssetTypeId { get; set; }
        Guid? InstanceOfId { get; set; }
        bool IsInstance { get; }
    }

}