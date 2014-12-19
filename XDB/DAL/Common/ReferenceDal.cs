
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDB.DAL
{

    internal class ReferenceDal : XSqlDal
    {

        internal Guid CurrencySymbolId_Get(string displayText)
        {
            string sql = "SELECT [Id] FROM [CurrencySymbols] WITH (NoLock) WHERE ([Display] = @Display) OR ([Value] = @Display)";
            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Display", displayText));
            return base.ExecuteScalarGuidInLine(sql, paramList);
        }

        internal string CurrencySymbol_Get(Guid id)
        {
            string sql = "SELECT [Value] FROM [CurrencySymbols] WITH (NoLock) WHERE [Id] = @Id";
            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", id));
            return base.ExecuteScalarStringInLine(sql, paramList);
        }

        internal Dictionary<Guid, string> CurrencySymbols_Get()
        {
            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            string sql = "SELECT [Id], [Value] FROM [CurrencySymbols] WITH (NoLock) ORDER BY [Value]";
            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql, new List<SqlParameter>()))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return values; }

                while (rdr.Read())
                {
                    values.Add(rdr.GetGuid(0), rdr.GetString(1));
                }

                return values;
            }

        }

        internal Dictionary<string, string> GetValuesDisplayValues()
        {

            Dictionary<string, string> values = new Dictionary<string, string>();

            string sql = "SELECT [Value], [Display] FROM [CurrencySymbols] WITH (NoLock) ORDER BY [Value]";

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql, new List<SqlParameter>()))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return values; }

                while (rdr.Read())
                {
                    values.Add(rdr.GetString(0), rdr.GetString(1));
                }

                return values;
            }

        }

        internal Dictionary<int, string> GetDictionary()
        {
            string sql = "SELECT [Id], [Operator] FROM [lkOperators] ORDER BY [Id]";

            Dictionary<int, string> values = new Dictionary<int, string>();

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql, new List<SqlParameter>()))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return values; }

                while (rdr.Read())
                {
                    values.Add(rdr.GetInt32(0), rdr.GetString(1));
                }

                return values;

            }

        }

        internal string GetOperator(int operatorId)
        {
            string sql = string.Format("SELECT [Operator] FROM [lkOperators] WHERE [Id] = {0}", operatorId);
            return base.ExecuteScalarString(sql);
        }

        public Dictionary<int, string> Permissions_GetDictionary()
        {
            string sql = "SELECT [Id], [DisplayValue] FROM [lkPermissions] WITH (NoLock) ORDER BY [Id]";

            Dictionary<int, string> values = new Dictionary<int, string>();

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql, new List<SqlParameter>()))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return values; }

                while (rdr.Read())
                {
                    values.Add(rdr.GetInt32(0), rdr.GetString(1));
                }

                return values;

            }

        }

    }

}