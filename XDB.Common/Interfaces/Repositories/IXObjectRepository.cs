
using System;
using System.Collections.Generic;

using XDB.Common.Enumerations;

namespace XDB.Common.Interfaces
{

    public interface IXObjectRepository<T> where T : XBase, IXObject
    {
        IXObject Get(Guid id);
        void Save(T xObject);
        void Delete(Guid id, Guid userId);
        Guid? Asset_GetId(string assetName, Guid assetTypeId);
        bool Asset_Rename(Guid assetId, string newName);
        IList<Guid> AssetIds_Get(string assetName, IList<Guid> assetTypeIdsToSearch);
        IDictionary<Guid, string> Assets_SearchByName(IList<Guid> assetTypeIds, EXObjectRequestType requestType, string searchValue, bool includeDescriptions);
        Guid AssetTypeId(Guid assetId);
        IList<Guid> GetIdsByName(string assetName);
        Guid? InstanceOfId(Guid assetId);
        IList<Guid> InstanceOfIds(IList<Guid> assetIds);
        bool IsInstance(Guid assetId);
        bool MarkAsUpdated(Guid assetid, Guid userId);
        Guid? ParentId(Guid assetId);
        bool ChangeObjectType(Guid instanceOfId, Guid newObjectTypeId);
    }

}