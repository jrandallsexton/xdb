
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

    internal class XListValueDal : XBaseDal
    {

        public XListValueDal() : base(ECommonObjectType.XListValue) { }

        internal XListValue Get(Guid id)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", id));

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.PicklistValue_Get, paramList))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return null; }

                XListValue plValue = new XListValue();
                plValue.Id = id;

                rdr.Read();

                int value = rdr.GetOrdinal("Value");
                int pickListId = rdr.GetOrdinal("PickListId");
                int display = rdr.GetOrdinal("DisplayValue");
                int bgColor = rdr.GetOrdinal("BGColor");
                int order = rdr.GetOrdinal("Order");
                int created = rdr.GetOrdinal("Created");
                int approved = rdr.GetOrdinal("Approved");
                int lastMod = rdr.GetOrdinal("LastModified");
                int createdBy = rdr.GetOrdinal("CreatedBy");
                int approvedBy = rdr.GetOrdinal("ApprovedBy");
                int lastModBy = rdr.GetOrdinal("LastModifiedBy");
                int deletedBy = rdr.GetOrdinal("DeletedBy");

                if (!rdr.IsDBNull(value)) { plValue.Value = rdr.GetString(value); }

                if (!rdr.IsDBNull(pickListId)) { plValue.PickListId = rdr.GetGuid(pickListId); }

                if (!rdr.IsDBNull(display)) { plValue.DisplayValue = rdr.GetString(display); }

                if (!rdr.IsDBNull(bgColor)) { plValue.BGColor = rdr.GetString(bgColor); }

                if (!rdr.IsDBNull(order)) { plValue.Index = rdr.GetInt32(order); }

                if (!rdr.IsDBNull(created)) { plValue.Created = rdr.GetDateTime(created); }

                if (!rdr.IsDBNull(approved)) { plValue.Approved = rdr.GetDateTime(approved); }

                if (!rdr.IsDBNull(lastMod)) { plValue.LastModified = rdr.GetDateTime(lastMod); }

                if (!rdr.IsDBNull(createdBy)) { plValue.CreatedBy = rdr.GetGuid(createdBy); }

                if (!rdr.IsDBNull(approvedBy)) { plValue.ApprovedBy = rdr.GetGuid(approvedBy); }

                if (!rdr.IsDBNull(lastModBy)) { plValue.LastModifiedBy = rdr.GetGuid(lastModBy); }

                if (!rdr.IsDBNull(deletedBy)) { plValue.DeletedBy = rdr.GetGuid(deletedBy); }

                plValue.IsNew = false;
                plValue.IsDirty = false;

                return plValue;
            }

        }

        internal bool Save(XListValue plValue, Guid userId)
        {

            if ((!plValue.IsDirty) || (!plValue.IsNew)) { return true; }

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", plValue.Id));
            paramList.Add(new SqlParameter("@PickListId", plValue.PickListId));
            paramList.Add(new SqlParameter("@Value", plValue.Value));

            paramList.Add(new SqlParameter()
            {
                ParameterName = "@DisplayValue",
                Value = (string.IsNullOrEmpty(plValue.DisplayValue)) ? null : plValue.DisplayValue
            });

            paramList.Add(new SqlParameter()
            {
                ParameterName = "@BGColor",
                Value = (string.IsNullOrEmpty(plValue.BGColor)) ? null : plValue.BGColor
            });

            paramList.Add(new SqlParameter("@Index", plValue.Index));
            paramList.Add(new SqlParameter("@Created", plValue.Created));
            paramList.Add(new SqlParameter("@CreatedBy", plValue.CreatedBy));
            paramList.Add(new SqlParameter("@Approved", plValue.Approved));
            paramList.Add(new SqlParameter("@ApprovedBy", plValue.ApprovedBy));
            paramList.Add(new SqlParameter("@LastModified", plValue.LastModified));
            paramList.Add(new SqlParameter("@LastModifiedBy", plValue.LastModifiedBy));
            paramList.Add(new SqlParameter("@Deleted", plValue.Deleted));
            paramList.Add(new SqlParameter("@DeletedBy", plValue.DeletedBy));

            if (base.ExecuteSql(StoredProcs.PicklistValue_Save, paramList))
            {

                plValue.IsNew = false;
                plValue.IsDirty = false;

                return true;

            }
            else
            {
                return false;
            }

        }

        internal bool Delete(Guid valueId, Guid userId)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", valueId));
            paramList.Add(new SqlParameter("@DeletedBy", userId));

            return base.ExecuteSql(StoredProcs.PicklistValue_Delete, paramList);

        }

        internal bool Save(List<XListValue> values, Guid userId)
        {
            foreach (XListValue value in values) { this.Save(value, userId); }
            return true;
        }

        internal List<XListValue> GetCollection(Guid pickListId)
        {

            List<XListValue> values = new List<XListValue>();

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@PickListId", pickListId));

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.PicklistValues_Get, paramList))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return values; }

                //get the index of each property we are going to load
                int Id = rdr.GetOrdinal("Id");
                int value = rdr.GetOrdinal("Value");
                int display = rdr.GetOrdinal("DisplayValue");
                int bgColor = rdr.GetOrdinal("BGColor");
                int index = rdr.GetOrdinal("Index");
                int created = rdr.GetOrdinal("Created");
                int createdBy = rdr.GetOrdinal("CreatedBy");
                int approved = rdr.GetOrdinal("Approved");
                int approvedBy = rdr.GetOrdinal("ApprovedBy");
                int lastMod = rdr.GetOrdinal("LastModified");
                int lastModBy = rdr.GetOrdinal("LastModifiedBy");

                while (rdr.Read())
                {

                    XListValue plValue = new XListValue();

                    plValue.Id = rdr.GetGuid(Id);

                    plValue.PickListId = pickListId;

                    if (!rdr.IsDBNull(value)) { plValue.Value = rdr.GetString(value); }

                    if (!rdr.IsDBNull(display)) { plValue.DisplayValue = rdr.GetString(display); }

                    if (!rdr.IsDBNull(bgColor)) { plValue.BGColor = rdr.GetString(bgColor); }

                    if (!rdr.IsDBNull(index)) { plValue.Index = rdr.GetInt32(index); }

                    plValue.Created = rdr.GetDateTime(created);
                    plValue.CreatedBy = rdr.GetGuid(createdBy);

                    if (!rdr.IsDBNull(approved)) { plValue.Approved = rdr.GetDateTime(approved); }
                    if (!rdr.IsDBNull(approvedBy)) { plValue.ApprovedBy = rdr.GetGuid(approvedBy); }

                    if (!rdr.IsDBNull(lastMod)) { plValue.LastModified = rdr.GetDateTime(lastMod); }
                    if (!rdr.IsDBNull(lastModBy)) { plValue.LastModifiedBy = rdr.GetGuid(lastModBy); }

                    plValue.IsNew = false;
                    plValue.IsDirty = false;

                    values.Add(plValue);

                }

            }

            return values;

        }

        internal bool PickListValueList_DeleteByPickListId(Guid pickListId, Guid userId)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@PickListId", pickListId));
            paramList.Add(new SqlParameter("@DeletedBy", userId));

            return base.ExecuteSql(StoredProcs.PickListValues_DeleteByPickListId, paramList);

        }

        internal Guid GetIdByValue(Guid pickListId, string value)
        {
            string sql = "SELECT [Id] FROM [PickListValues] WITH (NoLock) WHERE [PickListId] = @PickListId AND [Value] = @Value AND [Deleted] IS NULL";
            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@PickListId", pickListId));
            paramList.Add(new SqlParameter("@Value", value));
            return base.ExecuteScalarGuidInLine(sql, paramList);
        }

        internal Guid GetIdByDisplayValue(Guid pickListId, string displayValue)
        {
            string sql = "SELECT [Id] FROM [PickListValues] WITH (NoLock) WHERE [PickListId] = @PickListId AND [DisplayValue] = @Value AND [Deleted] IS NULL";
            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@PickListId", pickListId));
            paramList.Add(new SqlParameter("@Value", displayValue));
            return base.ExecuteScalarGuidInLine(sql, paramList);
        }

        internal Dictionary<Guid, string> GetMatching(List<Guid> pickListValueIds, Guid pickListId)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT PLV.[Id], IsNull(PLV.[DisplayValue], PLV.[Value]) AS [Value]");
            sql.AppendLine("FROM [PickListValues] PLV WITH (NoLock)");
            sql.AppendLine("WHERE PLV.[Id] IN (");

            int idCount = pickListValueIds.Count;

            for (int i = 0; i < idCount; i++)
            {
                if (i == (idCount - 1))
                {
                    sql.AppendLine("'" + pickListValueIds[i].ToString() + "')");
                }
                else
                {
                    sql.AppendLine("'" + pickListValueIds[i].ToString() + "',");
                }
            }

            sql.AppendLine("AND (PLV.[PickListId] = @PickListId)");
            sql.AppendLine("AND (PLV.[Deleted] IS NULL)");
            sql.AppendLine("ORDER BY [Value]");

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@PickListId", pickListId));

            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), paramList))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    while (rdr.Read())
                    {
                        values.Add(rdr.GetGuid(0), rdr.GetString(1));
                    }
                }
            }

            return values;

        }

    }

}