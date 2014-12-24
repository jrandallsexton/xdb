
using System;
using System.Collections.Generic;

using XDB.Models;
using XDB.Common;
using XDB.Common.Enumerations;
using XDB.Common.Exceptions;
using XDB.Common.Extensions;
using XDB.Common.Interfaces;
using XDB.Repositories;

namespace XDB.Domains
{

    public class XObjectTypePropertyDomain<T> : XBaseDomain, IXObjectTypePropertyDomain<T> where T : XBase, IXObjectTypeProperty
    {
        public XObjectTypePropertyDomain() : base(ECommonObjectType.XObjectTypeProperty) { }

        public IXObjectTypeProperty AssetTypePropertyRelation_Get(Guid assetTypeId, Guid propertyId)
        {
            throw new NotImplementedException();
        }

        public bool Exists(Guid xObjectTypeId, Guid xPropertyId, bool isInstance)
        {
            throw new NotImplementedException();
        }

        public IXObjectTypeProperty Get(Guid id)
        {
            throw new NotImplementedException();
        }

        public IList<IXObjectTypeProperty> GetByObjectTypeId(Guid assetTypeId)
        {
            throw new NotImplementedException();
        }

        public IList<IXObjectTypeProperty> GetCollectionByObjectTypeIdAndPropertyIds(Guid assetTypeId, IList<Guid> propertyIds)
        {
            throw new NotImplementedException();
        }

        public IList<IXObjectTypeProperty> GetCollectionByObjectTypeIdsAndPropertyId(IList<Guid> assetTypeIds, Guid propertyId)
        {
            throw new NotImplementedException();
        }

        public void Save(IList<IXObjectTypeProperty> relations)
        {
            throw new NotImplementedException();
        }

        public void Save(IXObjectTypeProperty relation)
        {
            throw new NotImplementedException();
        }


        public IList<IXObjectTypeProperty> GetCollectionByAssetTypeIdAndPropertyIds(Guid xObjectTypeId, IList<Guid> propertyIds)
        {
            throw new NotImplementedException();
        }
    }

}