
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Common;
using XDB.Common.Constants;
using XDB.Common.Interfaces;
using XDB.Common.Enumerations;

using XDB.Models;

namespace XDB.Repositories
{

    public class XObjectTypeRepository<T> : XBaseDal, IXObjectTypeRepository<T> where T : XBase, IXObjectType
    {

        public XObjectTypeRepository() : base(ECommonObjectType.XObjectType) { }

        public IXObjectType Get(Guid id)
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
        public IList<Guid> Children(Guid assetTypeId)
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

        public bool AllowAssets(Guid assetTypeId)
        {
            string sql = string.Format("SELECT [AllowAssets] FROM [AssetTypes] WHERE [Id] = '{0}'", assetTypeId);

            return base.ExecuteScalarBool(sql);
        }

        public void Save(T xObjectType)
        {

            if ((!xObjectType.IsDirty) || (!xObjectType.IsNew)) { return; }

            List<SqlParameter> paramList = new List<SqlParameter>();

            paramList.Add(new SqlParameter("@Id", xObjectType.Id));
            paramList.Add(new SqlParameter("@Name", xObjectType.Name));
            paramList.Add(new SqlParameter() { ParameterName = "@ParentId", Value = xObjectType.ParentId.HasValue ? xObjectType.ParentId : null });
            paramList.Add(new SqlParameter("@AllowAssets", xObjectType.AllowAssets));
            paramList.Add(new SqlParameter("@IsSystem", xObjectType.IsSystem));

            paramList.Add(new SqlParameter() { ParameterName = "@Pluralization", Value = string.IsNullOrEmpty(xObjectType.Plural) ? null : xObjectType.Plural });
            paramList.Add(new SqlParameter() { ParameterName = "@DefinitionLabel", Value = string.IsNullOrEmpty(xObjectType.DefinitionLabel) ? null : xObjectType.DefinitionLabel });
            paramList.Add(new SqlParameter() { ParameterName = "@DefinitionLabelP", Value = string.IsNullOrEmpty(xObjectType.DefinitionLabelPlural) ? null : xObjectType.DefinitionLabelPlural });
            paramList.Add(new SqlParameter() { ParameterName = "@InstanceLabel", Value = string.IsNullOrEmpty(xObjectType.InstanceLabel) ? null : xObjectType.InstanceLabel });
            paramList.Add(new SqlParameter() { ParameterName = "@InstanceLabelP", Value = string.IsNullOrEmpty(xObjectType.InstanceLabelPlural) ? null : xObjectType.InstanceLabelPlural });

            paramList.Add(new SqlParameter("@Created", xObjectType.Created));
            paramList.Add(new SqlParameter("@CreatedBy", xObjectType.CreatedBy));

            paramList.Add(new SqlParameter() { ParameterName = "@LastModified", Value = xObjectType.LastModified.HasValue ? xObjectType.LastModified : null });
            paramList.Add(new SqlParameter() { ParameterName = "@LastModifiedBy", Value = xObjectType.LastModifiedBy.HasValue ? xObjectType.LastModifiedBy : null });

            paramList.Add(new SqlParameter() { ParameterName = "@Deleted", Value = xObjectType.Deleted.HasValue ? xObjectType.Deleted : null });
            paramList.Add(new SqlParameter() { ParameterName = "@DeletedBy", Value = xObjectType.DeletedBy.HasValue ? xObjectType.DeletedBy : null });

            base.ExecuteSql(StoredProcs.ObjectType_Save, paramList);

        }

        /// <summary>
        /// Gets a dictionary of AssetType Ids and Names
        /// </summary>
        /// <param name="includePlaceholders">whether or not "placeholder" AssetTypes should be included (i.e. AllowAssets field)</param>
        /// <returns></returns>
        public IDictionary<Guid, string> GetDictionary(bool includePlaceholders)
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

        public IDictionary<Guid, string> AssetTypes_GetDictionary(Guid parentAssetTypeId)
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

        public Guid GetIdByAssetId(Guid assetId)
        {
            return base.ExecuteScalarGuidInLine("SELECT [AssetTypeId] FROM [Assets] WITH (NoLock) WHERE [Id] = @Id", new List<SqlParameter>() { new SqlParameter("@Id", assetId) });
        }

        public IDictionary<Guid, string> AssetTypes_GetParents()
        {

            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.AssetTypes_GetParentDictionary, new List<SqlParameter>()))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return values; }

                while (rdr.Read()) { values.Add(rdr.GetGuid(0), rdr.GetString(1)); }

            }

            return values;
        }

        public string Pluralization(Guid assetTypeId)
        {
            string sql = "SELECT ISNULL([Pluralization], [Name]) AS [Plural] FROM [AssetTypes] WITH (NoLock) WHERE [Id] = @Id";

            return base.ExecuteScalarStringInLine(sql, new List<SqlParameter>() { new SqlParameter("@Id", assetTypeId) });
        }

        public bool HasAssets(Guid assetTypeId, EXObjectRequestType requestType)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@AssetTypeId", assetTypeId));
            paramList.Add(new SqlParameter("@IsInstance", (requestType == EXObjectRequestType.Instance)));

            return base.ExecuteScalar(StoredProcs.AssetType_HasAssets, paramList) > 0;

        }

        public string ReportingLabel(Guid assetTypeId, bool forInstances, bool usePlural)
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