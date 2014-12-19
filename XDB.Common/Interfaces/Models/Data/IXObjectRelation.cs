
using System;

using XDB.Common.Enumerations;

namespace XDB.Common.Interfaces
{

    public interface IXObjectRelation
    {
        EObjectRelationType AssetRelationType { get; set; }
        Guid FromAssetId { get; set; }
        Guid ToAssetId { get; set; }
    }

}