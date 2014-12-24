
using System;
using System.Collections.Generic;

using XDB.Common.Enumerations;

namespace XDB.Common.Interfaces
{

    public interface IXObjectTypePropertyDomain<T> : IXBaseDomain where T : XBase, IXObjectTypeProperty
    {
        IXObjectTypeProperty AssetTypePropertyRelation_Get(Guid assetTypeId, Guid propertyId);
        bool Exists(Guid xObjectTypeId, Guid xPropertyId, bool isInstance);
        IXObjectTypeProperty Get(Guid id);
        IList<IXObjectTypeProperty> GetByObjectTypeId(Guid assetTypeId);
        IList<IXObjectTypeProperty> GetCollectionByObjectTypeIdAndPropertyIds(Guid assetTypeId, IList<Guid> propertyIds);
        IList<IXObjectTypeProperty> GetCollectionByObjectTypeIdsAndPropertyId(IList<Guid> assetTypeIds, Guid propertyId);
        void Save(IList<IXObjectTypeProperty> relations);
        void Save(IXObjectTypeProperty relation);
        IList<IXObjectTypeProperty> GetCollectionByAssetTypeIdAndPropertyIds(Guid xObjectTypeId, IList<Guid> propertyIds);
    }
}
