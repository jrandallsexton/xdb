
using System;
using System.Collections.Generic;

using XDB.Common.Enumerations;

namespace XDB.Common.Interfaces
{

    public interface IXObjectTypeDomain<T> :IXBaseDomain where T : XBase, IXObjectType
    {
        bool AllowAssets(Guid xObjectTypeId);
        IList<Guid> AssetType_GetChildren(Guid assetTypeId, bool includeAllDescendants);
        IDictionary<Guid, string> AssetType_GetLowestLevelParents(Guid assetTypeId);
        //global::XDB.Models.XObjectTypeProperty AssetTypePropertyRelation_Get(Guid assetTypeId, Guid propertyId);
        IDictionary<Guid, string> AssetTypes_GetAvailableForCreation(Guid assetTypeId);
        IDictionary<Guid, Guid> AssetTypes_GetDefaultViews(Guid userId, EXObjectRequestType requestType);
        IDictionary<Guid, string> AssetTypes_GetParents();
        bool Delete(Guid assetTypeId, Guid userId);
        IXObjectType Get(Guid id);
        IXObjectType GetByName(string assetTypeName);
        IDictionary<Guid, string> GetDictionary(bool includePlaceholders);
        IDictionary<Guid, string> GetDictionaryByParentId(Guid parentAssetTypeId);
        Guid GetIdByAssetId(Guid assetId);
        IList<Guid> GetStack(Guid assetTypeId);
        bool HasAssets(Guid assetTypeId, EXObjectRequestType requestType, bool searchChildAssetTypes);
        bool HasChildAssetTypes(Guid assetTypeId);
        bool HasProperty(Guid assetTypeId, Guid propertyId, bool isInstance);
        Guid? ParentId(Guid assetTypeId);
        string Pluralization(Guid assetTypeId);
        string ReportingLabel(Guid assetTypeId, bool forInstances, bool usePlural);
        void Save(T xObjectType, Guid userId);
    }

}