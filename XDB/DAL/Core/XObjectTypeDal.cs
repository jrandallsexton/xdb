
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
    internal class XObjectTypeDal : XBaseDal
    {

        public XObjectTypeDal() : base(ECommonObjectType.XObjectType) { }

        internal XObjectType Get(Guid id)
        {

            XObjectType assetType = null;

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.AssetType_Get, new List<SqlParameter>() { new SqlParameter("@Id", id) }))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return null; }

                assetType = new XObjectType();
                assetType.Id = id;

                rdr.Read();

                int name = rdr.GetOrdinal("Name");
                int parentId = rdr.GetOrdinal("ParentId");
                int allowAssets = rdr.GetOrdinal("AllowAssets");
                int isSystem = rdr.GetOrdinal("IsSystem");
                int plural = rdr.GetOrdinal("Pluralization");
                int defLabel = rdr.GetOrdinal("DefinitionLabel");
                int defLabelP = rdr.GetOrdinal("DefinitionLabelPlural");
                int insLabel = rdr.GetOrdinal("InstanceLabel");
                int insLabelP = rdr.GetOrdinal("InstanceLabelPlural");
                int created = rdr.GetOrdinal("Created");
                int createdBy = rdr.GetOrdinal("CreatedBy");
                int lastModified = rdr.GetOrdinal("LastModified");
                int lastModifiedBy = rdr.GetOrdinal("LastModifiedBy");
                int deleted = rdr.GetOrdinal("Deleted");
                int deletedBy = rdr.GetOrdinal("DeletedBy");

                assetType.Name = rdr.GetString(name);

                if (!rdr.IsDBNull(parentId)) { assetType.ParentId = rdr.GetGuid(parentId); }

                if (!rdr.IsDBNull(allowAssets)) { assetType.AllowAssets = (bool)rdr.GetValue(allowAssets); }

                if (!rdr.IsDBNull(isSystem)) { assetType.IsSystem = (bool)rdr.GetValue(isSystem); }

                if (!rdr.IsDBNull(plural)) { assetType.Plural = rdr.GetString(plural); }

                if (!rdr.IsDBNull(defLabel)) { assetType.DefinitionLabel = rdr.GetString(defLabel); }
                if (!rdr.IsDBNull(defLabelP)) { assetType.DefinitionLabel = rdr.GetString(defLabelP); }

                if (!rdr.IsDBNull(insLabel)) { assetType.InstanceLabel = rdr.GetString(insLabel); }
                if (!rdr.IsDBNull(insLabelP)) { assetType.InstanceLabel = rdr.GetString(insLabelP); }

                assetType.Created = rdr.GetDateTime(created);
                assetType.CreatedBy = rdr.GetGuid(createdBy);

                if (!rdr.IsDBNull(lastModified)) assetType.LastModified = rdr.GetDateTime(lastModified);
                if (!rdr.IsDBNull(lastModifiedBy)) assetType.LastModifiedBy = rdr.GetGuid(lastModifiedBy);

                if (!rdr.IsDBNull(deleted)) assetType.Deleted = rdr.GetDateTime(deleted);
                if (!rdr.IsDBNull(deletedBy)) assetType.DeletedBy = rdr.GetGuid(deletedBy);

                //foreach (var relation in new AssetTypePropertyRelationDal().GetByAssetTypeId(id)) { assetType.Properties.Add(relation); }

                //assetType.Properties.IsDirty = false;
                //assetType.Properties.IsNew = false;

                assetType.IsNew = false;
                assetType.IsDirty = false;

            }

            return assetType;

        }

        /// <summary>
        /// v1.5.0.6
        /// </summary>
        /// <param name="assetTypeId"></param>
        /// <returns></returns>
        internal List<Guid> Children(Guid assetTypeId)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", assetTypeId));

            List<Guid> values = new List<Guid>();

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.AssetType_GetChildDictionary, paramList))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return values; }

                while (rdr.Read()) { values.Add(rdr.GetGuid(0)); }

            }

            return values;
        }

        internal bool AllowAssets(Guid assetTypeId)
        {
            string sql = string.Format("SELECT [AllowAssets] FROM [AssetTypes] WHERE [Id] = '{0}'", assetTypeId);

            return base.ExecuteScalarBool(sql);
        }

        internal bool Save(XObjectType objectType, Guid userId)
        {

            if ((!objectType.IsDirty) || (!objectType.IsNew)) { return true; }

            List<SqlParameter> paramList = new List<SqlParameter>();

            paramList.Add(new SqlParameter("@Id", objectType.Id));
            paramList.Add(new SqlParameter("@Name", objectType.Name));
            paramList.Add(new SqlParameter() { ParameterName = "@ParentId", Value = objectType.ParentId.HasValue ? objectType.ParentId : null });
            paramList.Add(new SqlParameter("@AllowAssets", objectType.AllowAssets));
            paramList.Add(new SqlParameter("@IsSystem", objectType.IsSystem));

            paramList.Add(new SqlParameter() { ParameterName = "@Pluralization", Value = string.IsNullOrEmpty(objectType.Plural) ? null : objectType.Plural });
            paramList.Add(new SqlParameter() { ParameterName = "@DefinitionLabel", Value = string.IsNullOrEmpty(objectType.DefinitionLabel) ? null : objectType.DefinitionLabel });
            paramList.Add(new SqlParameter() { ParameterName = "@DefinitionLabelP", Value = string.IsNullOrEmpty(objectType.DefinitionLabelPlural) ? null : objectType.DefinitionLabelPlural });
            paramList.Add(new SqlParameter() { ParameterName = "@InstanceLabel", Value = string.IsNullOrEmpty(objectType.InstanceLabel) ? null : objectType.InstanceLabel });
            paramList.Add(new SqlParameter() { ParameterName = "@InstanceLabelP", Value = string.IsNullOrEmpty(objectType.InstanceLabelPlural) ? null : objectType.InstanceLabelPlural });

            paramList.Add(new SqlParameter("@Created", objectType.Created));
            paramList.Add(new SqlParameter("@CreatedBy", objectType.CreatedBy));

            paramList.Add(new SqlParameter() { ParameterName = "@LastModified", Value = objectType.LastModified.HasValue ? objectType.LastModified : null });
            paramList.Add(new SqlParameter() { ParameterName = "@LastModifiedBy", Value = objectType.LastModifiedBy.HasValue ? objectType.LastModifiedBy : null });

            paramList.Add(new SqlParameter() { ParameterName = "@Deleted", Value = objectType.Deleted.HasValue ? objectType.Deleted : null });
            paramList.Add(new SqlParameter() { ParameterName = "@DeletedBy", Value = objectType.DeletedBy.HasValue ? objectType.DeletedBy : null });

            return base.ExecuteSql(StoredProcs.ObjectType_Save, paramList);

        }

        /// <summary>
        /// Gets a dictionary of AssetType Ids and Names
        /// </summary>
        /// <param name="includePlaceholders">whether or not "placeholder" AssetTypes should be included (i.e. AllowAssets field)</param>
        /// <returns></returns>
        internal Dictionary<Guid, string> GetDictionary(bool includePlaceholders)
        {

            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            List<SqlParameter> paramList = new List<SqlParameter>() { new SqlParameter("@IncludePlaceholders", includePlaceholders) };

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.AssetTypes_GetDictionary, paramList))
            {
                if ((rdr == null) || (!rdr.HasRows)) { return values; }

                while (rdr.Read()) { values.Add(rdr.GetGuid(0), rdr.GetString(1)); }
            }

            return values;

        }

        internal Dictionary<Guid, string> AssetTypes_GetDictionary(Guid parentAssetTypeId)
        {

            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", parentAssetTypeId));

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.AssetType_GetChildDictionary, paramList))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return values; }

                while (rdr.Read())
                {
                    values.Add(rdr.GetGuid(0), rdr.GetString(1));
                }

            }

            return values;
        }

        public Guid? ParentId(Guid assetTypeId)
        {

            string sql = "SELECT [ParentId] FROM [AssetTypes] WHERE [Id] = @Id";

            Guid parentId = base.ExecuteScalarGuidInLine(sql, new List<SqlParameter>() { new SqlParameter("@Id", assetTypeId) });

            if (parentId == Guid.Empty)
            {
                return null;
            }
            else
            {
                return parentId;
            }
        }

        internal Guid GetIdByAssetId(Guid assetId)
        {
            return base.ExecuteScalarGuidInLine("SELECT [AssetTypeId] FROM [Assets] WITH (NoLock) WHERE [Id] = @Id", new List<SqlParameter>() { new SqlParameter("@Id", assetId) });
        }

        internal Dictionary<Guid, string> AssetTypes_GetParents()
        {

            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.AssetTypes_GetParentDictionary, new List<SqlParameter>()))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return values; }

                while (rdr.Read()) { values.Add(rdr.GetGuid(0), rdr.GetString(1)); }

            }

            return values;
        }

        internal string Pluralization(Guid assetTypeId)
        {
            string sql = "SELECT ISNULL([Pluralization], [Name]) AS [Plural] FROM [AssetTypes] WITH (NoLock) WHERE [Id] = @Id";

            return base.ExecuteScalarStringInLine(sql, new List<SqlParameter>() { new SqlParameter("@Id", assetTypeId) });
        }

        internal bool HasAssets(Guid assetTypeId, EAssetRequestType requestType)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@AssetTypeId", assetTypeId));
            paramList.Add(new SqlParameter("@IsInstance", (requestType == EAssetRequestType.Instance)));

            return base.ExecuteScalar(StoredProcs.AssetType_HasAssets, paramList) > 0;

        }

        internal string ReportingLabel(Guid assetTypeId, bool forInstances, bool usePlural)
        {
            string sql = string.Empty;

            if (forInstances)
            {
                if (usePlural)
                {
                    sql = "SELECT [InstanceLabelPlural] FROM [AssetTypes] WITH (NoLock) WHERE [Id] = @Id AND [Deleted] IS NULL";
                }
                else
                {
                    sql = "SELECT [InstanceLabel] FROM [AssetTypes] WITH (NoLock) WHERE [Id] = @Id AND [Deleted] IS NULL";
                }
            }
            else
            {
                if (usePlural)
                {
                    sql = "SELECT [DefinitionLabelPlural] FROM [AssetTypes] WITH (NoLock) WHERE [Id] = @Id AND [Deleted] IS NULL";
                }
                else
                {
                    sql = "SELECT [DefinitionLabel] FROM [AssetTypes] WITH (NoLock) WHERE [Id] = @Id AND [Deleted] IS NULL";
                }
            }

            return base.ExecuteScalarStringInLine(sql, new List<SqlParameter>() { new SqlParameter("@Id", assetTypeId) });

        }

    }

}