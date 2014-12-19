
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
using XDB.Common.SqlDal;

using XDB.Models;

namespace XDB.Repositories
{

    public class XObjectTypePropertyRepository : XSqlDal
    {

        public XObjectTypePropertyRepository() { }

        public XObjectTypePropertyRepository(string connString) { this.ConnectionString = connString; }

        #region private members

        //Collection SPs
        private const string spAssetTypePropertyRelationList_Get = "spr_AssetTypePropertyRelationList_Get";
        private const string spAssetTypePropertyRelationList_Save = "spr_AssetTypePropertyRelationList_Save";
        private const string spAssetTypePropertyRelationList_Delete = "spr_AssetTypePropertyRelationList_Delete";

        //Collection by FK SPs
        private const string spAssetTypePropertyRelationList_DeleteByAssetTypeId = "spr_AssetTypePropertyRelationList_DeleteByAssetTypeId";

        //Collection by FK SPs
        private const string spAssetTypePropertyRelationList_GetByPropertyId = "spr_AssetTypePropertyRelationList_GetByPropertyId";
        private const string spAssetTypePropertyRelationList_DeleteByPropertyId = "spr_AssetTypePropertyRelationList_DeletePropertyId";

        private const string spAssetTypePropertyRelationList_GetByAssetTypeIdsAndPropertyId = "spr_AssetTypePropertyRelationList_GetByAssetTypeIdsAndPropertyId";

        #endregion

        private XObjectTypeProperty Get(Guid id)
        {

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.AssetTypePropertyRelation_Get, new List<SqlParameter>() { new SqlParameter("@Id", id) }))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return null; }

                rdr.Read();

                XObjectTypeProperty relation = new XObjectTypeProperty();

                if (!rdr.IsDBNull(rdr.GetOrdinal("AssetTypeId"))) relation.AssetTypeId = (Guid)rdr[rdr.GetOrdinal("AssetTypeId")];

                if (!rdr.IsDBNull(rdr.GetOrdinal("PropertyId"))) relation.PropertyId = (Guid)rdr[rdr.GetOrdinal("PropertyId")];

                if (!rdr.IsDBNull(rdr.GetOrdinal("IsInstance"))) relation.IsInstance = (bool)rdr[rdr.GetOrdinal("IsInstance")];

                if (!rdr.IsDBNull(rdr.GetOrdinal("IsInheritedValue"))) relation.IsInheritedValue = (bool)rdr[rdr.GetOrdinal("IsInheritedValue")];

                if (!rdr.IsDBNull(rdr.GetOrdinal("Created"))) relation.Created = (DateTime)rdr[rdr.GetOrdinal("Created")];

                if (!rdr.IsDBNull(rdr.GetOrdinal("CreatedBy"))) relation.CreatedBy = (Guid)rdr[rdr.GetOrdinal("CreatedBy")];

                if (!rdr.IsDBNull(rdr.GetOrdinal("Deleted"))) relation.Deleted = (DateTime)rdr[rdr.GetOrdinal("Deleted")];

                if (!rdr.IsDBNull(rdr.GetOrdinal("DeletedBy"))) relation.DeletedBy = (Guid)rdr[rdr.GetOrdinal("DeletedBy")];

                relation.Id = id;
                relation.IsNew = false;
                relation.IsDirty = false;

                return relation;

            }

        }

        public XObjectTypeProperty AssetTypePropertyRelation_Get(Guid assetTypeId, Guid propertyId)
        {
            string sql = "SELECT [Id] FROM [AssetTypesProperties] WHERE [AssetTypeId] = @AssetTypeId AND [PropertyId] = @PropertyId AND [Deleted] IS NULL";

            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@AssetTypeId", assetTypeId),
                new SqlParameter("@PropertyId", propertyId)
            };

            Guid relationId = base.ExecuteScalarGuidInLine(sql, paramList);

            return this.Get(relationId);
        }

        private bool Save(XObjectTypeProperty relation)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", relation.Id));
            paramList.Add(new SqlParameter("@AssetTypeId", relation.AssetTypeId));
            paramList.Add(new SqlParameter("@PropertyId", relation.PropertyId));
            paramList.Add(new SqlParameter("@IsInstance", relation.IsInstance));
            paramList.Add(new SqlParameter("@IsInheritedValue", relation.IsInheritedValue));
            paramList.Add(new SqlParameter("@Created", relation.Created));
            paramList.Add(new SqlParameter("@CreatedBy", relation.CreatedBy));

            paramList.Add(new SqlParameter() { ParameterName = "@Deleted", Value = relation.Deleted.HasValue ? relation.Deleted : null });
            paramList.Add(new SqlParameter() { ParameterName = "@DeletedBy", Value = relation.DeletedBy.HasValue ? relation.DeletedBy : null });

            if (base.ExecuteSql(StoredProcs.AssetTypePropertyRelation_Save, paramList))
            {

                relation.IsNew = false;
                relation.IsDirty = false;

                return true;

            }
            else
            {
                return false;
            }

        }

        private List<XObjectTypeProperty> GetCollectionFromReader(string spName, List<SqlParameter> paramList)
        {

            List<XObjectTypeProperty> list = new List<XObjectTypeProperty>();

            using (SqlDataReader rdr = base.OpenDataReader(spName, paramList))
            {

                if ((rdr != null) && (rdr.HasRows))
                {
                    int Id = rdr.GetOrdinal("Id");
                    int AssetTypeId = rdr.GetOrdinal("AssetTypeId");
                    int PropertyId = rdr.GetOrdinal("PropertyId");
                    int IsInstance = rdr.GetOrdinal("IsInstance");
                    int IsInheritedValue = rdr.GetOrdinal("IsInheritedValue");
                    int Created = rdr.GetOrdinal("Created");
                    int CreatedBy = rdr.GetOrdinal("CreatedBy");

                    while (rdr.Read())
                    {
                        XObjectTypeProperty relation = new XObjectTypeProperty();

                        if (!rdr.IsDBNull(Id)) relation.Id = rdr.GetGuid(Id);

                        if (!rdr.IsDBNull(AssetTypeId)) relation.AssetTypeId = rdr.GetGuid(AssetTypeId);

                        if (!rdr.IsDBNull(PropertyId)) relation.PropertyId = rdr.GetGuid(PropertyId);

                        if (!rdr.IsDBNull(IsInstance)) relation.IsInstance = (bool)rdr.GetValue(IsInstance);

                        if (!rdr.IsDBNull(IsInheritedValue)) relation.IsInheritedValue = (bool)rdr.GetValue(IsInheritedValue);

                        if (!rdr.IsDBNull(Created)) relation.Created = rdr.GetDateTime(Created);

                        if (!rdr.IsDBNull(CreatedBy)) relation.CreatedBy = rdr.GetGuid(CreatedBy);

                        relation.IsNew = false;

                        relation.IsDirty = false;

                        list.Add(relation);
                    }
                }

            }

            return list;
        }

        internal List<XObjectTypeProperty> GetCollectionByAssetTypeIdAndPropertyIds(Guid assetTypeId, List<Guid> propertyIds)
        {

            if (assetTypeId == Guid.Empty) { return new List<XObjectTypeProperty>(); }
            if ((propertyIds == null) || (propertyIds.Count == 0)) { return new List<XObjectTypeProperty>(); }

            StringBuilder temp = new StringBuilder();
            foreach (Guid id in propertyIds) { temp.AppendFormat("{0},", id); }

            String final = temp.ToString();
            final = final.Remove(final.Length - 1);

            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@AssetTypeId", assetTypeId),
                new SqlParameter("@PropertyIds", final)
            };

            return this.GetCollectionFromReader(StoredProcs.AssetTypePropertyRelations_GetByAssetTypeIdAndPropertyIds, paramList);
        }

        internal List<XObjectTypeProperty> GetCollectionByAssetTypeIdsAndPropertyId(List<Guid> assetTypeIds, Guid propertyId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>();

            StringBuilder temp = new StringBuilder();
            foreach (Guid id in assetTypeIds)
            {
                temp.AppendFormat("{0},", id);
            }

            String final = temp.ToString();
            final = final.Remove(final.Length - 1);

            paramList.Add(new SqlParameter("@AssetTypeIds", final));
            paramList.Add(new SqlParameter("@PropertyId", propertyId));

            return this.GetCollectionFromReader(spAssetTypePropertyRelationList_GetByAssetTypeIdsAndPropertyId, paramList);

        }

        public List<XObjectTypeProperty> GetByAssetTypeId(Guid assetTypeId)
        {

            List<XObjectTypeProperty> list = new List<XObjectTypeProperty>();

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.AssetTypePropertyRelations_Get, new List<SqlParameter>() { new SqlParameter("@AssetTypeId", assetTypeId) }))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return list; }

                int Id = rdr.GetOrdinal("Id");
                int AssetTypeId = rdr.GetOrdinal("AssetTypeId");
                int PropertyId = rdr.GetOrdinal("PropertyId");
                int IsInstance = rdr.GetOrdinal("IsInstance");
                int IsInherited = rdr.GetOrdinal("IsInherited");
                int IsInheritedValue = rdr.GetOrdinal("IsInheritedValue");
                int Created = rdr.GetOrdinal("Created");
                int CreatedBy = rdr.GetOrdinal("CreatedBy");

                while (rdr.Read())
                {
                    XObjectTypeProperty relation = new XObjectTypeProperty();

                    if (!rdr.IsDBNull(Id)) relation.Id = rdr.GetGuid(Id);

                    if (!rdr.IsDBNull(AssetTypeId)) relation.AssetTypeId = rdr.GetGuid(AssetTypeId);

                    if (!rdr.IsDBNull(PropertyId)) relation.PropertyId = rdr.GetGuid(PropertyId);

                    if (!rdr.IsDBNull(IsInstance)) relation.IsInstance = (bool)rdr.GetValue(IsInstance);

                    if (!rdr.IsDBNull(IsInherited)) relation.IsInherited = (bool)rdr.GetValue(IsInherited);

                    if (!rdr.IsDBNull(IsInheritedValue)) relation.IsInheritedValue = (bool)rdr.GetValue(IsInheritedValue);

                    if (!rdr.IsDBNull(Created)) relation.Created = rdr.GetDateTime(Created);

                    if (!rdr.IsDBNull(CreatedBy)) relation.CreatedBy = rdr.GetGuid(CreatedBy);

                    relation.IsNew = false;

                    relation.IsDirty = false;

                    list.Add(relation);
                }

            }

            return list;

        }

        public bool Save(List<XObjectTypeProperty> relations)
        {
            foreach (XObjectTypeProperty relation in relations)
            {
                if (relation.IsDirty) { this.Save(relation); }
            }

            return true;
        }

        public bool Exists(Guid assetTypeId, Guid propertyId, bool isInstance)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT COUNT(*) FROM [AssetTypesProperties]");
            sql.AppendLine("WHERE [AssetTypeId] = @AssetTypeId");
            sql.AppendLine("AND [PropertyId] = @PropertyId");
            sql.AppendLine("AND [IsInstance] = @IsInstance");
            sql.AppendLine("AND [Deleted] IS NULL AND [DeletedBy] IS NULL");

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@AssetTypeId", assetTypeId));
            paramList.Add(new SqlParameter("@PropertyId", propertyId));
            paramList.Add(new SqlParameter("@IsInstance", isInstance));

            return (base.ExecuteScalarInLine(sql.ToString(), paramList) != 0);
        }

    }

}