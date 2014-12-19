
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

using XDB.Constants;
using XDB.DataObjects;
using XDB.Enumerations;

namespace XDB.DAL
{
    internal class XMoneyDal : XSqlDal
    {

        #region Constructors

        public XMoneyDal() { }

        public XMoneyDal(string connString) { this.ConnectionString = connString; }

        #endregion

        internal XMoney Get(Guid id)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("select cv.[value], cv.[CurrencySymbolId], cs.value as [symbol], cast(cs.display as nvarchar) as [symbolDisplay]");
            sql.AppendLine("from currencyValues cv WITH (NoLock)");
            sql.AppendLine("inner join currencysymbols cs on cs.id = cv.currencySymbolId");
            sql.AppendLine("where cv.[Id] = @Id");

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", id));

            XMoney value = null;

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), paramList))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    rdr.Read();
                    value = new XMoney();
                    value.Id = id;
                    value.Amount = rdr.GetDecimal(0);
                    value.SymbolId = rdr.GetGuid(1);
                    value.SymbolAscii = rdr.GetString(2);
                    value.SymbolText = rdr.GetString(3);
                }
            }

            return value;
        }

        internal bool CurrencyValue_Save(XMoney value)
        {

            StringBuilder sql = new StringBuilder();
            sql.AppendLine("INSERT INTO [CurrencyValues] ([Id], [CurrencySymbolId], [Value])");
            sql.AppendLine("VALUES (@Id, @SymbolId, @Value)");

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", value.Id));
            paramList.Add(new SqlParameter("@SymbolId", value.SymbolId));
            paramList.Add(new SqlParameter("@Value", value.Amount));

            return base.ExecuteInLineSql(sql.ToString(), paramList);

        }

    }

}