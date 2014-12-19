
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Common;
using XDB.Common.Constants;
using XDB.Common.Enumerations;
using XDB.Common.Exceptions;
using XDB.Common.Interfaces;
using XDB.Common.SqlDal;

using XDB.Repositories;

using XDB.Models;

namespace XDB.Domains
{

    internal class XGenEngine
    {

        private XSqlDal dal = new XSqlDal();

        public bool UpdateGenTables(Guid assetId)
        {

            List<string> generatedTableNames = this.GetGeneratedTableNames();

            StringBuilder sql = new StringBuilder();

            int index = 0;
            int max = generatedTableNames.Count - 1;
            foreach (string tblName in generatedTableNames)
            {
                sql.AppendFormat("SELECT COUNT(*) AS [Count], '{0}' AS [TableName] FROM [{1}] WHERE [AssetId] = @AssetId", tblName, tblName).AppendLine();
                if (index < max) { sql.AppendLine("UNION"); }
                index++;
            }

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@AssetId", assetId));

            List<string> tables = new List<string>();

            using (SqlDataReader rdr = this.dal.OpenDataReaderInLine(sql.ToString(), paramList))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    while (rdr.Read())
                    {
                        int c = rdr.GetInt32(0);
                        if (c > 0) { tables.Add(rdr.GetString(1)); }
                    }
                }
            }

            sql = new StringBuilder();

            foreach (string s in tables)
            {
                sql.AppendFormat("EXEC spr_{0}_Update '{1}'", s, assetId).AppendLine();
            }

            if (sql.Length > 0)
            {
                return this.dal.ExecuteInLineSql(sql.ToString(), new List<SqlParameter>());
            }
            else
            {
                return true;
            }

        }

        internal bool XObjectDeleteFromGenTables(Guid objectId)
        {
            StringBuilder sql = new StringBuilder();

            foreach (string tblName in this.GetGeneratedTableNames())
            {
                sql.AppendFormat("DELETE FROM [{0}] WHERE [AssetId] = @AssetId;", tblName, tblName).AppendLine();
            }

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@AssetId", objectId));

            return this.dal.ExecuteInLineSql(sql.ToString(), paramList);
        }

        internal List<string> GetGeneratedTableNames()
        {

            List<string> values = new List<string>();

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@SchemaName", string.Format("[{0}.{1}]", Config.DbSchemaPrefix, EDBSchema.Gen)));

            using (SqlDataReader rdr = this.dal.OpenDataReader(StoredProcs.GeneratedTableNames_Get, paramList))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return values; }

                while (rdr.Read()) { values.Add(rdr.GetString(0)); }

            }

            return values;
        }

        public bool ViewCreate(string viewName, string viewSql)
        {
            if (this.ViewDrop(viewName))
            {
                StringBuilder sql = new StringBuilder();
                sql.AppendFormat("CREATE VIEW [dbo].[{0}]", viewName).AppendLine();
                sql.AppendLine("AS");
                sql.AppendLine(viewSql);
                return this.dal.ExecuteInLineSql(sql.ToString(), new List<SqlParameter>());
            }
            return false;
        }

        internal bool ViewDrop(string viewName)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendFormat("IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[{0}]'))", viewName);
            sql.AppendFormat("DROP VIEW [dbo].[{0}]", viewName);
            return this.dal.ExecuteInLineSql(sql.ToString(), new List<SqlParameter>());
        }

    }

}