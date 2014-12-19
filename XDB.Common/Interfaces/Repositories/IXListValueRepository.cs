
using System;
using System.Collections.Generic;

namespace XDB.Common.Interfaces
{

    public interface IXListValueRepository<T> : IDisposable where T : XBase, IXListValue
    {
        void Delete(Guid valueId, Guid userId);
        T Get(Guid id);
        IList<T> GetCollection(Guid id);
        Guid GetIdByDisplayValue(Guid id, string displayValue);
        Guid GetIdByValue(Guid id, string value);
        IDictionary<Guid, string> GetMatching(IList<Guid> pickListValueIds, Guid id);
        void DeleteByXListId(Guid xListId, Guid userId);
        void Save(T plValue, Guid userId);
        void Save(IList<T> values, Guid userId);
    }

    //public interface IXListValueRepository<T> where T : XBase, IXListValue
    //{
    //    void Delete(Guid valueId, Guid userId);
    //    IXListValue Get(Guid id);
    //    IList<IXListValue> GetCollection(Guid pickListId);
    //    Guid GetIdByDisplayValue(Guid pickListId, string displayValue);
    //    Guid GetIdByValue(Guid pickListId, string value);
    //    IDictionary<Guid, string> GetMatching(IList<Guid> pickListValueIds, Guid pickListId);
    //    void PickListValueList_DeleteByPickListId(Guid pickListId, Guid userId);
    //    //void Save(IList<IXListValue> values, Guid userId);
    //    void Save(T plValue, Guid userId);
    //    //void Save(IXListValue plValue, Guid userId);
    //}

}
