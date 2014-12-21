
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Common.Constants;

namespace XDB.Common.SqlDal
{
    /// <summary>
    /// Base class from which all other data access layers inherit
    /// </summary>
    public partial class XSqlDal
    {

        #region Private Members

        private string _connectionString = string.Empty;
        //private bool _logDynamicSql = false;
        //private bool _logStoredProcs = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public XSqlDal()
        {
            this._connectionString = Config.DbConnString;
        }

        /// <summary>
        /// Overloaded constructor accepting a connection string
        /// </summary>
        /// <param name="connectionString"></param>
        public XSqlDal(string connectionString)
        {
            this._connectionString = connectionString;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// The connection string which this class will use to perform its operations against
        /// </summary>
        public string ConnectionString
        {
            get { return this._connectionString; }
            set { this._connectionString = value; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Executes a dynamic sql statement that uses SQL parameters
        /// FxCop warning suppressed because the consumer of this method should use a 'Using' on the SqlDataReader being returned
        /// </summary>
        /// <param name="sqlStatement">the SQL statement to execute</param>
        /// <param name="paramList">list of SQL parameters</param>
        /// <returns>a SqlDataReader containing the results</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public SqlDataReader OpenDataReaderInLine(string sqlStatement, List<SqlParameter> paramList)
        {

            SqlConnection cnnSql = null;
            SqlCommand cmdSql = null;
            SqlDataReader drdSql = null;

            try
            {

                cnnSql = new SqlConnection(this.ConnectionString);

                cmdSql = new SqlCommand(sqlStatement, cnnSql) { CommandTimeout = 30000, CommandType = CommandType.Text };

                if (paramList != null) { foreach (SqlParameter p in paramList) { cmdSql.Parameters.Add(p); } }

                cnnSql.Open();

                drdSql = cmdSql.ExecuteReader(CommandBehavior.CloseConnection);

                return drdSql;

            }
            catch (Exception)
            {
                if (cnnSql != null) cnnSql.Dispose();
                if (cmdSql != null) cmdSql.Dispose();
                throw;
            }

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public SqlDataReader OpenDataReaderInLine(string sqlStatement, List<SqlParameter> paramList, List<SqlParameter> valueParameters)
        {

            SqlConnection cnnSql = null;
            SqlCommand cmdSql = null;
            SqlDataReader drdSql = null;

            try
            {

                cnnSql = new SqlConnection(this.ConnectionString);

                cmdSql = new SqlCommand(sqlStatement, cnnSql) { CommandTimeout = 30000, CommandType = CommandType.Text };

                if (paramList != null) { foreach (SqlParameter p in paramList) { cmdSql.Parameters.Add(p); } }

                if (valueParameters != null)
                {
                    foreach (SqlParameter p in valueParameters)
                    {
                        cmdSql.Parameters.AddWithValue(p.ParameterName, "%" + p.Value + "%");
                    }
                }

                cnnSql.Open();

                drdSql = cmdSql.ExecuteReader(CommandBehavior.CloseConnection);

                return drdSql;

            }
            catch (Exception)
            {
                if (cnnSql != null) cnnSql.Dispose();
                if (cmdSql != null) cmdSql.Dispose();
                throw;
            }

        }

        /// <summary>
        /// FxCop warning suppressed because the consumer of this method should use a 'Using' on the SqlDataReader being returned
        /// </summary>
        /// <param name="storedProcName"></param>
        /// <param name="paramList"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public SqlDataReader OpenDataReader(string storedProcName, List<SqlParameter> paramList)
        {

            SqlConnection cnnSql = null;
            SqlCommand cmdSql = null;
            SqlDataReader drdSql = null;

            try
            {

                cnnSql = new SqlConnection(this.ConnectionString);

                cmdSql = new SqlCommand();
                cmdSql.CommandText = storedProcName;
                cmdSql.Connection = cnnSql;
                cmdSql.CommandTimeout = 3000;
                cmdSql.CommandType = CommandType.StoredProcedure;

                foreach (SqlParameter p in paramList)
                {
                    cmdSql.Parameters.Add(p);
                }

                cnnSql.Open();

                drdSql = cmdSql.ExecuteReader(CommandBehavior.CloseConnection);

                return drdSql;

            }
            catch (Exception)
            {
                //ExceptionManager.Publish(ex);
                if (cnnSql != null) cnnSql.Dispose();
                if (cmdSql != null) cmdSql.Dispose();
                throw;
            }
            finally
            {
                //if (logId != -1) {
                //    this.ExecuteSql(StoredProcs.spSys_StoredProcActivity, new List<SqlParameter>() { new SqlParameter("@SpName", storedProcName) });
                //    this.LogClose(logId); 
                //}
            }

        }

        public SqlDataReader OpenDataReader(string storedProcName, List<SqlParameter> paramList, List<SqlParameter> valueParameters)
        {
            SqlConnection cnnSql = null;
            SqlCommand cmdSql = null;
            SqlDataReader drdSql = null;

            //int logId = -1;

            try
            {

                //if (this._logStoredProcs) { logId = this.LogSql(storedProcName, paramList); }

                cnnSql = new SqlConnection(this.ConnectionString);

                cmdSql = new SqlCommand(storedProcName, cnnSql)
                {
                    CommandTimeout = 30000,
                    CommandType = CommandType.StoredProcedure
                };

                foreach (SqlParameter p in paramList) { cmdSql.Parameters.Add(p); }

                foreach (SqlParameter p in valueParameters) { cmdSql.Parameters.AddWithValue(p.ParameterName, "%" + p.Value + "%"); }

                cnnSql.Open();

                drdSql = cmdSql.ExecuteReader(CommandBehavior.CloseConnection);

                return drdSql;

            }
            catch (Exception)
            {
                //ExceptionManager.Publish(ex);
                if (cnnSql != null) cnnSql.Dispose();
                if (cmdSql != null) cmdSql.Dispose();
                throw;
            }
            //finally
            //{
            //    if (logId != -1)
            //    {
            //        this.ExecuteSql(StoredProcs.spSys_StoredProcActivity, new List<SqlParameter>() { new SqlParameter("@SpName", storedProcName) });
            //        this.LogClose(logId);
            //    }
            //}
        }

        /// <summary>
        /// Executes a simple in-line SQL statement.
        /// Should only be used with UPDATE statements.
        /// </summary>
        /// <param name="sqlStatement">the SQL statement to execute</param>
        /// <returns>true if successful; false otherwise</returns>
        public bool ExecuteSql(string sqlStatement)
        {

            //int logId = -1;

            try
            {

                //if (this._logDynamicSql) { logId = this.LogSql(sqlStatement, new List<SqlParameter>()); }

                using (SqlConnection connection = new SqlConnection(this.ConnectionString))
                {

                    using (SqlCommand cmdSql = new SqlCommand(sqlStatement, connection))
                    {
                        cmdSql.CommandTimeout = 300;
                        connection.Open();
                        return cmdSql.ExecuteNonQuery() >= -1;
                    }

                }
            }
            catch (Exception)
            {
                //ExceptionManager.Publish(ex);
                throw;
            }
            //finally
            //{
            //    if (logId != -1) { this.LogClose(logId); }
            //}

        }

        /// <summary>
        /// Executes a simple in-line SQL statement that uses SQL parameters
        /// FxCop violation suppressed because the sqlStatement is parameterized
        /// </summary>
        /// <param name="sqlStatement">the SQL statement to execute</param>
        /// <param name="paramList">list of SQL parameters</param>
        /// <returns>true if successful; false otherwise</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public bool ExecuteInLineSql(string sqlStatement, List<SqlParameter> paramList)
        {

            //int logId = -1;

            try
            {

                //if (this._logDynamicSql) { logId = this.LogSql(sqlStatement, paramList); }

                using (SqlConnection connection = new SqlConnection(this.ConnectionString))
                {

                    using (SqlCommand cmdSql = new SqlCommand(sqlStatement, connection))
                    {

                        cmdSql.CommandTimeout = 3000;
                        cmdSql.CommandType = CommandType.Text;

                        if (paramList != null) { foreach (SqlParameter p in paramList) { cmdSql.Parameters.Add(p); } }

                        connection.Open();
                        cmdSql.ExecuteNonQuery();

                        return true;
                    }

                }
            }
            catch (Exception)
            {
                //ExceptionManager.Publish(ex);
                throw;
            }
            //finally
            //{
            //    if (logId != -1) { this.LogClose(logId); }
            //}

        }

        /// <summary>
        /// Executes a stored procedure
        /// </summary>
        /// <param name="storedProcName">the name of the stored procedure</param>
        /// <param name="paramList">list of SQL parameters</param>
        /// <returns>true if successful; false otherwise</returns>
        public bool ExecuteSql(string storedProcName, List<SqlParameter> paramList)
        {

            //int logId = -1;

            try
            {

                //if (this._logStoredProcs) { logId = this.LogSql(storedProcName, paramList); }

                using (SqlConnection connection = new SqlConnection(this.ConnectionString))
                {

                    using (SqlCommand cmdSql = new SqlCommand(storedProcName, connection))
                    {

                        cmdSql.CommandTimeout = 3000;
                        cmdSql.CommandType = CommandType.StoredProcedure;

                        foreach (SqlParameter p in paramList) { cmdSql.Parameters.Add(p); }

                        connection.Open();
                        cmdSql.ExecuteNonQuery();

                        return true;
                    }

                }
            }
            catch (Exception)
            {
                //ExceptionManager.Publish(ex);
                throw;
            }
            //finally
            //{               
            //    if (logId != -1) { 
            //        this.LogClose(logId);
            //        if (storedProcName != StoredProcs.spSys_StoredProcActivity)
            //        {
            //            this.ExecuteSql(StoredProcs.spSys_StoredProcActivity, new List<SqlParameter>() { new SqlParameter("@SpName", storedProcName) });
            //        } 
            //    }
            //}

        }

        public List<string> GetTableColumnNames(string tableName)
        {

            List<string> values = null;

            string sql = string.Format("SELECT TOP 1 * FROM [{0}]", tableName);

            DataSet data = this.GetDataSetInLine(sql, new List<SqlParameter>());

            if ((data != null) && (data.Tables != null) && (data.Tables.Count == 1))
            {
                values = new List<string>();

                foreach (DataColumn col in data.Tables[0].Columns)
                {
                    values.Add(col.ColumnName);
                }
            }

            return values;

        }

        public List<string> GetStoredProcColumnNames(string spName, List<SqlParameter> paramList)
        {


            List<string> values = null;

            DataSet data = this.GetDataset(spName, paramList);

            if ((data != null) && (data.Tables != null) && (data.Tables.Count == 1))
            {
                values = new List<string>();

                foreach (DataColumn col in data.Tables[0].Columns)
                {
                    if (col.ColumnName == "rn") { continue; }
                    if (col.ColumnName == "AssetId") { continue; }
                    values.Add(col.ColumnName);
                }
            }

            return values;
        }

        public DataSet GetDataSetInLine(string sqlStatement, List<SqlParameter> paramList)
        {
            DataSet dstData = null;
            //int logId = -1;

            try
            {

                //if (this._logDynamicSql) { logId = this.LogSql(sqlStatement, new List<SqlParameter>()); }

                using (SqlConnection connection = new SqlConnection(this.ConnectionString))
                {
                    using (SqlCommand command = new SqlCommand(sqlStatement, connection))
                    {

                        command.CommandTimeout = 3000;
                        command.CommandType = CommandType.Text;

                        //add the parameters
                        foreach (SqlParameter p in paramList) { command.Parameters.Add(p); }

                        connection.Open();

                        using (DataSet dst = new DataSet())
                        {

                            using (SqlDataReader rdr = command.ExecuteReader(CommandBehavior.CloseConnection))
                            {
                                dst.Load(rdr, LoadOption.OverwriteChanges, "Dataset");

                                // I know this is odd, and I don't really like it.
                                // Didn't know you were supposed to dispose of a dataset and return a copy
                                // http://social.msdn.microsoft.com/Forums/en-US/vstscode/thread/643437d4-030a-4f02-b7c8-8a0855047b72/
                                dstData = dst;

                                return dstData;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                //ExceptionManager.Publish(ex);
                throw;
            }
            //finally
            //{
            //    if (logId != -1) { this.LogClose(logId); }
            //}
        }

        public DataSet GetDataset(string sqlStatement)
        {

            DataSet dstData = null;
            //int logId = -1;

            try
            {

                //if (this._logDynamicSql) { logId = this.LogSql(sqlStatement, new List<SqlParameter>()); }

                using (SqlConnection connection = new SqlConnection(this.ConnectionString))
                {
                    using (SqlCommand command = new SqlCommand(sqlStatement, connection))
                    {

                        command.CommandTimeout = 3000;
                        command.CommandType = CommandType.Text;

                        connection.Open();

                        using (DataSet dst = new DataSet())
                        {

                            using (SqlDataReader rdr = command.ExecuteReader(CommandBehavior.CloseConnection))
                            {
                                dst.Load(rdr, LoadOption.OverwriteChanges, "Dataset");

                                // I know this is odd, and I don't really like it.
                                // Didn't know you were supposed to dispose of a dataset and return a copy
                                // http://social.msdn.microsoft.com/Forums/en-US/vstscode/thread/643437d4-030a-4f02-b7c8-8a0855047b72/
                                dstData = dst;

                                return dstData;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                //ExceptionManager.Publish(ex);
                throw;
            }
            //finally
            //{
            //    if (logId != -1) { this.LogClose(logId); }
            //}

        }

        public DataSet GetDataset(string storedProcName, List<SqlParameter> paramList)
        {

            DataSet dstData = null;

            //int logId = -1;

            try
            {

                //if (this._logStoredProcs) { logId = this.LogSql(storedProcName, paramList); }

                using (SqlConnection connection = new SqlConnection(this.ConnectionString))
                {
                    using (SqlCommand command = new SqlCommand(storedProcName, connection))
                    {
                        command.Connection = connection;
                        command.CommandTimeout = 3000;
                        command.CommandType = CommandType.StoredProcedure;

                        //add the parameters
                        foreach (SqlParameter p in paramList) { command.Parameters.Add(p); }

                        //open the connection
                        connection.Open();

                        //initialize the dataset
                        using (DataSet dst = new DataSet())
                        {

                            //create the data table name - equal to the SP's name
                            string[] dataTableNames = new string[1];
                            dataTableNames[0] = storedProcName;

                            using (SqlDataReader rdr = command.ExecuteReader(CommandBehavior.CloseConnection))
                            {
                                //create the dataset from the datareader
                                dst.Load(rdr, LoadOption.OverwriteChanges, dataTableNames);

                                // I know this is odd, and I don't really like it.
                                // Didn't know you were supposed to dispose of a dataset and return a copy
                                // http://social.msdn.microsoft.com/Forums/en-US/vstscode/thread/643437d4-030a-4f02-b7c8-8a0855047b72/
                                dstData = dst;

                                return dstData;

                            }
                        }

                    }

                }

            }
            catch (Exception)
            {
                //ExceptionManager.Publish(ex);
                return null;
            }
            //finally
            //{
            //    if (logId != -1)
            //    {
            //        this.ExecuteSql(StoredProcs.spSys_StoredProcActivity, new List<SqlParameter>() { new SqlParameter("@SpName", storedProcName) });
            //        this.LogClose(logId);
            //    }
            //}

        }

        public Dictionary<Guid, string> GetDictionary(string spName, List<SqlParameter> paramList)
        {
            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            using (SqlDataReader rdr = this.OpenDataReader(spName, paramList))
            {
                if ((rdr == null) || (!rdr.HasRows)) return values;
                while (rdr.Read())
                {
                    if (rdr.IsDBNull(0))
                    {
                        values.Add(Misc.NullStringGuid, "NULL");
                    }
                    else
                    {
                        Guid id;
                        if (Guid.TryParse(rdr.GetValue(0).ToString(), out id))
                        {
                            values.Add(id, rdr.GetString(1));
                        }
                        else
                        {
                            if (rdr.IsDBNull(1))
                            {
                                values.Add(Misc.EmptyStringGuid, Misc.EmptyString);
                            }
                            else
                            {
                                values.Add(Misc.EmptyStringGuid, rdr.GetString(1));
                            }
                        }
                    }
                }
            }

            return values;
        }

        public IDictionary<Guid, string> GetDictionary(string sql)
        {
            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            using (var rdr = this.OpenDataReaderInLine(sql, new List<SqlParameter>()))
            {
                if ((rdr == null) || (!rdr.HasRows)) return values;
                while (rdr.Read())
                {
                    if (rdr.IsDBNull(0))
                    {
                        if (!values.ContainsKey(Constants.Misc.NullStringGuid)) { values.Add(Constants.Misc.NullStringGuid, "NULL"); }
                    }
                    else
                    {
                        Guid id;
                        if (Guid.TryParse(rdr.GetValue(0).ToString(), out id))
                        {
                            if (!values.ContainsKey(id)) { values.Add(id, rdr.GetString(1)); }
                        }
                        else
                        {
                            if (values.ContainsKey(Constants.Misc.EmptyStringGuid)) { continue; }
                            if (rdr.IsDBNull(1))
                            {
                                values.Add(Constants.Misc.EmptyStringGuid, Constants.Misc.EmptyString);
                            }
                            else
                            {
                                values.Add(Constants.Misc.EmptyStringGuid, rdr.GetString(1));
                            }
                        }
                    }
                }
            }

            return values;
        }

        public Dictionary<string, string> GetDictionaryStringString(string sql, List<SqlParameter> paramList)
        {
            Dictionary<string, string> values = new Dictionary<string, string>();

            using (var rdr = this.OpenDataReaderInLine(sql, paramList))
            {
                if ((rdr == null) || (!rdr.HasRows)) return values;
                while (rdr.Read())
                {
                    var key = rdr.GetValue(0).ToString();
                    if (!values.ContainsKey(key)) { values.Add(key, rdr.GetValue(1).ToString()); }
                }
            }

            return values;
        }

        public Dictionary<string, string> GetDictionaryStringString(string sql)
        {
            Dictionary<string, string> values = new Dictionary<string, string>();

            using (var rdr = this.OpenDataReaderInLine(sql, new List<SqlParameter>()))
            {
                if ((rdr == null) || (!rdr.HasRows)) return values;
                while (rdr.Read())
                {
                    var key = rdr.GetValue(0).ToString();
                    if (!values.ContainsKey(key)) { values.Add(key, rdr.GetValue(1).ToString()); }
                }
            }

            return values;
        }

        public Dictionary<string, int> GetDictionaryStringInt(string sql)
        {
            Dictionary<string, int> values = new Dictionary<string, int>();

            using (var rdr = this.OpenDataReaderInLine(sql, new List<SqlParameter>()))
            {
                if ((rdr == null) || (!rdr.HasRows)) return values;
                while (rdr.Read())
                {
                    values.Add(rdr.GetString(0), rdr.GetInt32(1));
                }
            }

            return values;
        }

        public bool ExecuteScalarBool(string sql)
        {
            //int logId = -1;
            try
            {
                //if (this._logDynamicSql) { logId = this.LogSql(sql, new List<SqlParameter>()); }
                using (SqlConnection connection = new SqlConnection(this.ConnectionString))
                {
                    using (SqlCommand cmdSql = new SqlCommand(sql, connection))
                    {
                        connection.Open();
                        return (bool)cmdSql.ExecuteScalar();
                    }
                }
            }
            catch (Exception)
            {
                //ExceptionManager.Publish(ex);
                throw;
            }
            //finally
            //{
            //    if (logId != -1) { this.LogClose(logId); }
            //}
        }

        public int ExecuteScalar(string sqlStatement)
        {
            //int logId = -1;
            try
            {
                //if (this._logDynamicSql) { logId = this.LogSql(sqlStatement, new List<SqlParameter>()); }
                using (SqlConnection connection = new SqlConnection(this.ConnectionString))
                {
                    using (SqlCommand cmdSql = new SqlCommand(sqlStatement, connection))
                    {
                        connection.Open();
                        return (int)cmdSql.ExecuteScalar();
                    }
                }
            }
            catch (Exception)
            {
                //ExceptionManager.Publish(ex);
                throw;
            }
            //finally
            //{
            //    if (logId != -1) { this.LogClose(logId); }
            //}
        }

        public int ExecuteScalar(string spName, List<SqlParameter> paramList)
        {

            //int logId = -1;

            try
            {

                //if (this._logStoredProcs) { logId = this.LogSql(spName, paramList); }

                using (SqlConnection connection = new SqlConnection(this.ConnectionString))
                {
                    using (SqlCommand cmdSql = new SqlCommand(spName, connection))
                    {

                        foreach (SqlParameter param in paramList)
                        {
                            cmdSql.Parameters.Add(param);
                        }

                        cmdSql.CommandType = CommandType.StoredProcedure;

                        connection.Open();

                        object value = cmdSql.ExecuteScalar();

                        int returnValue;

                        if (int.TryParse(value.ToString(), out returnValue))
                        {
                            return returnValue;
                        }

                        return -1;
                    }
                }
            }
            catch (Exception)
            {
                //ExceptionManager.Publish(ex);
                throw;
            }
            //finally
            //{
            //    if (logId != -1)
            //    {
            //        this.ExecuteSql(StoredProcs.spSys_StoredProcActivity, new List<SqlParameter> { new SqlParameter("@SpName", spName) });
            //        this.LogClose(logId);
            //    }
            //}
        }

        /// <summary>
        /// FxCop violation suppressed because the sqlStatement is parameterized
        /// </summary>
        /// <param name="sqlStatement"></param>
        /// <param name="paramList"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public int ExecuteScalarInLine(string sqlStatement, List<SqlParameter> paramList)
        {

            //int logId = -1;

            //if (this._logDynamicSql) { logId = this.LogSql(sqlStatement, paramList); }

            try
            {
                using (SqlConnection connection = new SqlConnection(this.ConnectionString))
                {
                    using (SqlCommand cmdSql = new SqlCommand(sqlStatement, connection))
                    {

                        foreach (SqlParameter param in paramList)
                        {
                            cmdSql.Parameters.Add(param);
                        }

                        connection.Open();

                        var value = cmdSql.ExecuteScalar();

                        int returnValue;

                        if (int.TryParse(value.ToString(), out returnValue))
                        {
                            return returnValue;
                        }
                        return -1;

                    }
                }
            }
            catch (Exception)
            {
                //ExceptionManager.Publish(ex);
                throw;
            }
            //finally
            //{
            //    if (logId != -1) { this.LogClose(logId); }
            //}
        }

        public string ExecuteScalarString(string sqlStatement)
        {

            //int logId = -1;

            //if (this._logDynamicSql) { logId = this.LogSql(sqlStatement, new List<SqlParameter>()); }

            try
            {
                using (var connection = new SqlConnection(this.ConnectionString))
                {
                    using (var cmdSql = new SqlCommand(sqlStatement, connection))
                    {

                        connection.Open();
                        var val = cmdSql.ExecuteScalar();

                        return val == null ? string.Empty : val.ToString().Trim();
                    }
                }
            }
            catch (Exception)
            {
                //ExceptionManager.Publish(ex);
                throw;
            }
            //finally
            //{
            //    if (logId != -1) { this.LogClose(logId); }
            //}
        }

        public string ExecuteScalarString(string spName, List<SqlParameter> paramList)
        {
            //int logId = -1;

            try
            {
                //if (this._logStoredProcs) { logId = this.LogSql(spName, paramList); }

                using (SqlConnection connection = new SqlConnection(this.ConnectionString))
                {
                    using (SqlCommand cmdSql = new SqlCommand(spName, connection))
                    {

                        foreach (SqlParameter param in paramList)
                        {
                            cmdSql.Parameters.Add(param);
                        }

                        cmdSql.CommandType = CommandType.StoredProcedure;
                        connection.Open();

                        var val = cmdSql.ExecuteScalar();

                        return val == null ? string.Empty : val.ToString().Trim();

                    }
                }
            }
            catch (Exception)
            {
                //ExceptionManager.Publish(ex);
                throw;
            }
            //finally
            //{
            //    if (logId != -1)
            //    {
            //        this.ExecuteSql(StoredProcs.spSys_StoredProcActivity, new List<SqlParameter>() { new SqlParameter("@SpName", spName) });
            //        this.LogClose(logId);
            //    }
            //}

        }

        public string ExecuteScalarStringInLine(string sqlStatement, List<SqlParameter> paramList)
        {

            //int logId = -1;

            try
            {

                //if (this._logDynamicSql) { logId = this.LogSql(sqlStatement, paramList); }

                using (SqlConnection connection = new SqlConnection(this.ConnectionString))
                {
                    using (SqlCommand cmdSql = new SqlCommand(sqlStatement, connection))
                    {

                        foreach (SqlParameter param in paramList)
                        {
                            cmdSql.Parameters.Add(param);
                        }

                        connection.Open();

                        var val = cmdSql.ExecuteScalar();

                        return val == null ? string.Empty : val.ToString().Trim();

                    }
                }
            }
            catch (Exception)
            {
                //ExceptionManager.Publish(ex);
                throw;
            }
            //finally
            //{
            //    if (logId != -1) { this.LogClose(logId); }
            //}
        }

        public Guid? ExecuteScalarGuid(string spName, List<SqlParameter> paramList)
        {
            //int logId = -1;
            try
            {
                //if (this._logStoredProcs) { logId = this.LogSql(spName, paramList); }
                using (SqlConnection cnnSql = new SqlConnection(this.ConnectionString))
                {
                    using (SqlCommand cmdSql = new SqlCommand(spName, cnnSql))
                    {
                        foreach (SqlParameter param in paramList)
                        {
                            cmdSql.Parameters.Add(param);
                        }

                        cmdSql.CommandType = CommandType.StoredProcedure;
                        cnnSql.Open();

                        object val = cmdSql.ExecuteScalar();

                        if (val != null)
                        {
                            Guid value;

                            if (Guid.TryParse(val.ToString(), out value))
                            {
                                return value;
                            }
                        }

                        return null;

                    }
                }
            }
            catch (Exception)
            {
                //ExceptionManager.Publish(ex);
                throw;
            }
            //finally
            //{
            //    if (logId != -1)
            //    {
            //        this.ExecuteSql(StoredProcs.spSys_StoredProcActivity, new List<SqlParameter>() { new SqlParameter("@SpName", spName) });
            //        this.LogClose(logId);
            //    }
            //}
        }

        /// <summary>
        /// FxCop violation suppressed because the sqlStatement is parameterized
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="paramList"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public Guid ExecuteScalarGuidInLine(string sql, List<SqlParameter> paramList)
        {

            //int logId = -1;

            try
            {

                //if (this._logDynamicSql) { logId = this.LogSql(sql, paramList); }

                using (SqlConnection cnnSql = new SqlConnection(this.ConnectionString))
                {
                    using (SqlCommand cmdSql = new SqlCommand(sql, cnnSql))
                    {

                        if (paramList != null)
                        {
                            foreach (SqlParameter param in paramList)
                            {
                                cmdSql.Parameters.Add(param);
                            }
                        }

                        cnnSql.Open();

                        object val = cmdSql.ExecuteScalar();

                        if ((val == null) || (string.IsNullOrEmpty(val.ToString())))
                        {
                            return new Guid();
                        }
                        else
                        {
                            return new Guid(val.ToString().Trim());
                        }

                    }
                }
            }
            catch (Exception)
            {
                //ExceptionManager.Publish(ex);
                throw;
            }
            //finally
            //{
            //    if (logId != -1) { this.LogClose(logId); }
            //}
        }

        /// <summary>
        /// FxCop violation suppressed because the sqlStatement is parameterized
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="paramList"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public DateTime? ExecuteScalarDateTime(string spName, List<SqlParameter> paramList)
        {

            //int logId = -1;

            try
            {

                //if (this._logDynamicSql) { logId = this.LogSql(sql, paramList); }

                using (SqlConnection cnnSql = new SqlConnection(this.ConnectionString))
                {
                    using (SqlCommand cmdSql = new SqlCommand(spName, cnnSql))
                    {

                        cmdSql.CommandType = CommandType.StoredProcedure;

                        if (paramList != null) { foreach (SqlParameter param in paramList) { cmdSql.Parameters.Add(param); } }

                        cnnSql.Open();

                        object val = cmdSql.ExecuteScalar();

                        if (val == null)
                        {
                            return null;
                        }
                        else
                        {
                            DateTime value;
                            if (DateTime.TryParse(val.ToString(), out value))
                            {
                                return value;
                            }
                            return null;
                        }

                    }
                }
            }
            catch (Exception)
            {
                //ExceptionManager.Publish(ex);
                throw;
            }
            //finally
            //{
            //    if (logId != -1) { this.LogClose(logId); }
            //}
        }

        public byte[] ExecuteScalarImage(string sqlStatement, List<SqlParameter> paramList)
        {
            using (SqlDataReader rdr = this.OpenDataReaderInLine(sqlStatement, paramList))
            {
                if (rdr.Read())
                {
                    byte[] img = (byte[])rdr[0];
                    return img;
                }
            }
            return null;
        }

        #endregion

        #region private methods

        private void LogClose(int logId)
        {
            using (SqlConnection connection = new SqlConnection(this.ConnectionString))
            {
                using (SqlCommand cmdSql = new SqlCommand("UPDATE [SysSqllogs] SET [TsEnd] = GetDate() WHERE [Id] = " + logId.ToString(), connection))
                {
                    connection.Open();
                    cmdSql.ExecuteNonQuery();
                }
            }
        }

        //private Int32 LogSql(string sql, List<SqlParameter> paramList)
        //{

        //    // get call stack
        //    StackTrace stackTrace = new StackTrace(true);

        //    string invokingClass = string.Empty;
        //    string invokingMethod = string.Empty;
        //    int invokingLineNo = -1;

        //    string invokedClass = string.Empty;
        //    string invokedMethod = string.Empty;
        //    int invokedLineNo = -1;

        //    System.Text.StringBuilder sb = new System.Text.StringBuilder();

        //    for (int i = 2; i < stackTrace.FrameCount; i++) // start at index 2; index 1 is some method here in the BaseDal
        //    {
        //        StackFrame temp = stackTrace.GetFrame(i);

        //        if (i == 2)
        //        {
        //            invokedClass = System.IO.Path.GetFileName(temp.GetFileName());
        //            invokedMethod = temp.GetMethod().Name;
        //            invokedLineNo = temp.GetFileLineNumber();
        //            sb.AppendFormat("[{0} --> {1} : {2}] ", invokedClass, invokedMethod, invokedLineNo, Environment.NewLine);
        //        }
        //        else if (i == 3)
        //        {
        //            invokingClass = System.IO.Path.GetFileName(temp.GetFileName());
        //            invokingMethod = temp.GetMethod().Name;
        //            invokingLineNo = temp.GetFileLineNumber();
        //            sb.AppendFormat("[{0} --> {1} : {2}] ", invokedClass, invokedMethod, invokedLineNo, Environment.NewLine);
        //        }
        //        else
        //        {
        //            string fName = System.IO.Path.GetFileName(temp.GetFileName());
        //            if (string.IsNullOrEmpty(fName)) { break; }
        //            sb.AppendFormat("[{0} --> {1} : {2}] ", fName, temp.GetMethod().Name, temp.GetFileLineNumber(), Environment.NewLine);
        //        }
        //    }

        //    return this.LogSql(invokingClass, invokingMethod, invokingLineNo, invokedClass, invokedMethod, invokedLineNo, sql, paramList, sb.ToString());
        //}

        private string ParamListToString(List<SqlParameter> paramList)
        {
            if ((paramList != null) && (paramList.Count > 0))
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (SqlParameter p in paramList)
                {
                    if (p.Value != null)
                    {
                        sb.AppendFormat("{0}:{1},", p.ParameterName, p.Value.ToString());
                    }
                    else
                    {
                        sb.AppendFormat("{0}:{1},", p.ParameterName, string.Empty);
                    }
                }
                return sb.ToString();
            }
            return string.Empty;
        }

        #endregion

        public string QuoteReplace(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, "'", "''");
        }

    }

}