
using System;
using System.Collections.Generic;

namespace XDB.Common.Interfaces
{

    public interface IXObjectDomain<T> : IXBaseDomain where T : XBase, IXObject
    {
        void Delete(Guid id, Guid userId);
        IXObject Get(Guid id);
        void Save(T list);
        bool ChangeObjectType(Guid instanceOfId, Guid newObjectTypeId);
    }

}