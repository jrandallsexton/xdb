
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlClient;

using XDB.Constants;
using XDB.DataObjects;
using XDB.Enumerations;

namespace XDB.DAL
{

    internal class XSubmittalDal : XSqlDal
    {

        #region Constructors

        public XSubmittalDal() { }

        public XSubmittalDal(string connString) { this.ConnectionString = connString; }

        #endregion

        private const string spPropertyValueSubmittal_Get_IgnoreDeletedFlag = "spr_PropertyValueSubmittal_Get_IgnoreDeletedFlag";
        private const string spPropertyValueSubmittal_Delete = "spr_PropertyValueSubmittal_Delete";

        //Collection SPs
        private const string spPropertyValueSubmittalList_Get = "spr_PropertyValueSubmittalList_Get";
        private const string spPropertyValueSubmittalList_GetDisplay = "spr_PropertyValueSubmittalList_GetDisplay";

        internal KeyValuePair<Guid, string> AssetInfo(Guid submittalId)
        {
            KeyValuePair<Guid, string> kvp = new KeyValuePair<Guid, string>();

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.PropertyValueSubmittal_GetAssetInfo, new List<SqlParameter>() { new SqlParameter("@Id", submittalId) }))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    rdr.Read();
                    kvp = new KeyValuePair<Guid, string>(rdr.GetGuid(0), rdr.GetString(1));
                }
            }

            return kvp;
        }

        private XSubmittal PropertyValueSubmittal_LoadFromReader(SqlDataReader rdr)
        {

            XSubmittal submittal = null;

            if ((rdr != null) && (rdr.HasRows))
            {

                submittal = new XSubmittal();

                rdr.Read();

                if (!rdr.IsDBNull(rdr.GetOrdinal("Id"))) { submittal.Id = rdr.GetGuid(rdr.GetOrdinal("Id")); }

                if (!rdr.IsDBNull(rdr.GetOrdinal("AssetId"))) { submittal.AssetId = rdr.GetGuid(rdr.GetOrdinal("AssetId")); }

                if (!rdr.IsDBNull(rdr.GetOrdinal("Notes"))) { submittal.Notes = rdr.GetString(rdr.GetOrdinal("Notes")); }

                if (!rdr.IsDBNull(rdr.GetOrdinal("Created"))) { submittal.Created = (DateTime)rdr[rdr.GetOrdinal("Created")]; }

                if (!rdr.IsDBNull(rdr.GetOrdinal("Reviewed"))) { submittal.Approved = (DateTime)rdr[rdr.GetOrdinal("Reviewed")]; }

                if (!rdr.IsDBNull(rdr.GetOrdinal("Deleted"))) { submittal.Deleted = (DateTime)rdr[rdr.GetOrdinal("Deleted")]; }

                if (!rdr.IsDBNull(rdr.GetOrdinal("CreatedBy"))) { submittal.CreatedBy = rdr.GetGuid(rdr.GetOrdinal("CreatedBy")); }

                if (!rdr.IsDBNull(rdr.GetOrdinal("ReviewedBy"))) { submittal.ApprovedBy = rdr.GetGuid(rdr.GetOrdinal("ReviewedBy")); }

                if (!rdr.IsDBNull(rdr.GetOrdinal("DeletedBy"))) { submittal.DeletedBy = rdr.GetGuid(rdr.GetOrdinal("DeletedBy")); }

                //submittal.PropertyValues = new PropertyValueDal().PropertyValues_GetForASubmittalGroup(submittal.Id);

                submittal.IsNew = false;
                submittal.IsDirty = false;

            }

            return submittal;
        }

        internal XSubmittal PropertyValueSubmittal_Get_IgnoreDeletedFlag(Guid id)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", id));

            using (SqlDataReader drdSql = base.OpenDataReader(spPropertyValueSubmittal_Get_IgnoreDeletedFlag, paramList))
            {
                return this.PropertyValueSubmittal_LoadFromReader(drdSql);
            }

        }

        /// <summary>
        /// Gets an instance of a property value
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        internal XSubmittal Get(Guid id)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", id));

            using (SqlDataReader drdSql = base.OpenDataReader(StoredProcs.PropertyValueSubmittal_Get, paramList))
            {
                return this.PropertyValueSubmittal_LoadFromReader(drdSql);
            }

        }

        /// <summary>
        /// Deletes any occurences of the specified match that aren't already marked as deleted
        /// </summary>
        /// <param name="assetId"></param>
        /// <param name="propertyId"></param>
        /// <returns></returns>
        internal bool PropertyValueSubmittal_Delete(Guid assetId, Guid propertyId, Guid userId)
        {

            StringBuilder sql = new StringBuilder();
            sql.AppendLine("UPDATE [PropertyValueSubmittals]");
            sql.AppendLine("SET [Deleted] = GetDate(),");
            sql.AppendLine("[DeletedBy] = @DeletedBy");
            sql.AppendLine("WHERE [AssetId] = @AssetId");
            sql.AppendLine("AND [PropertyId] = @PropertyId");
            sql.AppendLine("AND [Deleted] IS NULL");

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@DeletedBy", userId));
            paramList.Add(new SqlParameter("@AssetId", assetId));
            paramList.Add(new SqlParameter("@PropertyId", propertyId));

            return base.ExecuteInLineSql(sql.ToString(), paramList);
        }

        internal bool PropertyValueSubmittal_Save(XSubmittal submittal)
        {

            if (!submittal.IsDirty) { return true; }

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", submittal.Id));
            paramList.Add(new SqlParameter("@AssetId", submittal.AssetId));
            paramList.Add(new SqlParameter("@Created", submittal.Created));
            paramList.Add(new SqlParameter("@Reviewed", submittal.Approved));
            paramList.Add(new SqlParameter("@Deleted", submittal.Deleted));
            paramList.Add(new SqlParameter("@CreatedBy", submittal.CreatedBy));
            paramList.Add(new SqlParameter("@ReviewedBy", submittal.ApprovedBy));
            paramList.Add(new SqlParameter("@DeletedBy", submittal.DeletedBy));

            if (base.ExecuteSql(StoredProcs.PropertyValueSubmittal_Save, paramList))
            {
                submittal.IsNew = false;
                submittal.IsDirty = false;

                return true;
            }

            return false;
        }

        internal bool PropertyValueSubmittal_Delete(Guid id, Guid userId)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", id));
            paramList.Add(new SqlParameter("@DeletedBy", userId));

            return base.ExecuteSql(spPropertyValueSubmittal_Delete, paramList);

        }

        //internal PropertyValueSubmittalList PropertyValueSubmittalList_GetDisplay(Dictionary<Guid, Enumerations.EAssetRequestType> assetTypeIds, int? maxRecords)
        //{

        //    PropertyValueSubmittalList list = new PropertyValueSubmittalList();

        //    StringBuilder sql = new StringBuilder();

        //    if (maxRecords.HasValue)
        //    {
        //        sql.AppendFormat("SELECT DISTINCT TOP {0}", maxRecords.Value).AppendLine();
        //    }
        //    else
        //    {
        //        sql.AppendLine("SELECT DISTINCT TOP 10");
        //    }

        //    sql.AppendLine("A.AssetId, A.AssetName, A.Notes, A.Created, A.CreatedBy, A.CreatedByDisplay");
        //    sql.AppendLine("FROM (");

        //    if (maxRecords.HasValue)
        //    {
        //        sql.AppendFormat("SELECT DISTINCT TOP {0}", maxRecords.Value * 2).AppendLine(); // * 2 in order to get enough records for outer top distinct b/c of propertyValues
        //    }
        //    else
        //    {
        //        sql.AppendLine("SELECT DISTINCT");
        //    }

        //    sql.AppendLine("PVS.AssetId AS [AssetId],");
        //    sql.AppendLine("A.[Name] AS [AssetName],");
        //    sql.AppendLine("PVS.[Notes] AS [Notes],");
        //    sql.AppendLine("A.[LastModified] AS [Created],");
        //    sql.AppendLine("PVS.[Created] AS [PVSCreated],");
        //    sql.AppendLine("PVS.[CreatedBy] AS [CreatedBy],");
        //    sql.AppendLine("[dbo].[GetMemberValueById](PVS.[CreatedBy]) AS [CreatedByDisplay]");

        //    sql.AppendLine("FROM [dbo].[PropertyValueSubmittals] PVS ");

        //    sql.AppendLine("INNER JOIN [Members] M ON M.[Id] = PVS.[CreatedBy]");
        //    //sql.AppendLine("INNER JOIN [PropertyValues] PV ON PV.[SubmittalGroupId] = PVS.[Id]");
        //    sql.AppendLine("INNER JOIN [Assets] A ON A.[Id] = PVS.[AssetId]");
        //    sql.AppendLine("WHERE");

        //    sql.AppendFormat("(PVS.[Created] > '{0}') AND", DateTime.Now.Subtract(new TimeSpan(30, 0, 0, 0, 0))).AppendLine();
        //    sql.AppendLine("(PVS.[Deleted] IS NULL)");

        //    if ((assetTypeIds != null) && (assetTypeIds.Count > 0))
        //    {
        //        int idx = 0;
        //        int max = assetTypeIds.Count - 1;

        //        sql.AppendLine("AND (");
        //        foreach (KeyValuePair<Guid, Enumerations.EAssetRequestType> kvp in assetTypeIds)
        //        {

        //            switch (kvp.Value)
        //            {
        //                case Enumerations.EAssetRequestType.Both:
        //                    sql.AppendFormat("(A.[AssetTypeId] = '{0}')", kvp.Key).AppendLine();
        //                    break;
        //                case Enumerations.EAssetRequestType.Definition:
        //                    sql.AppendFormat("(A.[AssetTypeId] = '{0}' AND A.[IsInstance] = 0)", kvp.Key).AppendLine();
        //                    break;
        //                case Enumerations.EAssetRequestType.Instance:
        //                    sql.AppendFormat("(A.[AssetTypeId] = '{0}' AND A.[IsInstance] = 0)", kvp.Key).AppendLine();
        //                    break;
        //            }

        //            if (idx < max)
        //            {
        //                sql.AppendLine("OR");
        //            }

        //            idx++;
        //        }

        //        sql.AppendLine(")");
        //    }

        //    sql.AppendLine("ORDER BY PVS.[Created] DESC");

        //    sql.AppendLine(") A");
        //    sql.AppendLine("ORDER BY [Created] DESC");

        //    using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), new List<SqlParameter>()))
        //    {
        //        if ((rdr != null) && (rdr.HasRows))
        //        {
        //            int index = 0;
        //            while (rdr.Read())
        //            {
        //                if (index == 15) { break; }

        //                PropertyValueSubmittal submittal = new PropertyValueSubmittal();

        //                if (!rdr.IsDBNull(0)) submittal.AssetId = rdr.GetGuid(0);

        //                if (!rdr.IsDBNull(1)) submittal.AssetName = rdr.GetString(1);

        //                if (!rdr.IsDBNull(2)) submittal.Notes = rdr.GetString(2);

        //                if (!rdr.IsDBNull(3)) { submittal.Created = (DateTime)rdr.GetValue(3); }

        //                if (!rdr.IsDBNull(4)) submittal.CreatedBy = (Guid)rdr.GetValue(4);

        //                if (!rdr.IsDBNull(5)) submittal.CreatedByDisplay = rdr.GetString(5);

        //                //if (!rdr.IsDBNull(7)) { submittal.Reviewed = (DateTime)rdr.GetValue(7); }

        //                //if (!rdr.IsDBNull(8)) submittal.ReviewedBy = (Guid)rdr.GetValue(8);

        //                //if (!rdr.IsDBNull(9)) submittal.ReviewedByDisplay = rdr.GetString(9);

        //                submittal.IsNew = false;
        //                submittal.IsDirty = false;

        //                list.Add(submittal);

        //                index++;
        //            }
        //        }
        //    }

        //    list.IsNew = false;
        //    list.IsDirty = false;

        //    return list;

        //}

        //internal PropertyValueSubmittalList PropertyValueSubmittalList_GetDisplay()
        //{

        //    List<SqlParameter> paramList = null;
        //    PropertyValueSubmittalList list = new PropertyValueSubmittalList();
        //    PropertyValueSubmittal submittal = null;

        //    try
        //    {

        //        paramList = new List<SqlParameter>();
        //        using (SqlDataReader drdSql = base.OpenDataReader(spPropertyValueSubmittalList_GetDisplay, paramList))
        //        {

        //            if ((drdSql != null) && (drdSql.HasRows))
        //            {

        //                //get the index of each property we are going to load
        //                int Id = drdSql.GetOrdinal("Id");
        //                int AssetId = drdSql.GetOrdinal("AssetId");
        //                int AssetName = drdSql.GetOrdinal("AssetName");
        //                int Notes = drdSql.GetOrdinal("Notes");
        //                int Created = drdSql.GetOrdinal("Created");
        //                int CreatedBy = drdSql.GetOrdinal("CreatedBy");
        //                int CreatedByDisplay = drdSql.GetOrdinal("CreatedByDisplay");
        //                int Reviewed = drdSql.GetOrdinal("Reviewed");
        //                int ReviewedBy = drdSql.GetOrdinal("ReviewedBy");
        //                int ReviewedByDisplay = drdSql.GetOrdinal("ReviewedByDisplay");

        //                while (drdSql.Read())
        //                {

        //                    submittal = new PropertyValueSubmittal();

        //                    if (!drdSql.IsDBNull(Id)) submittal.Id = drdSql.GetGuid(Id);

        //                    if (!drdSql.IsDBNull(AssetId)) submittal.AssetId = drdSql.GetGuid(AssetId);

        //                    if (!drdSql.IsDBNull(AssetName)) submittal.AssetName = drdSql.GetString(AssetName);

        //                    if (!drdSql.IsDBNull(Notes)) submittal.Notes = drdSql.GetString(Notes);

        //                    if (!drdSql.IsDBNull(Created)) { submittal.Created = (DateTime)drdSql.GetValue(Created); }

        //                    if (!drdSql.IsDBNull(CreatedBy)) submittal.CreatedBy = (Guid)drdSql.GetValue(CreatedBy);

        //                    if (!drdSql.IsDBNull(CreatedByDisplay)) submittal.CreatedByDisplay = drdSql.GetString(CreatedByDisplay);

        //                    if (!drdSql.IsDBNull(Reviewed)) { submittal.Reviewed = (DateTime)drdSql.GetValue(Reviewed); }

        //                    if (!drdSql.IsDBNull(ReviewedBy)) submittal.ReviewedBy = (Guid)drdSql.GetValue(ReviewedBy);

        //                    if (!drdSql.IsDBNull(ReviewedByDisplay)) submittal.ReviewedByDisplay = drdSql.GetString(ReviewedByDisplay);

        //                    submittal.IsNew = false;
        //                    submittal.IsDirty = false;

        //                    list.Add(submittal);

        //                }

        //            }

        //            list.IsNew = false;
        //            list.IsDirty = false;

        //            return list;

        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        ExceptionManager.Publish(ex);
        //        throw;
        //    }
        //}

        //internal PropertyValueSubmittalList PropertyValueSubmittalList_Get()
        //{

        //    List<SqlParameter> paramList = null;
        //    PropertyValueSubmittalList list = new PropertyValueSubmittalList();
        //    PropertyValueSubmittal _PropertyValueSubmittal = null;

        //    try
        //    {

        //        paramList = new List<SqlParameter>();
        //        using (SqlDataReader drdSql = base.OpenDataReader(spPropertyValueSubmittalList_Get, paramList))
        //        {

        //            if ((drdSql != null) && (drdSql.HasRows))
        //            {

        //                //get the index of each property we are going to load
        //                int Id = drdSql.GetOrdinal("Id");
        //                int AssetId = drdSql.GetOrdinal("AssetId");
        //                int Notes = drdSql.GetOrdinal("Notes");
        //                int Created = drdSql.GetOrdinal("Created");
        //                int Reviewed = drdSql.GetOrdinal("Reviewed");
        //                int CreatedBy = drdSql.GetOrdinal("CreatedBy");
        //                int ReviewedBy = drdSql.GetOrdinal("ReviewedBy");

        //                while (drdSql.Read())
        //                {

        //                    _PropertyValueSubmittal = new PropertyValueSubmittal();

        //                    if (!drdSql.IsDBNull(Id)) _PropertyValueSubmittal.Id = drdSql.GetGuid(Id);

        //                    if (!drdSql.IsDBNull(AssetId)) _PropertyValueSubmittal.AssetId = drdSql.GetGuid(AssetId);

        //                    if (!drdSql.IsDBNull(Notes)) _PropertyValueSubmittal.Notes = drdSql.GetString(Notes);

        //                    if (!drdSql.IsDBNull(Created)) _PropertyValueSubmittal.Created = (DateTime)drdSql.GetValue(Created);

        //                    if (!drdSql.IsDBNull(Reviewed)) _PropertyValueSubmittal.Reviewed = (DateTime)drdSql.GetValue(Reviewed);

        //                    if (!drdSql.IsDBNull(CreatedBy)) _PropertyValueSubmittal.CreatedBy = (Guid)drdSql.GetValue(CreatedBy);

        //                    if (!drdSql.IsDBNull(ReviewedBy)) _PropertyValueSubmittal.ReviewedBy = (Guid)drdSql.GetValue(ReviewedBy);

        //                    _PropertyValueSubmittal.IsNew = false;

        //                    _PropertyValueSubmittal.IsDirty = false;

        //                    list.Add(_PropertyValueSubmittal);

        //                }

        //            }

        //            list.IsNew = false;
        //            list.IsDirty = false;

        //            return list;

        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        ExceptionManager.Publish(ex);
        //        throw;
        //    }

        //}

        //internal bool PropertyValueSubmittalList_Save(PropertyValueSubmittalList submittals)
        //{

        //    try
        //    {

        //        foreach (PropertyValueSubmittal submittal in submittals)
        //        {
        //            if (submittal.IsDirty)
        //            {

        //                this.PropertyValueSubmittal_Save(submittal);

        //                submittal.IsNew = false;
        //                submittal.IsDirty = false;
        //            }

        //        }

        //        submittals.IsNew = false;
        //        submittals.IsDirty = false;

        //        return true;

        //    }
        //    catch (Exception ex)
        //    {
        //        ExceptionManager.Publish(ex);
        //        throw;
        //    }

        //}

        //internal bool PropertyValueSubmittalList_Delete(PropertyValueSubmittalList submittals, Guid userId)
        //{
        //    foreach (PropertyValueSubmittal PropertyValueSubmittal in submittals)
        //    {
        //        PropertyValueSubmittal_Delete(PropertyValueSubmittal.Id, userId);
        //    }
        //    return true;

        //}

        internal List<XSubmittal> GetUnprocessedForTriggers()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("SELECT [ID]");
            sb.AppendLine("FROM [PropertyValueSubmittals]");
            sb.AppendLine("WHERE [TriggerChecked] IS NULL");
            sb.AppendLine("ORDER BY [Created] DESC");

            List<Guid> ids = new List<Guid>();
            using (SqlDataReader rdr = base.OpenDataReaderInLine(sb.ToString(), new List<SqlParameter>()))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    while (rdr.Read())
                    {
                        ids.Add(rdr.GetGuid(0));
                    }
                }
            }

            List<XSubmittal> values = new List<XSubmittal>();

            foreach (Guid id in ids) { values.Add(this.Get(id)); }

            return values;
        }

        internal List<XSubmittalHelper> SubmittalActivity_GetForAdmin(int maxItems)
        {
            List<XSubmittalHelper> values = new List<XSubmittalHelper>();

            StringBuilder sql = new StringBuilder();
            sql.AppendFormat("SELECT DISTINCT TOP {0} A.[Id], A.[AssetTypeId], ISNULL(A.[DisplayValue], A.[Name]) AS [Asset Code], 'Admin' AS [Role], PVS.[Id], PVS.[Narrative], PVS.[Created]", maxItems).AppendLine();
            sql.AppendLine("FROM [Assets] A");
            sql.AppendLine("INNER JOIN [PropertyValueSubmittals] PVS ON PVS.[AssetId] = A.[Id]");
            sql.AppendLine("WHERE ((PVS.[Narrative] IS NOT NULL) AND (PVS.[CreatedBy] != '6CACA132-7827-4DE6-81E6-2B7D8DB939B9'))");
            sql.AppendLine("ORDER BY PVS.[Created] DESC");

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), new List<SqlParameter>()))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    while (rdr.Read())
                    {
                        values.Add(new XSubmittalHelper(rdr.GetGuid(0), rdr.GetGuid(1), rdr.GetString(2), rdr.GetString(3), rdr.GetGuid(4), rdr.GetString(5), rdr.GetDateTime(6)));
                    }
                }
            }

            return values;
        }

        internal List<XSubmittalHelper> SubmittalActivity_GetByRoleMembership(string innerViewSelect, int maxItems)
        {

            List<XSubmittalHelper> values = new List<XSubmittalHelper>();

            StringBuilder sql = new StringBuilder();
            sql.AppendFormat("SELECT DISTINCT TOP {0} A.[AssetId], A2.[AssetTypeId], [Asset Code], [Role], PVS.[Id], PVS.[Narrative], PVS.[Created] FROM (", maxItems).AppendLine();
            sql.AppendLine(innerViewSelect);
            sql.AppendLine(") A");
            sql.AppendLine("INNER JOIN [PropertyValueSubmittals] PVS ON PVS.[AssetId] = A.[AssetId]");
            sql.AppendLine("INNER JOIN [Assets] A2 ON A2.[Id] = A.[AssetId]");
            sql.AppendLine("WHERE PVS.[Narrative] IS NOT NULL");
            sql.AppendLine("ORDER BY PVS.[Created] DESC");

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), new List<SqlParameter>()))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    while (rdr.Read())
                    {
                        values.Add(new XSubmittalHelper(rdr.GetGuid(0), rdr.GetGuid(1), rdr.GetString(2), rdr.GetString(3), rdr.GetGuid(4), rdr.GetString(5), rdr.GetDateTime(6)));
                    }
                }
            }

            return values;
        }

        internal XNarrativeHelper CreateHelper(Guid submittalId)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT");
            sql.AppendLine("P.[Id]");
            sql.AppendLine(",ISNULL(P.[DisplayValue], P.[Name]) AS [Property]");
            sql.AppendLine(",pv.[Value] AS [PVValue]");
            sql.AppendLine(",dbo.GetPropertyValueLookupByPropValId(PV.[Id], PV.[PropertyId]) AS [Value]");
            sql.AppendLine("FROM [PropertyValues] pv");
            sql.AppendLine("INNER JOIN [Properties] p on p.[Id] = pv.[PropertyId]");
            sql.AppendLine("where pv.[SubmittalGroupId] = @Id");
            sql.AppendLine("ORDER BY [Property]");

            sql.AppendLine("SELECT A.[Id], IsNull(A.[DisplayValue], A.[Name]) AS [Asset], A.[AssetTypeId], A.[Created], PVS.[Created] AS [Submitted], A.[IsInstance],");
            sql.AppendLine("isnull(dbo.GetAssetParentDescByInstanceId(A.[Id]), dbo.GetAssetParentNameByInstanceId(A.[Id])) AS [Product Code]");
            sql.AppendLine(",dbo.[GetMemberFriendlyValueById](PVS.[CreatedBy]) AS [SubmittedBy]");
            sql.AppendLine(",AT.[Name] AS [AssetType]");
            sql.AppendLine("FROM [PropertyValueSubmittals] PVS");
            sql.AppendLine("INNER JOIN [Assets] A ON A.[Id] = PVS.[AssetId]");
            sql.AppendLine("INNER JOIN [AssetTypes] AT ON AT.[Id] = A.[AssetTypeId]");
            sql.AppendLine("WHERE pvs.Id = @Id");

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", submittalId));

            XNarrativeHelper helper = null;

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), paramList))
            {
                if ((rdr != null) && (rdr.HasRows))
                {

                    helper = new XNarrativeHelper();
                    helper.SubmittalId = submittalId;

                    while (rdr.Read())
                    {
                        Guid propId = rdr.GetGuid(0);

                        if (helper.PropertyIds.Contains(propId))
                        {
                            continue;
                        }

                        string prop = rdr.GetString(1);
                        string val = rdr.IsDBNull(2) ? string.Empty : rdr.GetString(2);
                        string decoded = rdr.IsDBNull(3) ? string.Empty : rdr.GetString(3);

                        helper.PropertyIds.Add(propId);
                        helper.Values.Add(propId, val);
                        helper.Decoded.Add(propId, string.Format("{0} = {1}", prop, decoded));
                    }

                    if (helper != null)
                    {
                        if (rdr.NextResult())
                        {
                            rdr.Read();
                            helper.AssetId = rdr.GetGuid(0);
                            helper.AssetName = rdr.GetString(1);
                            helper.AssetTypeId = rdr.GetGuid(2);
                            helper.AssetCreated = rdr.GetDateTime(3);
                            helper.SubmittalDate = rdr.GetDateTime(4);
                            helper.IsInstance = (bool)rdr.GetValue(5);
                            helper.InstanceOf = rdr.IsDBNull(6) ? string.Empty : rdr.GetString(6);
                            helper.Submitter = rdr.GetString(7);
                            helper.AssetType = rdr.GetString(8);
                        }
                    }

                }
            }

            return helper;
        }

        internal bool SetNarrative(Guid submittalId, string narrative)
        {
            string sql = "UPDATE [PropertyValueSubmittals] SET [Narrative] = @Narr WHERE [Id] = @Id";

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Narr", narrative));
            paramList.Add(new SqlParameter("@Id", submittalId));

            return base.ExecuteInLineSql(sql, paramList);
        }

        internal Dictionary<string, KeyValuePair<string, string>> GetSummary(Guid submittalId)
        {
            Dictionary<string, KeyValuePair<string, string>> values = new Dictionary<string, KeyValuePair<string, string>>();

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.PropertyValueSubmittal_GetSummary, new List<SqlParameter>() { new SqlParameter("@Id", submittalId) }))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    while (rdr.Read())
                    {
                        var key = rdr.GetString(0);
                        if (!values.ContainsKey(key)) { values.Add(key, new KeyValuePair<string, string>(rdr.GetString(1), rdr.GetString(2))); }
                    }
                }
            }

            return values;
        }

        internal DateTime Created(Guid submittalId)
        {
            string sql = "SELECT [Created] FROM [PropertyValueSubmittals] WHERE [Id] = @Id";

            return base.ExecuteScalarDateTime(sql, new List<SqlParameter>() { new SqlParameter("@Id", submittalId) }).Value;
        }

    }

}