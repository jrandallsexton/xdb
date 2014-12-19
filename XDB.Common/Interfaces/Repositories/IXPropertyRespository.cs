
using System;
using System.Collections.Generic;

namespace XDB.Common.Interfaces
{

    public interface IXPropertyRepository<T> where T : XBase, IXProperty
    {
        IXProperty Get(Guid id);
        void Save(T xProp);
        void Delete(Guid id, Guid userId);
        IDictionary<Guid, string> GetDictionary(bool includeDeleted);
    }

}