
using System;
using System.Collections.Generic;

namespace XDB.Common.Interfaces
{

    public interface IXListDomain<T> where T : XBase, IXList
    {
        void Delete(Guid id, Guid userId);
        IXList Get(Guid id);
        IDictionary<Guid, string> GetDictionary(bool includeDeleted);
        void Save(T list);
    }

    //public interface IXListDomain
    //{
    //    void Delete(Guid pickListId, Guid userId);
    //    IXList Get(Guid id);
    //    //XList GetByPropertyId(Guid propertyId, bool includeDeleted, bool includeUnapproved);
    //    IDictionary<Guid, string> GetDictionary(bool includeDeleted);
    //    //Guid GetIdByPropertyId(Guid propertyId);
    //    void Save(IXList picklist, Guid userId);
    //}

}