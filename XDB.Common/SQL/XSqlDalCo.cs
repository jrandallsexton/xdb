
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Common.Constants;
using XDB.Common.Enumerations;

namespace XDB.Common.SqlDal
{

    public partial class XSqlDal
    {

        public bool IsValidId(ECommonObjectType objectType, Guid id)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@ObjectTypeId", objectType.GetHashCode()),
                new SqlParameter("@Id", id) };

            return this.ExecuteScalar(StoredProcs.IsValidId, paramList) == 1;
        }

        public Guid Id(ECommonObjectType objectType, string nameOrDisplay)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@ObjectTypeId", objectType.GetHashCode()),
                new SqlParameter("@NameOrDisplay", nameOrDisplay) };

            return this.ExecuteScalarGuid(StoredProcs.Id, paramList).Value;
        }

        public string Name(ECommonObjectType objectType, Guid id)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@ObjectTypeId", objectType.GetHashCode()),
                new SqlParameter("@Id", id) };

            return this.ExecuteScalarString(StoredProcs.Name, paramList);
        }

        public string DisplayValue(ECommonObjectType objectType, Guid id)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@ObjectTypeId", objectType.GetHashCode()),
                new SqlParameter("@Id", id) };

            return this.ExecuteScalarString(StoredProcs.DisplayValue, paramList);
        }

        public string Description(ECommonObjectType objectType, Guid id)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@ObjectTypeId", objectType.GetHashCode()),
                new SqlParameter("@Id", id) };

            return this.ExecuteScalarString(StoredProcs.Description, paramList);
        }

        public DateTime Created(ECommonObjectType objectType, Guid id)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@ObjectTypeId", objectType.GetHashCode()),
                new SqlParameter("@Id", id) };

            return this.ExecuteScalarDateTime(StoredProcs.Created, paramList).Value;
        }

        public Guid CreatedBy(ECommonObjectType objectType, Guid id)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@ObjectTypeId", objectType.GetHashCode()),
                new SqlParameter("@Id", id) };

            return this.ExecuteScalarGuid(StoredProcs.CreatedBy, paramList).Value;
        }

        public DateTime? LastModified(ECommonObjectType objectType, Guid id)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@ObjectTypeId", objectType.GetHashCode()),
                new SqlParameter("@Id", id) };

            return this.ExecuteScalarDateTime(StoredProcs.LastModified, paramList);
        }

        public Guid? LastModifiedBy(ECommonObjectType objectType, Guid id)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@ObjectTypeId", objectType.GetHashCode()),
                new SqlParameter("@Id", id) };

            return this.ExecuteScalarGuid(StoredProcs.LastModifiedBy, paramList);
        }

        public Dictionary<Guid, string> GetDictionary(ECommonObjectType objectType)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() { new SqlParameter("@ObjectTypeId", objectType.GetHashCode()) };
            return this.GetDictionary(StoredProcs.Dictionary, paramList);
        }

    }

}