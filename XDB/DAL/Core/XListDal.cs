
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Constants;
using XDB.Enumerations;

using XDB.DataObjects;

namespace XDB.DAL
{

    internal class XListDal : XBaseDal
    {

        public XListDal() : base(ECommonObjectType.XList) { }

        internal XList Get(Guid id)
        {

            XList pickList = null;

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", id));

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.Picklist_Get, paramList))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return null; }

                pickList = new XList();

                rdr.Read();

                int name = rdr.GetOrdinal("Name");
                int displayValue = rdr.GetOrdinal("DisplayValue");
                int description = rdr.GetOrdinal("Description");
                int isMemberList = rdr.GetOrdinal("IsMemberList");
                int allowNewValues = rdr.GetOrdinal("AllowNewValues");
                int serviceUrl = rdr.GetOrdinal("ServiceUrl");
                int serviceMethod = rdr.GetOrdinal("ServiceMethod");
                int serviceUsername = rdr.GetOrdinal("ServiceUsername");
                int servicePassword = rdr.GetOrdinal("ServicePassword");
                int created = rdr.GetOrdinal("Created");
                int createdBy = rdr.GetOrdinal("CreatedBy");
                int approved = rdr.GetOrdinal("Approved");
                int approvedBy = rdr.GetOrdinal("ApprovedBy");
                int lastModified = rdr.GetOrdinal("LastModified");
                int lastModifiedBy = rdr.GetOrdinal("LastModifiedBy");
                int deleted = rdr.GetOrdinal("Deleted");
                int deletedBy = rdr.GetOrdinal("DeletedBy");

                pickList.Id = id;

                if (!rdr.IsDBNull(name)) pickList.Name = rdr.GetString(name);

                if (!rdr.IsDBNull(displayValue)) pickList.DisplayValue = rdr.GetString(displayValue);

                if (!rdr.IsDBNull(description)) pickList.Description = rdr.GetString(description);

                if (!rdr.IsDBNull(isMemberList)) pickList.IsMemberList = (bool)rdr[isMemberList];

                if (!rdr.IsDBNull(allowNewValues)) pickList.AllowNewValues = (bool)rdr[allowNewValues];

                if (!rdr.IsDBNull(serviceUrl)) pickList.ServiceUrl = rdr.GetString(serviceUrl);

                if (!rdr.IsDBNull(serviceMethod)) pickList.ServiceMethod = rdr.GetString(serviceMethod);

                if (!rdr.IsDBNull(serviceUsername)) pickList.ServiceUsername = rdr.GetString(serviceUsername);

                if (!rdr.IsDBNull(servicePassword)) pickList.ServicePassword = rdr.GetString(servicePassword);

                pickList.Created = rdr.GetDateTime(created);
                pickList.CreatedBy = rdr.GetGuid(createdBy);

                if (!rdr.IsDBNull(approved)) pickList.Approved = rdr.GetDateTime(approved);
                if (!rdr.IsDBNull(approvedBy)) pickList.ApprovedBy = rdr.GetGuid(approvedBy);

                if (!rdr.IsDBNull(lastModified)) pickList.LastModified = rdr.GetDateTime(lastModified);
                if (!rdr.IsDBNull(lastModifiedBy)) pickList.LastModifiedBy = rdr.GetGuid(lastModifiedBy);

                if (!rdr.IsDBNull(deleted)) pickList.Deleted = rdr.GetDateTime(deleted);
                if (!rdr.IsDBNull(deletedBy)) pickList.DeletedBy = rdr.GetGuid(deletedBy);

                pickList.IsNew = false;
                pickList.IsDirty = false;

            }

            return pickList;

        }

        internal bool Save(XList picklist, Guid userId)
        {

            if ((!picklist.IsNew) || (!picklist.IsDirty)) { return true; }

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", picklist.Id));
            paramList.Add(new SqlParameter("@Name", picklist.Name));

            paramList.Add(new SqlParameter()
            {
                ParameterName = "@DisplayValue",
                Value = (string.IsNullOrEmpty(picklist.DisplayValue)) ? null : picklist.DisplayValue
            });

            paramList.Add(new SqlParameter()
            {
                ParameterName = "@Description",
                Value = (string.IsNullOrEmpty(picklist.Description)) ? null : picklist.Description
            });

            paramList.Add(new SqlParameter("@IsMemberList", picklist.IsMemberList));

            paramList.Add(new SqlParameter()
            {
                ParameterName = "@ServiceUrl",
                Value = (string.IsNullOrEmpty(picklist.ServiceUrl)) ? null : picklist.ServiceUrl
            });

            paramList.Add(new SqlParameter()
            {
                ParameterName = "@ServiceMethod",
                Value = (string.IsNullOrEmpty(picklist.ServiceMethod)) ? null : picklist.ServiceMethod
            });

            paramList.Add(new SqlParameter()
            {
                ParameterName = "@ServiceUsername",
                Value = (string.IsNullOrEmpty(picklist.ServiceUsername)) ? null : picklist.ServiceUsername
            });

            paramList.Add(new SqlParameter()
            {
                ParameterName = "@ServicePassword",
                Value = (string.IsNullOrEmpty(picklist.ServicePassword)) ? null : picklist.ServicePassword
            });

            //paramList.Add(new SqlParameter("@AllowNewValues", picklist.AllowNewValues));
            paramList.Add(new SqlParameter("@Created", picklist.Created));
            paramList.Add(new SqlParameter("@CreatedBy", picklist.CreatedBy));
            paramList.Add(new SqlParameter("@Approved", picklist.Approved));
            paramList.Add(new SqlParameter("@ApprovedBy", picklist.ApprovedBy));
            paramList.Add(new SqlParameter("@LastModified", picklist.LastModified));
            paramList.Add(new SqlParameter("@LastModifiedBy", picklist.LastModifiedBy));

            if (base.ExecuteSql(StoredProcs.Picklist_Save, paramList))
            {

                picklist.IsNew = false;
                picklist.IsDirty = false;

                return true;

            }
            else
            {
                return false;
            }

        }

        internal bool Delete(Guid pickListId, Guid userId)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", pickListId));
            paramList.Add(new SqlParameter("@DeletedBy", userId));

            return base.ExecuteSql(StoredProcs.Picklist_Delete, paramList);

        }

        internal Guid GetIdByPropertyId(Guid propertyId)
        {
            string sql = "SELECT [PickListId] FROM [Properties] WITH (NoLock) WHERE [Id] = @PropertyId";
            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@PropertyId", propertyId));
            return base.ExecuteScalarGuidInLine(sql, paramList);
        }

        internal Dictionary<Guid, string> GetDictionary(bool includeDeleted)
        {

            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@IncludeDeleted", includeDeleted));

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.Picklists_GetDictionary, paramList))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return values; }

                while (rdr.Read())
                {
                    if ((!rdr.IsDBNull(0)) && (!rdr.IsDBNull(1)))
                    {
                        values.Add(rdr.GetGuid(0), rdr.GetString(1));
                    }
                }

            }

            return values;

        }

    }

}