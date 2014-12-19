
using System;
using System.Collections.Generic;

namespace XDB.Common.Interfaces
{

    public interface IXListRepository<T> where T : XBase, IXList
    {
        IXList Get(Guid id);
        void Save(T list);
        void Delete(Guid id, Guid userId);
        IDictionary<Guid, string> GetDictionary(bool includeDeleted);
    }

}