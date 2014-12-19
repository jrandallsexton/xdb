
using System;

namespace XDB.Common.Interfaces
{

    public interface IXObjectType
    {
        bool AllowAssets { get; set; }
        string DefinitionLabel { get; set; }
        string DefinitionLabelPlural { get; set; }
        string InstanceLabel { get; set; }
        string InstanceLabelPlural { get; set; }
        Guid? ParentId { get; set; }
    }

}