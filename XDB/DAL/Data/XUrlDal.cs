
using System;
using System.Text;
using System.Collections.Generic;
using System.Data.SqlClient;

using XDB.Constants;
using XDB.DataObjects;
using XDB.Enumerations;

namespace XDB.DAL
{
    internal class XUrlDal : XSqlDal
    {

        public XUrlDal() { }

        public XUrl Get(Guid id)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT [Name], [Description], [URL], [Created], [CreatedBy], [Deleted], [DeletedBy]");
            sql.AppendLine("FROM [URLs] WITH (NoLock) WHERE [Id] = @Id");

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", id));

            XUrl url = null;

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), paramList))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    url = new XUrl();
                    rdr.Read();
                    url.Id = id;
                    if (!rdr.IsDBNull(0)) { url.Name = rdr.GetString(0); }
                    if (!rdr.IsDBNull(1)) { url.Description = rdr.GetString(1); }
                    if (!rdr.IsDBNull(2)) { url.Url = rdr.GetString(2); }
                    url.Created = rdr.GetDateTime(3);
                    url.CreatedBy = rdr.GetGuid(4);
                    if (!rdr.IsDBNull(5)) { url.Deleted = rdr.GetDateTime(5); }
                    if (!rdr.IsDBNull(6)) { url.DeletedBy = rdr.GetGuid(6); }
                    url.IsNew = false;
                    url.IsDirty = false;
                }
            }

            return url;
        }

        public bool Save(XUrl url)
        {
            List<SqlParameter> paramList = new List<SqlParameter>();

            paramList.Add(new SqlParameter("@Id", url.Id));

            if (!string.IsNullOrEmpty(url.Name))
            {
                paramList.Add(new SqlParameter("@Name", url.Name));
            }
            else
            {
                paramList.Add(new SqlParameter("@Name", null));
            }

            if (!string.IsNullOrEmpty(url.Description))
            {
                paramList.Add(new SqlParameter("@Description", url.Description));
            }
            else
            {
                paramList.Add(new SqlParameter("@Description", null));
            }

            if (!string.IsNullOrEmpty(url.Url))
            {
                paramList.Add(new SqlParameter("@URL", url.Url));
            }
            else
            {
                paramList.Add(new SqlParameter("@URL", null));
            }

            paramList.Add(new SqlParameter("@Created", url.Created));
            paramList.Add(new SqlParameter("@CreatedBy", url.CreatedBy));

            if (url.Deleted.HasValue)
            {
                paramList.Add(new SqlParameter("@Deleted", url.Deleted.Value));
            }
            else
            {
                paramList.Add(new SqlParameter("@Deleted", null));
            }

            if (url.DeletedBy.HasValue)
            {
                paramList.Add(new SqlParameter("@DeletedBy", url.DeletedBy.Value));
            }
            else
            {
                paramList.Add(new SqlParameter("@DeletedBy", null));
            }

            return base.ExecuteSql("spr_URL_Save", paramList);

        }

    }

}