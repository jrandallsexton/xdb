
using System;
using System.Collections.Generic;

namespace XDB.Common.Interfaces
{

    public interface IXObjectTypePropertyRepository<T> where T : XBase, IXObjectTypeProperty
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