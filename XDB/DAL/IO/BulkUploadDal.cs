
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Constants;
using XDB.DataObjects;
using XDB.Enumerations;

namespace XDB.DAL
{

    public class BulkUploadDal : XSqlDal
    {

        public BulkUploadDal() { }

        internal Dictionary<Guid, string> GetTemplateDictionary()
        {
            Dictionary<Guid, string> values = new Dictionary<Guid, string>();
            string sql = "SELECT [Id], [Name] FROM [BulkUploadTemplates] ORDER BY [Name]";
            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql, new List<SqlParameter>()))
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

        internal List<Guid> GetTemplatePropertyIds(Guid templateId)
        {
            List<Guid> values = new List<Guid>();

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", templateId));

            using (SqlDataReader rdr = base.OpenDataReader("spr_BulkUploadTemplatePropertyIds_Get", paramList))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    while (rdr.Read())
                    {
                        values.Add(rdr.GetGuid(0));
                    }
                }
            }

            return values;
        }

        public bool BulkUpload_Start(Guid uploadId, Guid fileId, Guid userId)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("INSERT INTO [BulkUploads] ([Id], [FileId], [Created], [CreatedBy])");
            sql.AppendLine("VALUES (@Id, @FileId, GetDate(), @UserId)");

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", uploadId));
            paramList.Add(new SqlParameter("@FileId", fileId));
            paramList.Add(new SqlParameter("@UserId", userId));

            return base.ExecuteInLineSql(sql.ToString(), paramList);
        }

        public bool BulkImport_Stop(Guid uploadId, int recordsProcessed)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("UPDATE [BulkUploads] SET [Completed] = GetDate(), [RecordCount] = @RecordCount WHERE [Id] = @Id");

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@RecordCount", recordsProcessed));
            paramList.Add(new SqlParameter("@Id", uploadId));

            return base.ExecuteInLineSql(sql.ToString(), paramList);
        }

        public BulkUpload BulkUpload_Get(Guid uploadId)
        {

            string sql = "SELECT [FileId], [Created], [CreatedBy], [Completed], [RecordCount] FROM [BulkUploads] WITH (NoLock) WHERE [Id] = @Id";

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", uploadId));

            BulkUpload upload = null;

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql, paramList))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    rdr.Read();
                    upload = new BulkUpload();
                    upload.Id = uploadId;
                    upload.FileId = rdr.GetGuid(0);
                    upload.Created = rdr.GetDateTime(1);
                    upload.CreatedBy = rdr.GetGuid(2);
                    if (!rdr.IsDBNull(3)) { upload.Completed = rdr.GetDateTime(3); }
                    if (!rdr.IsDBNull(4)) { upload.RecordCount = rdr.GetInt32(4); }
                }
            }

            return upload;

        }

        public Dictionary<Guid, string> BulkUploads_Get(Guid? memberId, int filter)
        {

            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            List<SqlParameter> paramList = new List<SqlParameter>();

            if (memberId.HasValue)
            {
                paramList.Add(new SqlParameter("@MemberId", memberId.Value));
            }
            else
            {
                paramList.Add(new SqlParameter("@MemberId", null));
            }

            using (SqlDataReader rdr = base.OpenDataReader("spr_BulkUploads_Get", paramList))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    while (rdr.Read())
                    {
                        Guid uploadId = rdr.GetGuid(0);
                        string fileName = rdr.GetString(1);
                        string lName = rdr.GetString(2);
                        string fName = rdr.GetString(3);
                        string userId = rdr.GetString(4);
                        string completed = rdr.IsDBNull(5) ? string.Empty : rdr.GetDateTime(5).ToString();
                        int recordCount = rdr.IsDBNull(6) ? -1 : rdr.GetInt32(6);
                        DateTime created = rdr.GetDateTime(7);

                        string value = string.Empty;

                        if (filter != 0)
                        {
                            if (filter == 1)
                            {
                                // successful imports only
                                if (recordCount <= 0) { continue; }
                            }
                            else
                            {
                                // failed attempts only
                                if (recordCount >= 0) { continue; }
                            }
                        }

                        string result = recordCount > 0 ? "uploaded successfully" : "failed to upload";

                        if (memberId.HasValue)
                        {
                            value = string.Format("{0} {1} on {2}.  UploadId: {3}", fileName, result, created.ToString(), uploadId);
                        }
                        else
                        {
                            value = string.Format("{0} {1} on {2} by {3}, {4} ({5}).  UploadId: {6}", fileName, result, created.ToString(), lName, fName, userId, uploadId);
                        }

                        values.Add(uploadId, value);
                    }
                }
            }

            return values;
        }

        public bool BulkUploadLog_Create(Guid uploadId, string category, string message, Guid? assetId, int order)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@BulkUploadId", uploadId));
            paramList.Add(new SqlParameter("@Category", category));

            if (assetId.HasValue)
            {
                paramList.Add(new SqlParameter("@AssetId", assetId.Value));
            }
            else
            {
                paramList.Add(new SqlParameter("@AssetId", null));
            }

            paramList.Add(new SqlParameter("@Message", message));
            paramList.Add(new SqlParameter("@Order", order));

            return base.ExecuteSql("spr_BulkUpdateLog_Save", paramList);

        }

        public List<BulkUploadLog> BulkUploadLogs_Get(Guid bulkUploadId, bool desc, int startPos)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@BulkUploadId", bulkUploadId));

            string spName = string.Empty;

            if (startPos > 0)
            {
                paramList.Add(new SqlParameter("@StartPos", startPos));
                spName = (desc == true) ? "spr_BulkUploadLogsLimited_GetDesc" : "spr_BulkUploadLogsLimited_GetAsc";
            }
            else
            {
                spName = (desc == true) ? "spr_BulkUploadLogs_GetDesc" : "spr_BulkUploadLogs_GetAsc";
            }

            List<BulkUploadLog> logs = new List<BulkUploadLog>();

            using (SqlDataReader rdr = base.OpenDataReader(spName, paramList))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    int recordcount = 0;
                    while (rdr.Read())
                    {
                        recordcount++;
                        int id = rdr.GetInt32(0);
                        string category = rdr.GetString(1);

                        Guid? assetId = null;
                        string assetName = string.Empty;
                        Guid? assetTypeId = null;
                        bool isInstance = false;
                        if (!rdr.IsDBNull(2))
                        {
                            assetId = rdr.GetGuid(2);
                            if (!rdr.IsDBNull(3)) { assetName = rdr.GetString(3); }
                            if (!rdr.IsDBNull(4)) { assetTypeId = rdr.GetGuid(4); }
                            if (!rdr.IsDBNull(5)) { isInstance = (bool)rdr.GetValue(5); }
                        }

                        string message = rdr.GetString(6);
                        int order = rdr.GetInt32(7);

                        BulkUploadLog log = new BulkUploadLog();
                        log.Id = id;
                        log.Category = category;
                        log.AssetId = assetId;
                        log.AssetName = assetName;
                        log.AssetTypeId = assetTypeId;
                        log.IsInstance = isInstance;
                        log.Message = base.QuoteReplace(message);
                        log.Order = order;

                        logs.Add(log);
                    }
                }
            }

            return logs;
        }

    }

}