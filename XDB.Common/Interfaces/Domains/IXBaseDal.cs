
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDB.Common.Interfaces
{

    public interface IXBaseDal : IDisposable
    {

        IList<T> GetAll<T>() where T : class, new();
        IList<T> GetAll<T>(int pgIdx, int pgSize) where T : class, new();

        IDictionary<Guid, string> GetDictionary();

        void Add(object item);
        void Delete(object item);
        void Save(object item);

        bool IsInTransaction { get; }
        bool IsDirty { get; }

        void BeginTransaction();
        void Commit();
        void Rollback();

    }

}