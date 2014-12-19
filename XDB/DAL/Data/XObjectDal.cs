
using System;
using System.Text;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

using XDB.Constants;
using XDB.DataObjects;
using XDB.Enumerations;

namespace XDB.DAL
{

    internal class XObjectDal : XBaseDal
    {

        public XObjectDal() : base(ECommonObjectType.XObject) { }

        //public AssetDal(string connString) : this() { this.ConnectionString = connString; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        /// <remarks>1.05.06</remarks>
        internal XObject Get(Guid Id)
        {

            XObject asset = null;

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.Asset_Get, new List<SqlParameter>() { new SqlParameter("@Id", Id) }))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return null; }

                rdr.Read();

                asset = new XObject();

                int assetTypeId = rdr.GetOrdinal("AssetTypeId");
                int name = rdr.GetOrdinal("Name");
                int displayValue = rdr.GetOrdinal("DisplayValue");
                int desc = rdr.GetOrdinal("Description");
                int instanceOfId = rdr.GetOrdinal("InstanceOfId");
                int created = rdr.GetOrdinal("Created");
                int createdBy = rdr.GetOrdinal("CreatedBy");
                int approved = rdr.GetOrdinal("Approved");
                int approvedBy = rdr.GetOrdinal("ApprovedBy");
                int lastMod = rdr.GetOrdinal("LastModified");
                int lastModBy = rdr.GetOrdinal("LastModifiedBy");
                int deleted = rdr.GetOrdinal("Deleted");
                int deletedBy = rdr.GetOrdinal("DeletedBy");

                asset.AssetTypeId = rdr.GetGuid(assetTypeId);

                if (!rdr.IsDBNull(name)) { asset.Name = rdr.GetString(name); }

                if (!rdr.IsDBNull(displayValue)) { asset.DisplayValue = rdr.GetString(displayValue); }

                if (!rdr.IsDBNull(desc)) { asset.Description = rdr.GetString(desc); }

                if (!rdr.IsDBNull(instanceOfId)) { asset.InstanceOfId = rdr.GetGuid(instanceOfId); }

                asset.Created = rdr.GetDateTime(created);
                asset.CreatedBy = rdr.GetGuid(createdBy);

                if (!rdr.IsDBNull(approved)) { asset.Approved = rdr.GetDateTime(approved); }
                if (!rdr.IsDBNull(approvedBy)) { asset.ApprovedBy = rdr.GetGuid(approvedBy); }

                if (!rdr.IsDBNull(lastMod)) { asset.LastModified = rdr.GetDateTime(lastMod); }
                if (!rdr.IsDBNull(lastModBy)) { asset.LastModifiedBy = rdr.GetGuid(lastModBy); }

                if (!rdr.IsDBNull(deleted)) { asset.Deleted = rdr.GetDateTime(deleted); }
                if (!rdr.IsDBNull(deletedBy)) { asset.DeletedBy = rdr.GetGuid(deletedBy); }

                asset.Id = Id;
                asset.IsNew = false;
                asset.IsDirty = false;

                return asset;

            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        /// <remarks>1.05.06</remarks>
        internal bool Save(XObject asset)
        {

            if (!asset.IsDirty) { return true; }

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", asset.Id));
            paramList.Add(new SqlParameter("@AssetTypeId", asset.AssetTypeId));
            paramList.Add(new SqlParameter("@Name", asset.Name));
            paramList.Add(new SqlParameter() { ParameterName = "@DisplayValue", Value = string.IsNullOrEmpty(asset.DisplayValue) ? null : asset.DisplayValue });
            paramList.Add(new SqlParameter() { ParameterName = "@Description", Value = string.IsNullOrEmpty(asset.Description) ? null : asset.Description });
            paramList.Add(new SqlParameter() { ParameterName = "@InstanceOfId", Value = asset.InstanceOfId.HasValue ? asset.InstanceOfId : null });
            paramList.Add(new SqlParameter("@Created", asset.Created));
            paramList.Add(new SqlParameter("@CreatedBy", asset.CreatedBy));
            paramList.Add(new SqlParameter() { ParameterName = "@Approved", Value = asset.Approved.HasValue ? asset.Approved : null });
            paramList.Add(new SqlParameter() { ParameterName = "@ApprovedBy", Value = asset.ApprovedBy.HasValue ? asset.ApprovedBy : null });
            paramList.Add(new SqlParameter() { ParameterName = "@LastModified", Value = asset.LastModified.HasValue ? asset.LastModified : null });
            paramList.Add(new SqlParameter() { ParameterName = "@LastModifiedBy", Value = asset.LastModifiedBy.HasValue ? asset.LastModifiedBy : null });

            if (base.ExecuteSql(StoredProcs.Asset_Save, paramList))
            {

                //if (asset.AssetMembers != null)
                //{
                //    AssetAssetRelationDal relationDal = new AssetAssetRelationDal();
                //    foreach (AssetAssetRelation relation in asset.AssetMembers)
                //    {
                //        if (!relationDal.AssetAssetRelationExists(relation.FromAssetId, relation.ToAssetId, relation.AssetRelationType.GetHashCode()))
                //        {
                //            if (!relationDal.AssetAssetRelation_Save(relation)) { return false; }
                //        }
                //    }
                //}

                asset.IsNew = false;
                asset.IsDirty = false;

                return true;

            }

            return false;

        }

        internal bool Delete(Guid objectId, Guid userId)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", objectId));
            paramList.Add(new SqlParameter("@DeletedBy", userId));

            return base.ExecuteSql(StoredProcs.Asset_Delete, paramList);

        }

        internal Dictionary<Guid, string> Assets_SearchByName(List<Guid> assetTypeIds, EAssetRequestType requestType, string searchValue, bool includeDescriptions)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT [Id], [Name],");
            if (requestType == EAssetRequestType.Instance)
            {
                sql.AppendLine("[dbo].GetAssetParentDescByInstanceId ([Id]) AS [Description]");
            }
            else
            {
                sql.AppendLine("[Description]");
            }
            sql.AppendLine("FROM [Assets] WITH (NoLock)");
            sql.AppendLine("WHERE [Deleted] IS NULL");
            sql.AppendLine("AND [Approved] IS NOT NULL");

            switch (requestType)
            {
                case EAssetRequestType.Definition:
                    sql.AppendLine("AND [IsInstance] = 0");
                    break;
                case EAssetRequestType.Instance:
                    sql.AppendLine("AND [IsInstance] = 1");
                    break;
            }
            sql.AppendLine("AND [Name] LIKE @SearchValue");

            sql.AppendLine("AND [AssetTypeId] IN (");
            for (int i = 0; i < assetTypeIds.Count; i++)
            {
                if (i == (assetTypeIds.Count - 1))
                {
                    sql.AppendLine(string.Format("'{0}'", assetTypeIds[i].ToString()));
                }
                else
                {
                    sql.AppendLine(string.Format("'{0}',", assetTypeIds[i].ToString()));
                }
            }
            sql.AppendLine(")");
            sql.AppendLine("ORDER BY [Name]");

            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            List<SqlParameter> valueParams = new List<SqlParameter>();
            valueParams.Add(new SqlParameter("@SearchValue", searchValue));

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), new List<SqlParameter>(), valueParams))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    while (rdr.Read())
                    {
                        if ((!rdr.IsDBNull(0)) && (!rdr.IsDBNull(1)))
                        {
                            Guid id = rdr.GetGuid(0);
                            string name = rdr.GetString(1);
                            string desc = string.Empty;

                            if (!rdr.IsDBNull(2)) { desc = rdr.GetString(2); }

                            if (!string.IsNullOrEmpty(desc))
                            {
                                values.Add(id, name + " (" + desc + ")");
                            }
                            else
                            {
                                values.Add(id, name);
                            }
                        }
                    }
                }
            }

            return values;
        }

        internal Guid? Asset_GetId(string assetName, Guid assetTypeId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>
                {
                    new SqlParameter("@Name", assetName),
                    new SqlParameter("@AssetTypeId", assetTypeId)
                };
            return base.ExecuteScalarGuid(StoredProcs.Asset_GetId, paramList);
        }

        internal List<Guid> AssetIds_Get(string assetName, List<Guid> assetTypeIdsToSearch)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT [Id] FROM [Assets] WITH (NoLock)");
            sql.AppendLine("WHERE [Name] = @AssetName");
            sql.AppendLine("AND [AssetTypeId] IN (");
            for (int i = 0; i < assetTypeIdsToSearch.Count; i++)
            {
                if (i == (assetTypeIdsToSearch.Count - 1))
                {
                    sql.AppendLine(string.Format("'{0}'", assetTypeIdsToSearch[i].ToString()));
                }
                else
                {
                    sql.AppendLine(string.Format("'{0}',", assetTypeIdsToSearch[i].ToString()));
                }
            }

            sql.AppendLine(") AND [Deleted] IS NULL");

            List<SqlParameter> paramList = new List<SqlParameter> { new SqlParameter("@AssetName", assetName) };

            List<Guid> values = new List<Guid>();
            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), paramList))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    while (rdr.Read())
                    {
                        if (!rdr.IsDBNull(0)) { values.Add(rdr.GetGuid(0)); }
                    }
                }
            }
            return values;
        }

        internal List<Guid> GetIdsByName(string assetName)
        {
            string sql = "SELECT [Id] FROM [Assets] WITH (NoLock) WHERE [Name] = @Name";
            //sql += " AND [Deleted] IS NULL AND [DeletedBy] IS NULL";
            //sql += " AND [Approved] IS NOT NULL AND [ApprovedBy] IS NOT NULL";

            List<SqlParameter> paramList = new List<SqlParameter> { new SqlParameter("@Name", assetName) };

            List<Guid> values = new List<Guid>();

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql, paramList))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    while (rdr.Read())
                    {
                        if (!rdr.IsDBNull(0)) { values.Add(rdr.GetGuid(0)); }
                    }
                }
            }

            return values;
        }

        internal bool MarkAsUpdated(Guid assetid, Guid userId)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("DELETE FROM [dbo].[AssetClients] WHERE [AssetId] = @AssetId");
            sql.AppendLine("UPDATE [Assets] SET [LastModified] = GetDate(), [LastModifiedBy] = @UserId WHERE [Id] = @AssetId");

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@UserId", userId));
            paramList.Add(new SqlParameter("@AssetId", assetid));
            return base.ExecuteInLineSql(sql.ToString(), paramList);
        }

        internal Guid? InstanceOfId(Guid assetId)
        {
            return base.ExecuteScalarGuid(StoredProcs.Asset_GetInstanceOfId, new List<SqlParameter> { new SqlParameter("@Id", assetId) });
        }

        internal Guid AssetTypeId(Guid assetId)
        {
            return base.ExecuteScalarGuid(StoredProcs.Asset_GetAssetTypeId, new List<SqlParameter> { new SqlParameter("@Id", assetId) }).Value;
        }

        internal Guid? ParentId(Guid assetId)
        {
            Guid? returnValue = null;

            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT [FromAssetId] FROM [AssetRelations] WITH (NoLock)");
            sql.AppendLine(string.Format("WHERE [ToAssetId] = '{0}' AND [AssetRelationTypeId] = 0", assetId.ToString()));
            sql.AppendLine("AND [Deleted] IS NULL");

            Guid parentId = base.ExecuteScalarGuidInLine(sql.ToString(), new List<SqlParameter>());

            if (parentId.CompareTo(new Guid()) != 0)
            {
                returnValue = parentId;
            }
            return returnValue;
        }

        /// <summary>
        /// Gets whether or not the asset referenced by the provided assetId is an instance
        /// </summary>
        /// <param name="assetId">id of the asset whose instance status is to be determined</param>
        /// <returns>true if the asset is an instance; false if it is a definition</returns>
        /// <remarks>1.05.06</remarks>
        internal bool IsInstance(Guid assetId)
        {
            Guid? instanceOfId = base.ExecuteScalarGuid(StoredProcs.Asset_GetInstanceOfId, new List<SqlParameter>() { new SqlParameter("@Id", assetId) });

            return instanceOfId.HasValue;
        }

        internal bool Asset_Rename(Guid assetId, string newName)
        {
            string sql = "UPDATE [Assets] SET [Name] = @AssetName WHERE [Id] = @Id";

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@AssetName", newName));
            paramList.Add(new SqlParameter("@Id", assetId));

            return base.ExecuteInLineSql(sql, paramList);
        }

        #region rework area

        internal List<Guid> InstanceOfIds(List<Guid> assetIds)
        {
            List<Guid> values = new List<Guid>();

            List<SqlParameter> paramList = new List<SqlParameter>
                {
                    new SqlParameter("@AssetIds", Helpers.ListOfGuidToCommaDelimString(assetIds))
                };

            using (SqlDataReader rdr = base.OpenDataReader("spr_AssetIds_GetByInstanceOfIds", paramList))
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

        internal bool Assets_ChangeAssetType(Guid instanceOfId, Guid newAssetTypeId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>
                {
                    new SqlParameter("@NewAssetTypeId", newAssetTypeId),
                    new SqlParameter("@InstanceOfId", instanceOfId)
                };
            return base.ExecuteSql(StoredProcs.spAsset_ChangeAssetType, paramList);
        }

        internal Dictionary<Guid, string> GetDictionaryFromPropertyValue(Guid propertyId)
        {

            Dictionary<Guid, string> assets = new Dictionary<Guid, string>();

            List<SqlParameter> paramList = new List<SqlParameter> { new SqlParameter("@PropertyId", propertyId) };

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.spAssets_GetFromPropertyValue, paramList))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    while (rdr.Read())
                    {
                        assets.Add(rdr.GetGuid(0), rdr.GetString(1));
                    }
                }
            }

            return assets;
        }

        internal Dictionary<Guid, string> GetDictionaryFromPropertyValue(Guid assetTypeId, Guid propertyId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@AssetTypeId", assetTypeId));
            paramList.Add(new SqlParameter("@PropertyId", propertyId));

            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.spAssets_GetInPropertyValue, paramList))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    while (rdr.Read())
                    {
                        if ((!rdr.IsDBNull(0)) && (!rdr.IsDBNull(1)))
                        {
                            values.Add(rdr.GetGuid(0), rdr.GetString(1));
                        }
                    }
                }
            }

            return values;
        }

        internal Dictionary<Guid, string> Assets_GetByPropertyValues(Guid assetTypeId, Dictionary<Guid, string> propsAndValues)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT DISTINCT PV.[AssetId], A.[Name]");
            sql.AppendLine("FROM [PropertyValues] PV WITH (NoLock)");
            sql.AppendLine("INNER JOIN [Assets] A WITH (NoLock) ON A.[Id] = [PV].[AssetId]");
            sql.AppendLine("WHERE (");

            int index = 0;
            foreach (KeyValuePair<Guid, string> propVal in propsAndValues)
            {
                sql.AppendLine(string.Format("(PV.[PropertyId] = '{0}' AND PV.[Value] = '{1}')", propVal.Key.ToString(), propVal.Value));
                if (index != (propsAndValues.Count - 1))
                {
                    sql.AppendLine("OR");
                }
                index++;
            }

            sql.AppendLine(") AND PV.[Deleted] IS NULL");
            sql.AppendLine("AND PV.[Approved] IS NOT NULL");
            sql.AppendLine("AND PV.[Rejected] IS NULL");
            sql.AppendLine("AND A.[Deleted] IS NULL");
            sql.AppendLine("AND A.[Approved] IS NOT NULL");
            sql.AppendLine("AND A.[AssetTypeId] = @AssetTypeId");
            sql.AppendLine("ORDER BY [Name]");

            Dictionary<Guid, string> assets = new Dictionary<Guid, string>();

            List<SqlParameter> paramList = new List<SqlParameter> { new SqlParameter("@AssetTypeId", assetTypeId) };

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), paramList))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    while (rdr.Read())
                    {
                        assets.Add(rdr.GetGuid(0), rdr.GetString(1));
                    }
                }
            }

            return assets;
        }

        internal Dictionary<Guid, string> Assets_GetByPropertyValues(Dictionary<Guid, string> propsAndValues)
        {

            Dictionary<Guid, string> assets = new Dictionary<Guid, string>();

            if ((propsAndValues == null) || (propsAndValues.Count == 0)) { return assets; }

            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT DISTINCT A.[Id], A.[Name]");
            sql.AppendLine("FROM [Assets] A WITH (NoLock)");

            int index = 0;
            foreach (KeyValuePair<Guid, string> propAndVal in propsAndValues)
            {
                sql.AppendLine(string.Format("INNER JOIN [PropertyValues] PV{0} WITH (NoLock) ON PV{1}.[AssetId] = A.[Id]", index, index));
                index++;
            }

            sql.AppendLine("WHERE (");

            index = 0;
            foreach (KeyValuePair<Guid, string> propVal in propsAndValues)
            {
                sql.AppendLine(string.Format("(PV{0}.[PropertyId] = '{1}' AND PV{2}.[Value] = '{3}')", index, propVal.Key.ToString(), index, propVal.Value));
                if (index != (propsAndValues.Count - 1))
                {
                    sql.AppendLine("AND");
                }
                index++;
            }

            sql.AppendLine(")");

            index = 0;
            foreach (KeyValuePair<Guid, string> propAndVal in propsAndValues)
            {
                sql.AppendLine(string.Format("AND PV{0}.[Deleted] IS NULL", index));
                sql.AppendLine(string.Format("AND PV{0}.[Approved] IS NOT NULL", index));
                sql.AppendLine(string.Format("AND PV{0}.[Rejected] IS NULL", index));
                index++;
            }

            sql.AppendLine("AND A.[Deleted] IS NULL");
            sql.AppendLine("AND A.[Approved] IS NOT NULL");
            sql.AppendLine("ORDER BY A.[Name]");

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), new List<SqlParameter>()))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    while (rdr.Read())
                    {
                        assets.Add(rdr.GetGuid(0), rdr.GetString(1));
                    }
                }
            }

            return assets;
        }

        /// <summary>
        /// Gets a list of Ids of Assets for the specified AssetType
        /// </summary>
        /// <param name="assetTypeId"></param>
        /// <param name="typeClass"></param>
        /// <param name="includeChildAssetTypes"></param>
        /// <returns></returns>
        /// <remarks>1.05.06</remarks>
        internal List<Guid> AssetIds_Get(Guid assetTypeId, EAssetTypeClass typeClass, bool includeChildAssetTypes)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@AssetTypeId", assetTypeId));
            paramList.Add(new SqlParameter() { ParameterName = "@IsInstance", Value = typeClass == EAssetTypeClass.Instance ? 1 : 0, SqlDbType = SqlDbType.Bit });
            paramList.Add(new SqlParameter() { ParameterName = "@IncludeChildren", Value = includeChildAssetTypes ? 1 : 0, SqlDbType = SqlDbType.Bit });

            List<Guid> values = new List<Guid>();

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.AssetIds_Get, paramList))
            {
                if ((rdr == null) || (!rdr.HasRows)) { return values; }

                while (rdr.Read()) { values.Add(rdr.GetGuid(0)); }
            }

            return values;
        }

        internal List<XObject> Asset_Get(string assetName, bool includeInstances)
        {
            List<XObject> assets = new List<XObject>();

            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT [Id] FROM [Assets] WITH (NoLock) WHERE [Name] = @Name");
            sql.AppendLine("AND ([Deleted] IS NULL) AND ([Approved] IS NOT NULL)");

            if (!includeInstances)
            {
                sql.AppendLine("AND [IsInstance] = 0");
            }

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Name", assetName));

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), paramList))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    while (rdr.Read())
                    {
                        Guid id = rdr.GetGuid(0);
                        assets.Add(this.Get(id));
                    }
                }
            }

            return assets;
        }

        internal Dictionary<Guid, string> Assets_Get(List<Guid> assetIds)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT [Id], IsNull([DisplayValue], [Name]) AS [Name]");
            sql.AppendLine("FROM [Assets] WITH (NoLock)");
            sql.AppendLine("WHERE [Id] IN (");

            int assetCount = assetIds.Count;

            for (int i = 0; i < assetCount; i++)
            {
                if (i == (assetCount - 1))
                {
                    sql.Append("'" + assetIds[i].ToString() + "')");
                }
                else
                {
                    sql.Append("'" + assetIds[i].ToString() + "',");
                }
            }

            sql.AppendLine("AND ([Deleted] IS NULL)");
            sql.AppendLine("AND ([Approved] IS NOT NULL)");
            sql.AppendLine("ORDER BY [Name]");

            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), new List<SqlParameter>()))
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

        internal Dictionary<Guid, string> Assets_GetByInstanceOfId(Guid instanceOfId)
        {

            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@InstanceOfId", instanceOfId));

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.spAssets_GetByInstanceOfId, paramList))
            {
                if (rdr.HasRows)
                {
                    while (rdr.Read())
                    {
                        if ((!rdr.IsDBNull(0)) && (!rdr.IsDBNull(1)))
                        {
                            values.Add(rdr.GetGuid(0), rdr.GetString(1));
                        }
                    }
                }
            }

            return values;
        }

        internal Dictionary<Guid, string> Assets_GetDictionary(List<Guid> assetTypeIds, EAssetRequestType requestType, bool includeDescriptions)
        {

            Dictionary<Guid, string> values = new Dictionary<Guid, string>();
            if ((assetTypeIds == null) || (assetTypeIds.Count == 0)) { return values; }

            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT [Id], IsNull([DisplayValue], [Name]) AS [Name], [Description] FROM [Assets] WITH (NoLock)");
            sql.AppendLine("WHERE [AssetTypeId] IN (");

            for (int i = 0; i < assetTypeIds.Count; i++)
            {
                if (i == (assetTypeIds.Count - 1))
                {
                    sql.AppendLine(string.Format("'{0}'", assetTypeIds[i].ToString()));
                }
                else
                {
                    sql.AppendLine(string.Format("'{0}',", assetTypeIds[i].ToString()));
                }
            }
            sql.AppendLine(") AND ([Deleted] IS NULL) AND ([Approved] IS NOT NULL)");

            switch (requestType)
            {
                case EAssetRequestType.Definition:
                    sql.AppendLine("AND [IsInstance] = 0");
                    break;
                case EAssetRequestType.Instance:
                    sql.AppendLine("AND [IsInstance] = 1");
                    break;
                default:
                    break;
            }

            if (includeDescriptions)
            {
                sql.AppendLine("ORDER BY [Description], [Name]");
            }
            else
            {
                sql.AppendLine("ORDER BY [Name]");
            }

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), new List<SqlParameter>()))
            {
                if (rdr.HasRows)
                {
                    while (rdr.Read())
                    {
                        if ((!rdr.IsDBNull(0)) && (!rdr.IsDBNull(1)))
                        {
                            Guid id = rdr.GetGuid(0);
                            string name = rdr.GetString(1).Trim();

                            if (includeDescriptions)
                            {
                                if (!rdr.IsDBNull(2))
                                {
                                    string desc = rdr.GetString(2).Trim();
                                    if (!string.IsNullOrEmpty(desc))
                                    {
                                        name += " (" + desc + ")";
                                    }
                                }
                            }

                            values.Add(id, name);
                        }
                    }
                }
            }

            return values;
        }

        internal Dictionary<Guid, string> Assets_GetDictionary(Guid assetTypeId, bool includeDescriptions, bool useDisplayValues)
        {
            StringBuilder sql = new StringBuilder();

            if (useDisplayValues)
            {
                sql.AppendLine("SELECT [Id], IsNull([DisplayValue], [Name]) AS [Name], [Description] FROM [Assets] WITH (NoLock) WHERE [AssetTypeId] = @AssetTypeId");
            }
            else
            {
                sql.AppendLine("SELECT [Id], [Name], [Description] FROM [Assets] WITH (NoLock) WHERE [AssetTypeId] = @AssetTypeId");
            }

            sql.AppendLine("AND [Deleted] IS NULL");
            sql.AppendLine("AND [Approved] IS NOT NULL");
            sql.AppendLine("AND [IsInstance] = 0");

            if (includeDescriptions)
            {
                sql.AppendLine("ORDER BY [Description], [Name]");
            }
            else
            {
                sql.AppendLine("ORDER BY [Name]");
            }

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@AssetTypeId", assetTypeId));

            Dictionary<Guid, string> values = new Dictionary<Guid, string>();
            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), paramList))
            {
                if (rdr.HasRows)
                {
                    if (includeDescriptions)
                    {
                        while (rdr.Read())
                        {
                            if ((!rdr.IsDBNull(0)) && (!rdr.IsDBNull(1)))
                            {
                                Guid id = rdr.GetGuid(0);
                                string name = rdr.GetString(1).Trim();

                                if (!rdr.IsDBNull(2))
                                {
                                    string desc = rdr.GetString(2).Trim();
                                    if (!string.IsNullOrEmpty(desc))
                                    {
                                        name += " (" + desc + ")";
                                    }
                                }

                                values.Add(id, name);
                            }
                        }
                    }
                    else
                    {
                        while (rdr.Read())
                        {
                            if ((!rdr.IsDBNull(0)) && (!rdr.IsDBNull(1)))
                            {
                                values.Add(rdr.GetGuid(0), rdr.GetString(1).Trim());
                            }
                        }
                    }

                }
            }

            return values;

        }

        /// <summary>
        /// Returns all asset instances for the provided assetTypeId
        /// Security is not in effect as no UserId is required
        /// </summary>
        /// <param name="assetTypeId"></param>
        /// <returns></returns>
        internal Dictionary<Guid, string> AssetInstances_Get(Guid assetTypeId)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@AssetTypeId", assetTypeId));

            Dictionary<Guid, string> values = new Dictionary<Guid, string>();
            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.spAssetInstances_Get, paramList))
            {
                if ((rdr == null) || (!rdr.HasRows)) { return null; }
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

        internal Dictionary<string, string> Assets_Get(Guid assetTypeId, bool includeChildren, EAssetRequestType requestType)
        {
            Dictionary<string, string> values = new Dictionary<string, string>();

            using (SqlDataReader rdr = base.OpenDataReader("spr_AssetInstances_GetByParentId", new List<SqlParameter>() { new SqlParameter("@AssetTypeId", assetTypeId) }))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    while (rdr.Read())
                    {
                        values.Add(rdr.GetGuid(0).ToString(), rdr.GetString(1));
                    }
                }
            }

            return values;
        }

        /// <summary>
        /// Returns all asset instances for the provided assetTypeIds
        /// Security is not in effect as no UserId is required
        /// </summary>
        /// <param name="assetTypeIds"></param>
        /// <returns></returns>
        internal Dictionary<Guid, string> AssetInstances_Get(List<Guid> assetTypeIds)
        {

            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@AssetTypeIds", Helpers.ListOfGuidToCommaDelimString(assetTypeIds)));

            using (SqlDataReader rdr = base.OpenDataReader("spr_AssetInstances_GetByAssetTypeIds", paramList))
            {
                if (rdr.HasRows)
                {
                    while (rdr.Read())
                    {
                        if ((!rdr.IsDBNull(0)) && (!rdr.IsDBNull(1)))
                        {
                            values.Add(rdr.GetGuid(0), rdr.GetString(1));
                        }
                    }
                }
            }

            return values;
        }

        /// <summary>
        /// Returns all asset instances for which a user has access to within one or more roles
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="assetTypeId"></param>
        /// <param name="roleIds"></param>
        /// <returns></returns>
        internal Dictionary<Guid, string> Assets_Get(Guid userId, Guid parentAssetTypeId, List<Guid> childAssetTypeIds, List<Guid> roleIds, EAssetRequestType requestType)
        {

            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            List<Guid> assetIds = new List<Guid>();

            foreach (Guid roleId in roleIds)
            {
                foreach (Guid assetId in this.Assets_Get(userId, parentAssetTypeId, childAssetTypeIds, roleId, requestType))
                {
                    if (!assetIds.Contains(assetId)) { assetIds.Add(assetId); }
                }
            }

            return this.GetDictionaryByAssetIds(assetIds);
        }

        internal Dictionary<string, Dictionary<Guid, string>> Assets_GetGroupedByPickListValues(List<Guid> assetIds, Guid propertyId)
        {

            Dictionary<string, Dictionary<Guid, string>> values = new Dictionary<string, Dictionary<Guid, string>>();

            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT A.[Id], IsNull(A.[DisplayValue], A.[Name]) AS [Name], IsNull(PLV.[Value], '[No Value]')");
            sql.AppendLine("FROM [Assets] A WITH (NoLock)");
            sql.AppendLine("LEFT JOIN [PropertyValues] PV WITH (NoLock) ON");
            sql.AppendLine("PV.[AssetId] = A.[Id] AND ");
            sql.AppendLine("PV.[PropertyId] = @PropertyId AND");
            sql.AppendLine("PV.[Deleted] IS NULL AND");
            sql.AppendLine("PV.[Approved] IS NOT NULL AND");
            sql.AppendLine("PV.[Value] IS NOT NULL AND");
            sql.AppendLine("PV.[Value] <> ''");
            sql.AppendLine("LEFT JOIN [PickListValues] PLV WITH (NoLock) ON");
            sql.AppendLine("PLV.[Id] = PV.[Value]");
            sql.AppendLine("WHERE");
            sql.AppendLine("A.[Id] IN (");

            for (int i = 0; i < assetIds.Count; i++)
            {
                if (i == (assetIds.Count - 1))
                {
                    sql.AppendLine("'" + assetIds[i].ToString() + "'");
                }
                else
                {
                    sql.AppendLine("'" + assetIds[i].ToString() + "',");
                }
            }

            sql.AppendLine(") ORDER BY PLV.[Value], A.[Name]");

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@PropertyId", propertyId));

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), paramList))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    while (rdr.Read())
                    {
                        if (!rdr.IsDBNull(2))
                        {
                            Guid id = rdr.GetGuid(0);
                            string assetName = rdr.GetString(1);
                            string groupName = rdr.GetString(2);

                            if (values.ContainsKey(groupName))
                            {
                                if (!values[groupName].ContainsKey(id))
                                {
                                    values[groupName].Add(id, assetName);
                                }
                            }
                            else
                            {
                                Dictionary<Guid, string> subValues = new Dictionary<Guid, string>();
                                subValues.Add(id, assetName);
                                values.Add(groupName, subValues);
                            }
                        }
                    }
                }
            }


            return values;
        }

        internal bool MatchesRoleFilters(Guid assetId, string roleViewName)
        {
            string sql = string.Format("SELECT COUNT(*) FROM [{0}] WHERE [AssetId] = '{1}'", roleViewName, assetId);
            return (base.ExecuteScalarInLine(sql, new List<SqlParameter>()) > 0);
        }

        //internal bool MatchesRoleFilters(Guid assetId, Guid assetTypeId, Guid roleId, Guid? userId)
        //{
        //    // TODO: REVISIT!!!
        //    return true;

        //    ////TODO: This is an area which will require revisiting at a later date.
        //    //// The inheritance only attempts to go up one level if the permissions are inherited - this will be a problem later
        //    //RoleAssetTypeRelationDal ratDal = new RoleAssetTypeRelationDal();
        //    //RoleAssetTypeRelation relation = ratDal.RoleAssetTypeRelation_Get(roleId, assetTypeId);
        //    //AssetTypeDal atDal = new AssetTypeDal();

        //    //if (relation != null)
        //    //{

        //    //    while (relation.InheritAccess)
        //    //    {
        //    //        Guid? parentAssetTypeId = atDal.AssetTypeId_GetParent(relation.AssetTypeId);
        //    //        if (parentAssetTypeId.HasValue)
        //    //        {
        //    //            RoleAssetTypeRelation tempRelation = ratDal.RoleAssetTypeRelation_Get(roleId, parentAssetTypeId.Value);
        //    //            if (tempRelation == null)
        //    //            {
        //    //                break;
        //    //            }
        //    //            else
        //    //            {
        //    //                relation = tempRelation;
        //    //            }
        //    //        }
        //    //        else { break; }
        //    //    }

        //    //    // no filters?  well then it matches.
        //    //    if ((relation.Filters == null) || (relation.Filters.Count == 0)) { return true; }

        //    //    StringBuilder sql = new StringBuilder();
        //    //    sql.AppendLine("SELECT COUNT(*)");
        //    //    sql.AppendLine("FROM [Assets] A");

        //    //    for (int i = 0; i < relation.Filters.Count; i++)
        //    //    {
        //    //        sql.AppendLine(string.Format("INNER JOIN [PropertyValues] PV{0} on PV{1}.[AssetId] = A.[Id]", i.ToString(), i.ToString()));
        //    //    }

        //    //    sql.AppendLine("WHERE");

        //    //    Dictionary<int, string> filterStrings = new Dictionary<int, string>();
        //    //    List<string> f = new List<string>();
        //    //    for (int i = 0; i < relation.Filters.Count; i++)
        //    //    {
        //    //        string val = string.Empty;
        //    //        Filter filter = relation.Filters[i];
        //    //        if (filter.IsIncludesFilter)
        //    //        {
        //    //            if (filter.Value == Constants.FilterValues.CurrentUser)
        //    //            {
        //    //                val = string.Format("PV{0}.[PropertyId] = '{1}' AND PV{2}.[Value] = '{3}'", i, filter.PropertyId, i, userId.Value.ToString());
        //    //            }
        //    //            else
        //    //            {
        //    //                val = string.Format("PV{0}.[PropertyId] = '{1}' AND PV{2}.[Value] = '{3}'", i, filter.PropertyId, i, filter.Value);
        //    //            }
        //    //        }
        //    //        else
        //    //        {
        //    //            if (filter.Value == Constants.FilterValues.CurrentUser)
        //    //            {
        //    //                val = string.Format("PV{0}.[PropertyId] = '{1}' AND PV{2}.[Value] != '{3}'", i, filter.PropertyId, i, userId.Value.ToString());
        //    //            }
        //    //            else
        //    //            {
        //    //                val = string.Format("PV{0}.[PropertyId] = '{1}' AND PV{2}.[Value] != '{3}'", i, filter.PropertyId, i, filter.Value);
        //    //            }
        //    //        }
        //    //        filterStrings.Add(i, val);
        //    //        f.Add(val);
        //    //    }

        //    //    sql.AppendLine(string.Format(relation.FilterLogic, f.ToArray()));

        //    //    for (int i = 0; i < relation.Filters.Count; i++)
        //    //    {
        //    //        sql.AppendLine(string.Format("AND PV{0}.[Deleted] IS NULL", i));
        //    //    }

        //    //    sql.AppendLine("AND A.[Id] = @AssetId");
        //    //    sql.AppendLine("AND A.[Deleted] IS NULL");

        //    //    List<SqlParameter> paramList = new List<SqlParameter>();
        //    //    paramList.Add(new SqlParameter("@AssetId", assetId));

        //    //    return base.ExecuteScalarInLine(sql.ToString(), paramList) > 0;
        //    //    //return base.ExecuteScalar(sql.ToString(), paramList) == 1;
        //    //}

        //    //return false;
        //}

        private List<Guid> Assets_Get(Guid userId, Guid parentAssetTypeId, List<Guid> childAssetTypeIds, Guid roleId, EAssetRequestType requestType)
        {

            // correct a potential error condition
            if (childAssetTypeIds == null) { childAssetTypeIds = new List<Guid>(); }

            List<Guid> assetIds = new List<Guid>();

            return assetIds;

            // TODO: REVISIT !!!

            //RoleAssetTypeRelationDal ratDal = new RoleAssetTypeRelationDal();
            //RoleAssetTypeRelation relation = ratDal.RoleAssetTypeRelation_Get(roleId, parentAssetTypeId);

            //if (relation != null)
            //{

            //    StringBuilder sql = new StringBuilder();

            //    sql.AppendLine("SELECT DISTINCT A.[Id]");
            //    sql.AppendLine("FROM [Assets] A WITH (NoLock)");

            //    bool whereAdded = false;

            //    if ((!string.IsNullOrEmpty(relation.FilterLogic)) && (relation.Filters.Count > 0))
            //    {
            //        for (int i = 0; i < relation.Filters.Count; i++)
            //        {
            //            sql.AppendLine(string.Format("INNER JOIN [PropertyValues] PV{0} on PV{1}.[AssetId] = A.[Id]", i.ToString(), i.ToString()));
            //        }
            //        sql.AppendLine("WHERE");
            //        whereAdded = true;
            //    }

            //    Dictionary<int, string> filterStrings = new Dictionary<int, string>();
            //    List<string> f = new List<string>();

            //    for (int i = 0; i < relation.Filters.Count; i++)
            //    {

            //        string val = string.Empty;

            //        Filter filter = relation.Filters[i];

            //        if (filter.IsIncludesFilter)
            //        {
            //            if (filter.Value == Constants.FilterValues.CurrentUser)
            //            {
            //                val = string.Format("PV{0}.[PropertyId] = '{1}' AND PV{2}.[Value] = '{3}'", i, filter.PropertyId, i, userId);
            //            }
            //            else
            //            {
            //                val = string.Format("PV{0}.[PropertyId] = '{1}' AND PV{2}.[Value] = '{3}'", i, filter.PropertyId, i, filter.Value);
            //            }
            //        }
            //        else
            //        {
            //            if (filter.Value == Constants.FilterValues.CurrentUser)
            //            {
            //                val = string.Format("PV{0}.[PropertyId] = '{1}' AND PV{2}.[Value] != '{3}'", i, filter.PropertyId, i, userId);
            //            }
            //            else
            //            {
            //                val = string.Format("PV{0}.[PropertyId] = '{1}' AND PV{2}.[Value] != '{3}'", i, filter.PropertyId, i, filter.Value);
            //            }
            //        }
            //        filterStrings.Add(i, val);
            //        f.Add(val);
            //    }

            //    if ((!string.IsNullOrEmpty(relation.FilterLogic)) && (f.Count > 0))
            //    {
            //        sql.AppendLine(string.Format(relation.FilterLogic, f.ToArray()));
            //        for (int i = 0; i < relation.Filters.Count; i++)
            //        {
            //            sql.AppendLine(string.Format("AND PV{0}.[Deleted] IS NULL", i));
            //        }
            //    }

            //    if (childAssetTypeIds.Count == 0)
            //    {
            //        if (whereAdded)
            //        {
            //            sql.AppendLine("AND A.[AssetTypeId] = @AssetTypeId");
            //        }
            //        else
            //        {
            //            sql.AppendLine("WHERE A.[AssetTypeId] = @AssetTypeId");
            //        }                    
            //    }
            //    else
            //    {

            //        if (whereAdded)
            //        {
            //            sql.AppendLine("AND A.[AssetTypeId] IN (");
            //        }
            //        else
            //        {
            //            sql.AppendLine("WHERE A.[AssetTypeId] IN (");
            //        }


            //        for (int i = 0; i < childAssetTypeIds.Count; i++)
            //        {
            //            if (i == (childAssetTypeIds.Count - 1))
            //            {
            //                sql.AppendFormat("'{0}')", childAssetTypeIds[i].ToString());
            //            }
            //            else
            //            {
            //                sql.AppendFormat("'{0}',", childAssetTypeIds[i].ToString());
            //            }
            //        }

            //    }

            //    sql.AppendLine("AND A.[Deleted] IS NULL");

            //    switch (requestType)
            //    {
            //        case EAssetRequestType.Instance:
            //            sql.AppendLine("AND A.[IsInstance] = 1");
            //            break;
            //        case EAssetRequestType.Definition:
            //            sql.AppendLine("AND A.[IsInstance] = 0");
            //            break;
            //        default:
            //            break;
            //    }

            //    List<SqlParameter> paramList = new List<SqlParameter>();

            //    if (childAssetTypeIds.Count == 0)
            //    {
            //        paramList.Add(new SqlParameter("@AssetTypeId", parentAssetTypeId));
            //    }

            //    using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), paramList))
            //    {
            //        if ((rdr != null) && (rdr.HasRows))
            //        {
            //            while (rdr.Read())
            //            {
            //                assetIds.Add(rdr.GetGuid(0));
            //            }
            //        }
            //    }
            //}

            //return assetIds;
        }

        //internal Dictionary<Guid, string> Assets_GetMatching(List<Guid> assetTypeIds,
        //                                                   string filterExpression,
        //                                                   List<Filter> propsAndValues,
        //                                                   Dictionary<Guid, Property> properties,
        //                                                   EAssetRequestType requestType)
        //{

        //    // TODO: Clean up this freaking mess
        //    Dictionary<Guid, string> values = new Dictionary<Guid, string>();

        //    StringBuilder sql = new StringBuilder();
        //    sql.AppendLine("SELECT DISTINCT A.[Id], ISNULL(A.[DisplayValue], A.[Name]) AS [Name]");
        //    sql.AppendLine("FROM [Assets] A WITH (NoLock)");

        //    List<SqlParameter> paramList = new List<SqlParameter>();

        //    StringBuilder joins = new StringBuilder();

        //    ArrayList wheres = new ArrayList();

        //    int index = 0;
        //    foreach (Filter filter in propsAndValues)
        //    {

        //        Property prop = properties[filter.PropertyId];

        //        string i = index.ToString();

        //        if (!prop.IsSystem)
        //        {
        //            if (filter.FromInstanceOfAsset)
        //            {
        //                joins.AppendLine("INNER JOIN [PropertyValues] PV" + i + " WITH (NoLock) ON PV" + i + ".[AssetId] = A.[InstanceOfId]");
        //            }
        //            else
        //            {
        //                joins.AppendLine("INNER JOIN [PropertyValues] PV" + i + " WITH (NoLock) ON PV" + i + ".[AssetId] = A.[Id]");
        //            }

        //            joins.AppendFormat("\tAND PV{0}.[PropertyId] = @Prop{1}", i, i);
        //            joins.AppendFormat("{0}\tAND PV{1}.[Deleted] IS NULL", Environment.NewLine, i);
        //            joins.AppendFormat("{0}\tAND PV{1}.[Approved] IS NOT NULL", Environment.NewLine, i);
        //            joins.AppendFormat("{0}\tAND PV{1}.[Value] <> ''", Environment.NewLine, i);
        //            joins.AppendFormat("{0}\tAND PV{1}.[Value] IS NOT NULL{2}", Environment.NewLine, i, Environment.NewLine);
        //        }

        //        switch (prop.DataType)
        //        {
        //            case EDataType.Date:

        //                if (prop.IsSystem)
        //                {
        //                    switch (prop.SystemType)
        //                    {
        //                        case ESystemType.Created:
        //                            wheres.Add(string.Format("A.[Created] {0} '{1}'", filter.Operator, filter.Value));
        //                            break;
        //                        case ESystemType.Deleted:
        //                            wheres.Add(string.Format("A.[Deleted] {0} '{1}'", filter.Operator, filter.Value));
        //                            break;
        //                        case ESystemType.LastModified:
        //                            wheres.Add(string.Format("A.[LastModified] {0} '{1}'", filter.Operator, filter.Value));
        //                            break;
        //                    }
        //                }
        //                else
        //                {

        //                    string targetDate = string.Empty;

        //                    Guid datePartId;
        //                    if (Guid.TryParse(filter.Value, out datePartId))
        //                    {
        //                        // the value is a DatePart
        //                        DatePart dPart = new DatePartDal().DatePart_Get(new Guid(filter.Value));

        //                        DateTime now = DateTime.Now;

        //                        switch (dPart.DisplayValue)
        //                        {
        //                            case "Today":
        //                                targetDate = now.ToShortDateString();
        //                                break;
        //                            case "Tommorrow":
        //                                targetDate = now.AddDays(1).ToShortDateString();
        //                                break;
        //                            case "Yesterday":
        //                                targetDate = now.AddDays(-1).ToShortDateString();
        //                                break;
        //                            case "YTD":
        //                                targetDate = new DateTime(now.Year, 1, 1).ToShortDateString();
        //                                break;
        //                            default:
        //                                switch (dPart.SqlDatePart)
        //                                {
        //                                    case "day":
        //                                        targetDate = now.AddDays(double.Parse(dPart.Value)).ToShortDateString();
        //                                        break;
        //                                    case "year":
        //                                        targetDate = now.AddYears(int.Parse(dPart.Value)).ToShortDateString();
        //                                        break;
        //                                }
        //                                break;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        targetDate = filter.Value;
        //                    }

        //                    wheres.Add(string.Format("(CAST(PV" + i + ".[Value] AS DATE)) {0} (CAST('{1}' AS DATE))", filter.Operator, targetDate));
        //                }

        //                break;
        //            case EDataType.Asset:
        //                if ((prop.IsSystem) && (prop.SystemType == ESystemType.InstanceOf))
        //                {
        //                    wheres.Add(string.Format("A.[InstanceOfId] {0} '{1}'", filter.Operator, filter.Value));
        //                }
        //                else
        //                {
        //                    wheres.Add(string.Format("PV" + i + ".[Value] {0} '{1}'", filter.Operator, filter.Value));
        //                }
        //                break;
        //            case EDataType.String:
        //                if (prop.IsSystem)
        //                {
        //                    switch (prop.SystemType)
        //                    {
        //                        case ESystemType.AssetType:
        //                            wheres.Add(string.Format("A.[AssetTypeId] {0} '{1}'", filter.Operator, filter.Value));
        //                            break;
        //                        default:
        //                            wheres.Add(string.Format("PV" + i + ".[Value] {0} '{1}'", filter.Operator, filter.Value));
        //                            break;
        //                    }
        //                }
        //                else
        //                {
        //                    wheres.Add(string.Format("PV" + i + ".[Value] {0} '{1}'", filter.Operator, filter.Value));
        //                }
        //                break;
        //            case EDataType.User:
        //                if (prop.IsSystem)
        //                {
        //                    switch (prop.SystemType)
        //                    {
        //                        case ESystemType.CreatedBy:
        //                            wheres.Add(string.Format("A.[CreatedBy] {0} '{1}'", filter.Operator, filter.Value));
        //                            break;
        //                        case ESystemType.DeletedBy:
        //                            wheres.Add(string.Format("A.[DeletedBy] {0} '{1}'", filter.Operator, filter.Value));
        //                            break;
        //                        case ESystemType.LastModifiedBy:
        //                            wheres.Add(string.Format("A.[LastModifiedBy] {0} '{1}'", filter.Operator, filter.Value));
        //                            break;
        //                    }
        //                }
        //                else
        //                {
        //                    wheres.Add(string.Format("PV" + i + ".[Value] {0} '{1}'", filter.Operator, filter.Value));
        //                }
        //                break;
        //            default:
        //                wheres.Add(string.Format("PV" + i + ".[Value] {0} '{1}'", filter.Operator, filter.Value));
        //                break;
        //        }

        //        paramList.Add(new SqlParameter("@Prop" + i, filter.PropertyId));

        //        index++;
        //    }

        //    // build the final sql
        //    StringBuilder finalSql = new StringBuilder();
        //    finalSql.Append(sql.ToString());
        //    finalSql.Append(joins.ToString());
        //    finalSql.AppendLine("WHERE");
        //    finalSql.AppendLine("A.[AssetTypeId] IN (");

        //    int atIndex = 0;
        //    int assetTypeCount = assetTypeIds.Count;

        //    foreach (Guid assetTypeId in assetTypeIds)
        //    {
        //        if (atIndex == (assetTypeCount - 1))
        //        {
        //            finalSql.AppendFormat("'{0}') AND", assetTypeId);
        //        }
        //        else
        //        {
        //            finalSql.AppendFormat("'{0}',", assetTypeId);
        //        }
        //        atIndex++;
        //    }

        //    finalSql.AppendLine(string.Format(filterExpression, wheres.ToArray()));
        //    finalSql.AppendLine("AND (A.[Deleted] IS NULL)");

        //    finalSql.AppendLine("ORDER BY [Name]");

        //    using (SqlDataReader rdr = base.OpenDataReaderInLine(finalSql.ToString(), paramList))
        //    {
        //        if ((rdr != null) && (rdr.HasRows))
        //        {
        //            while (rdr.Read())
        //            {
        //                values.Add(rdr.GetGuid(0), rdr.GetString(1));
        //            }
        //        }
        //    }

        //    return values;

        //}

        internal Dictionary<Guid, string> Assets_GetMatching(List<Guid> assetIds, Guid assetTypeId, bool includeDescriptions)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT [Id], [Name], [Description]");
            sql.AppendLine("FROM [Assets] WITH (NoLock)");
            sql.AppendLine("WHERE [Id] IN (");

            for (int i = 0; i < assetIds.Count; i++)
            {
                if (i == (assetIds.Count - 1))
                {
                    sql.AppendLine(string.Format("'{0}')", assetIds[i].ToString()));
                }
                else
                {
                    sql.AppendLine(string.Format("'{0}',", assetIds[i].ToString()));
                }
            }

            sql.AppendLine("AND [AssetTypeId] = @AssetTypeId");
            sql.AppendLine("AND [Deleted] IS NULL");
            sql.AppendLine("AND [Approved] IS NOT NULL");
            sql.AppendLine("ORDER BY [Name]");

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@AssetTypeId", assetTypeId));

            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), paramList))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    while (rdr.Read())
                    {
                        Guid id = rdr.GetGuid(0);
                        string name = rdr.GetString(1);
                        string desc = rdr.IsDBNull(2) ? string.Empty : rdr.GetString(2);

                        if ((includeDescriptions) && (!string.IsNullOrEmpty(desc)))
                        {
                            values.Add(id, string.Format("{0} ({1})", name, desc));
                        }
                        else
                        {
                            values.Add(id, name);
                        }
                    }
                }
            }

            return values;
        }

        internal Dictionary<Guid, string> Assets_GetMatching(List<Guid> assetIds, Guid propertyId, string propertyValue, bool includeDescriptions, bool useInstanceOfValues, bool exactMatch)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT A.[Id], IsNull(A.[DisplayValue], A.[Name]) AS [Name], A.[Description]");
            sql.AppendLine("FROM [Assets] A WITH (NoLock)");

            if (useInstanceOfValues)
            {
                sql.AppendLine("INNER JOIN [PropertyValues] PV WITH (NoLock) ON PV.[AssetId] = A.[InstanceOfId]");
            }
            else
            {
                sql.AppendLine("INNER JOIN [PropertyValues] PV WITH (NoLock) ON PV.[AssetId] = A.[Id]");
            }

            sql.AppendLine("WHERE A.[Id] IN (");

            for (int i = 0; i < assetIds.Count; i++)
            {
                if (i == (assetIds.Count - 1))
                {
                    sql.AppendLine("'" + assetIds[i].ToString() + "')");
                }
                else
                {
                    sql.AppendLine("'" + assetIds[i].ToString() + "',");
                }
            }

            sql.AppendLine("AND PV.[PropertyId] = @PropertyId");

            if (exactMatch)
            {
                sql.AppendLine("AND PV.[Value] = @Value");
            }
            else
            {
                sql.AppendLine("AND PV.[Value] like @PropertyValue");
            }

            sql.AppendLine("AND PV.[Deleted] IS NULL");
            sql.AppendLine("AND PV.[Approved] IS NOT NULL");
            sql.AppendLine("AND PV.[Rejected] IS NULL");
            sql.AppendLine("AND A.[Deleted] IS NULL");
            sql.AppendLine("AND A.[Approved] IS NOT NULL");
            sql.AppendLine("ORDER BY [Name]");

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@PropertyId", propertyId));
            if (exactMatch) { paramList.Add(new SqlParameter("@Value", propertyValue)); }

            List<SqlParameter> valueParams = new List<SqlParameter>();
            valueParams.Add(new SqlParameter("@PropertyValue", propertyValue));

            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), paramList, valueParams))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    while (rdr.Read())
                    {
                        if ((!rdr.IsDBNull(0)) && (!rdr.IsDBNull(1)))
                        {
                            Guid id = rdr.GetGuid(0);
                            string name = rdr.GetString(1);
                            string desc = string.Empty;

                            if ((includeDescriptions) && (!rdr.IsDBNull(2)))
                            {
                                desc = rdr.GetString(2);
                                if (!string.IsNullOrEmpty(desc))
                                {
                                    values.Add(id, name + " (" + desc + ")");
                                }
                            }
                            else
                            {
                                values.Add(id, name);
                            }
                        }
                    }
                }
            }

            return values;

        }

        internal Dictionary<Guid, string> Assets_GetMatching(List<Guid> assetIds, ESystemType systemType, string value)
        {

            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT DISTINCT A.[Id], ISNULL(A.[DisplayValue], A.[Name]) AS [Name]");
            sql.AppendLine("FROM [Assets] A WITH (NoLock)");
            sql.AppendLine("WHERE A.[Id] IN (");

            for (int i = 0; i < assetIds.Count; i++)
            {
                if (i == assetIds.Count - 1)
                {
                    sql.AppendLine("'" + assetIds[i].ToString() + "')");
                }
                else
                {
                    sql.AppendLine("'" + assetIds[i].ToString() + "',");
                }
            }

            switch (systemType)
            {
                case ESystemType.CreatedBy:
                    sql.AppendLine("AND A.[CreatedBy] = @UserId");
                    break;
                case ESystemType.DeletedBy:
                    sql.AppendLine("AND A.[DeletedBy] = @UserId");
                    break;
                case ESystemType.LastModifiedBy:
                    sql.AppendLine("AND A.[LastModifiedBy] = @UserId");
                    break;
            }

            if ((systemType != ESystemType.DeletedBy) && (systemType != ESystemType.Deleted))
            {
                sql.AppendLine("AND A.[Deleted] IS NULL AND A.[DeletedBy] IS NULL");
            }

            sql.AppendLine("ORDER BY [Name]");

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@UserId", value));

            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), paramList))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    while (rdr.Read())
                    {
                        if ((!rdr.IsDBNull(0)) && (!rdr.IsDBNull(1)))
                        {
                            values.Add(rdr.GetGuid(0), rdr.GetString(1));
                        }
                    }
                }
            }

            return values;
        }

        /// <summary>
        /// This uses the newly generated tables
        /// </summary>
        /// <param name="assetTypeId"></param>
        /// <param name="requestType"></param>
        /// <param name="searchTerm">optional</param>
        /// <returns></returns>
        internal Dictionary<Guid, string> GetDictionary(string tableName, string assetTypeName, string searchTerm)
        {
            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            string sql = string.Empty;

            if (string.IsNullOrEmpty(searchTerm))
            {
                sql = string.Format("SELECT [AssetId], [Name] AS [{0}] FROM [{1}] WITH (NoLock) ORDER BY [{2}]", assetTypeName, tableName, assetTypeName);
            }
            else
            {
                sql = string.Format("SELECT [AssetId], [Name] AS [{0}] FROM [{1}] WITH (NoLock) WHERE [Name] LIKE '%{2}%' ORDER BY [{3}]", assetTypeName, tableName, searchTerm, assetTypeName);
            }

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

        internal Dictionary<Guid, string> GetDictionaryByNames(List<string> assetNames, List<Guid> assetTypeIds, EAssetRequestType requestType)
        {
            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@AssetNames", assetNames));
            paramList.Add(new SqlParameter("@AssetTypeIds", Helpers.ListOfGuidToCommaDelimString(assetTypeIds)));
            paramList.Add(new SqlParameter("@IsInstance", (requestType == EAssetRequestType.Instance)));

            using (SqlDataReader rdr = base.OpenDataReader("spr_Assets_GetDictionaryByNames", paramList))
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

        private Dictionary<Guid, string> GetDictionaryByAssetIds(List<Guid> assetIds)
        {

            Dictionary<Guid, string> assetNames = new Dictionary<Guid, string>();

            if ((assetIds != null) && (assetIds.Count > 0))
            {
                StringBuilder sql = new StringBuilder();
                sql.AppendLine("SELECT");
                sql.AppendLine("[Id],");
                sql.AppendLine("IsNull([DisplayValue], [Name]) AS [Name]");
                sql.AppendLine("FROM [Assets] WITH (NoLock)");
                sql.AppendLine("WHERE [Id] IN (");

                for (int i = 0; i < assetIds.Count; i++)
                {
                    if (i == (assetIds.Count - 1))
                    {
                        sql.AppendLine(string.Format("'{0}')", assetIds[i].ToString()));
                    }
                    else
                    {
                        sql.AppendLine(string.Format("'{0}',", assetIds[i].ToString()));
                    }
                }

                sql.AppendLine("ORDER BY [Name]");

                using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), new List<SqlParameter>()))
                {
                    if ((rdr != null) && (rdr.HasRows))
                    {
                        while (rdr.Read())
                        {
                            assetNames.Add(rdr.GetGuid(0), rdr.GetString(1));
                        }
                    }
                }
            }

            return assetNames;

        }

        internal XObject GetParent(Guid assetId)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT [FromAssetId] FROM [AssetRelations] WITH (NoLock) WHERE [ToAssetId] = @ChildId");
            sql.AppendLine("AND [Deleted] IS NULL");
            sql.AppendLine("AND [Approved] IS NOT NULL");
            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@ChildId", assetId));

            Guid parentId = base.ExecuteScalarGuidInLine(sql.ToString(), paramList);

            if (parentId != new Guid())
            {
                return this.Get(parentId);
            }

            return null;

        }

        internal bool Kill(Guid assetId)
        {
            return base.ExecuteSql(StoredProcs.Asset_Kill, new List<SqlParameter>() { new SqlParameter("@AssetId", assetId) });
        }

        internal Dictionary<Guid, string> Asset_GetChildren(Guid parentAssetId)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT");
            sql.AppendLine("A.[Id] AS [Id],");
            sql.AppendLine("IsNull(A.DisplayValue, A.[Name]) AS [Name]");
            sql.AppendLine("FROM [Assets] A WITH (NoLock)");
            sql.AppendLine("INNER JOIN [AssetRelations] AR WITH (NoLock) ON AR.[ToAssetId] = A.[Id]");
            sql.AppendLine("WHERE AR.[FromAssetId] = @ParentId");
            sql.AppendLine("AND A.[Deleted] IS NULL");
            sql.AppendLine("AND A.[Approved] IS NOT NULL");
            sql.AppendLine("AND AR.[Deleted] IS NULL");
            sql.AppendLine("AND AR.[AssetRelationTypeId] = 0");
            sql.AppendLine("ORDER BY A.[Name]");

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@ParentId", parentAssetId));

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

        #endregion

    }

}