
using System;
using System.Collections.Generic;

using XDB.Common.Enumerations;

namespace XDB.Common.Interfaces
{

    public interface IXObjectTypeRepository<T> where T : XBase, IXObjectType
    {
        bool AllowAssets(Guid assetTypeId);
        IDictionary<Guid, string> AssetTypes_GetDictionary(Guid parentAssetTypeId);
        IDictionary<Guid, string> AssetTypes_GetParents();
        IList<Guid> Children(Guid assetTypeId);
        IXObjectType Get(Guid id);
        IDictionary<Guid, string> GetDictionary(bool includePlaceholders);
        Guid GetIdByAssetId(Guid assetId);
        bool HasAssets(Guid assetTypeId, EXObjectRequestType requestType);
        Guid? ParentId(Guid assetTypeId);
        string Pluralization(Guid xObjectTypeId);
        string ReportingLabel(Guid xObjectTypeId, bool forInstances, bool usePlural);
        void Save(T xObject);
    }

}