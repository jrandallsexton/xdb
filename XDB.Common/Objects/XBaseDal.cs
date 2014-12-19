
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Common.Enumerations;
using XDB.Common.SqlDal;
using XDB.Common.Interfaces;

namespace XDB.Common
{

    public class XBaseDal : XSqlDal, IXBaseDal
    {
        protected bool _inTrans = false;
        protected bool _isDirty = false;

        public XBaseDal(ECommonObjectType objectType) : base() { this.CommonObjectType = objectType; }

        protected internal ECommonObjectType CommonObjectType;

        //abstract internal string NameById(Guid id);
        //abstract internal bool IsValidId(Guid id);
        //abstract internal DateTime Created(Guid id);
        //abstract internal Guid CreatedBy(Guid id);

        internal DateTime Created(Guid id) { return base.Created(this.CommonObjectType, id); }
        internal Guid CreatedBy(Guid id) { return base.CreatedBy(this.CommonObjectType, id); }

        internal DateTime? LastModified(Guid id) { return base.LastModified(this.CommonObjectType, id); }
        internal Guid? LastModifiedBy(Guid id) { return base.LastModifiedBy(this.CommonObjectType, id); }

        internal bool IsValidId(Guid id) { return base.IsValidId(this.CommonObjectType, id); }

        internal Guid Id(string name) { return base.Id(this.CommonObjectType, name); }
        internal string Name(Guid id) { return base.Name(this.CommonObjectType, id); }
        internal string DisplayValue(Guid id) { return base.DisplayValue(this.CommonObjectType, id); }

        internal string Description(Guid id) { return base.Description(this.CommonObjectType, id); }

        public IDictionary<Guid, string> GetDictionary() { return base.GetDictionary(this.CommonObjectType); }

        //// http://stackoverflow.com/questions/965580/c-sharp-generics-syntax-for-multiple-type-parameter-contraints?rq=1
        //abstract internal bool Save(object commonObject, Guid userId);

        public virtual bool IsDirty
        {
            get { return _isDirty; }
            private set { _isDirty = value; }
        }

        public virtual bool IsInTransaction
        {
            get { return _inTrans; }
            private set { _inTrans = value; }
        }

        public void Dispose()
        {
            if (this.IsInTransaction) { this.Rollback(); }
        }

        public void Rollback()
        {
            throw new NotImplementedException();
        }

        public IList<T> GetAll<T>() where T : class, new()
        {
            throw new NotImplementedException();
        }

        public IList<T> GetAll<T>(int pgIdx, int pgSize) where T : class, new()
        {
            throw new NotImplementedException();
        }

        public void Add(object item)
        {
            throw new NotImplementedException();
        }

        public void Delete(object item)
        {
            throw new NotImplementedException();
        }

        public void Save(object item)
        {
            throw new NotImplementedException();
        }

        public void BeginTransaction()
        {
            throw new NotImplementedException();
        }

        public void Commit()
        {
            throw new NotImplementedException();
        }

    }

}