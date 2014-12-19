
using System;
using System.Text;
using System.Collections.Generic;
using System.Data.SqlClient;

using XDB.UI.Constants;
using XDB.DAL;
using XDB.DataObjects;
using XDB.Enumerations;

using XDB.UI.DataObjects;

namespace XDB.UI.DAL
{

    internal class PropertyGroupDal : XSqlDal
    {

        public PropertyGroupDal() { }

        public PropertyGroupDal(string connString) { this.ConnectionString = connString; }

        private const string spPropertyGroup_Get = "spr_PropertyGroup_Get";
        private const string spPropertyGroup_Kill = "spr_PropertyGroup_Kill";
        private const string spPropertyGroup_Save = "spr_PropertyGroup_Save";
        private const string spPropertyGroup_Delete = "spr_PropertyGroup_Delete";

        //Collection SPs
        private const string spPropertyGroupList_GetByViewId = "spr_PropertyGroupList_GetByViewId";
        private const string spPropertyGroupList_Save = "spr_PropertyGroupList_Save";
        private const string spPropertyGroupList_Delete = "spr_PropertyGroupList_Delete";

        /// <summary>
        /// Gets a PropertyGroup instance based on the supplied id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal XPropertyGroup Get(Guid id)
        {

            XPropertyGroup propGroup = null;

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", id));

            using (SqlDataReader rdr = base.OpenDataReader(spPropertyGroup_Get, paramList))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return null; }

                propGroup = new XPropertyGroup();
                propGroup.Id = id;

                rdr.Read();

                int name = rdr.GetOrdinal("Name");
                int display = rdr.GetOrdinal("DisplayValue");
                int atId = rdr.GetOrdinal("AssetTypeId");
                int isInstance = rdr.GetOrdinal("IsInstance");
                int created = rdr.GetOrdinal("Created");
                int lastMod = rdr.GetOrdinal("LastModified");
                int deleted = rdr.GetOrdinal("Deleted");
                int createdBy = rdr.GetOrdinal("CreatedBy");
                int lastModBy = rdr.GetOrdinal("LastModifiedBy");
                int deletedBy = rdr.GetOrdinal("DeletedBy");

                if (!rdr.IsDBNull(name)) { propGroup.Name = rdr.GetString(name); }

                if (!rdr.IsDBNull(display)) { propGroup.DisplayValue = rdr.GetString(display); }

                if (!rdr.IsDBNull(atId)) { propGroup.AssetTypeId = rdr.GetGuid(atId); }

                if (!rdr.IsDBNull(isInstance)) { propGroup.IsInstance = (bool)rdr.GetValue(isInstance); }

                if (!rdr.IsDBNull(created)) { propGroup.Created = rdr.GetDateTime(created); }

                if (!rdr.IsDBNull(lastMod)) { propGroup.LastModified = rdr.GetDateTime(lastMod); }

                if (!rdr.IsDBNull(deleted)) { propGroup.Deleted = rdr.GetDateTime(deleted); }

                if (!rdr.IsDBNull(createdBy)) { propGroup.CreatedBy = rdr.GetGuid(createdBy); }

                if (!rdr.IsDBNull(lastModBy)) { propGroup.LastModifiedBy = rdr.GetGuid(lastModBy); }

                if (!rdr.IsDBNull(deletedBy)) { propGroup.DeletedBy = rdr.GetGuid(deletedBy); }

                //PropertyGroupPropertyRelationDal relationDal = new PropertyGroupPropertyRelationDal();
                //PropertyDal pDal = new PropertyDal();

                //foreach (PropertyGroupPropertyRelation relation in relationDal.PropertyGroupPropertyRelationList_GetByPropertyGroupId(id))
                //{
                //    propGroup.AddPropertyMember(relation);

                //    Property prop = pDal.Get(relation.PropertyId);
                //    prop.IsRequired = relation.IsRequired;
                //    prop.IsReadOnly = relation.IsReadOnly;
                //    prop.DefaultValue = relation.DefaultValue;

                //    propGroup.Properties.Add(prop);
                //}

                //propGroup.Properties.IsNew = false;
                //propGroup.Properties.IsDirty = false;

                propGroup.IsNew = false;
                propGroup.IsDirty = false;

                return propGroup;

            }

        }

        internal bool IsValidId(Guid id)
        {
            string sql = "SELECT COUNT(*) FROM [PropertyGroups] WITH (NoLock) WHERE [Id] = @Id AND [Deleted] IS NULL";
            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", id));
            return base.ExecuteScalarInLine(sql, paramList) == 1;
        }

        internal bool Kill(Guid id)
        {
            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", id));
            return base.ExecuteSql(spPropertyGroup_Kill, paramList);
        }

        /// <summary>
        /// Saves a property group
        /// </summary>
        /// <param name="propertyGroup"></param>
        /// <returns>true if successful; false otherwise</returns>
        internal bool Save(XPropertyGroup propertyGroup)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();

            if (propertyGroup.IsDirty)
            {

                paramList.Add(new SqlParameter("@Id", propertyGroup.Id));
                paramList.Add(new SqlParameter("@Name", propertyGroup.Name));
                paramList.Add(new SqlParameter("@DisplayValue", propertyGroup.DisplayValue));
                paramList.Add(new SqlParameter("@AssetTypeId", propertyGroup.AssetTypeId));
                paramList.Add(new SqlParameter("@IsInstance", propertyGroup.IsInstance));
                paramList.Add(new SqlParameter("@Created", propertyGroup.Created));
                paramList.Add(new SqlParameter("@LastModified", propertyGroup.LastModified));
                paramList.Add(new SqlParameter("@Deleted", propertyGroup.Deleted));
                paramList.Add(new SqlParameter("@CreatedBy", propertyGroup.CreatedBy));
                paramList.Add(new SqlParameter("@LastModifiedBy", propertyGroup.LastModifiedBy));
                paramList.Add(new SqlParameter("@DeletedBy", propertyGroup.DeletedBy));

                if (base.ExecuteSql(spPropertyGroup_Save, paramList))
                {
                    if (new PropertyGroupPropertyRelationDal(this.ConnectionString).PropertyGroupPropertyRelationList_Save(propertyGroup.PropertyMembers))
                    {
                        propertyGroup.IsNew = false;
                        propertyGroup.IsDirty = false;

                        return true;
                    }
                }

                return false;

            }

            return true;

        }

        /// <summary>
        /// Deletes a PropertyGroup instance based on the supplied id
        /// </summary>
        /// <param name="id">id of the property group to delete</param>
        /// <param name="userId">id of the user deleting this object</param>
        /// <returns>true if successful; false otherwise</returns>
        internal bool Delete(Guid id, Guid userId)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", id));
            paramList.Add(new SqlParameter("@DeletedBy", userId));

            return base.ExecuteSql(spPropertyGroup_Delete, paramList);

        }

        internal Dictionary<Guid, string> PropertyGroups_GetDictionary(bool includeDeleted, Guid? assetTypeId, Guid? pickListId)
        {
            bool includeDisplayValues = true;

            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT [Id], ");

            if (includeDisplayValues)
            {
                sql.AppendLine("[Name] + ' (' + [DisplayValue] + ')' FROM [PropertyGroups] WITH (NoLock)");
            }
            else
            {
                sql.AppendLine("[Name] FROM [PropertyGroups] WITH (NoLock)");
            }

            bool whereAdded = false;

            if (assetTypeId.HasValue)
            {
                sql.AppendLine("WHERE [AssetTypeId] = @AssetTypeId");
                whereAdded = true;
            }
            else if (pickListId.HasValue)
            {
                sql.AppendLine("WHERE [PickListId] = @PickListId");
                whereAdded = true;
            }

            if (!includeDeleted)
            {
                if (!whereAdded)
                {
                    sql.AppendLine("WHERE [Deleted] IS NULL");
                }
                else
                {
                    sql.AppendLine("AND [Deleted] IS NULL");
                }
            }

            sql.AppendLine("ORDER BY [Name]");

            List<SqlParameter> paramList = new List<SqlParameter>();

            if (assetTypeId.HasValue)
            {
                paramList.Add(new SqlParameter("@AssetTypeId", assetTypeId.Value));
            }
            else if (pickListId.HasValue)
            {
                paramList.Add(new SqlParameter("@PickListId", pickListId.Value));
            }

            Dictionary<Guid, string> groups = new Dictionary<Guid, string>();

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), paramList))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    while (rdr.Read())
                    {
                        if ((!rdr.IsDBNull(0)) && (!rdr.IsDBNull(1)))
                        {
                            groups.Add(rdr.GetGuid(0), rdr.GetString(1));
                        }
                    }
                }
            }

            return groups;

        }

        internal Dictionary<Guid, string> GetDictionaryByPropertyId(Guid propertyId)
        {

            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT PG.[Id], IsNull(PG.[DisplayValue], PG.[Name]) FROM [PropertyGroups] PG WITH (NoLock)");
            sql.AppendLine("INNER JOIN [PropertyGroupsProperties] PGP WITH (NoLock) ON PGP.[PropertyGroupId] = PG.[Id]");
            sql.AppendLine("INNER JOIN [Properties] P WITH (NoLock) ON P.Id = PGP.[PropertyId]");
            sql.AppendLine("WHERE PG.[Deleted] IS NULL AND");
            sql.AppendLine("PGP.[Deleted] IS NULL AND");
            sql.AppendLine("P.[Deleted] IS NULL AND");
            sql.AppendLine("P.[Id] = @PropertyId");
            sql.AppendLine("ORDER BY PG.[Name]");

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@PropertyId", propertyId));

            Dictionary<Guid, string> groups = new Dictionary<Guid, string>();

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), paramList))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    while (rdr.Read())
                    {
                        groups.Add(rdr.GetGuid(0), rdr.GetString(1));
                    }
                }
            }

            return groups;
        }

        internal List<XPropertyGroup> GetCollectionByViewId(Guid viewId)
        {

            //PropertyDal propDal = new PropertyDal();

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@ViewId", viewId));

            List<XPropertyGroup> values = new List<XPropertyGroup>();

            using (SqlDataReader rdr = base.OpenDataReader(spPropertyGroupList_GetByViewId, paramList))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return values; }

                int id = rdr.GetOrdinal("Id");
                int name = rdr.GetOrdinal("Name");
                int display = rdr.GetOrdinal("DisplayValue");
                int atId = rdr.GetOrdinal("AssetTypeId");
                int isInstance = rdr.GetOrdinal("IsInstance");
                int created = rdr.GetOrdinal("Created");
                int lastMod = rdr.GetOrdinal("LastModified");
                int createdBy = rdr.GetOrdinal("CreatedBy");
                int lastModBy = rdr.GetOrdinal("LastModifiedBy");

                while (rdr.Read())
                {
                    XPropertyGroup propGroup = new XPropertyGroup();

                    propGroup.Id = rdr.GetGuid(id);

                    if (!rdr.IsDBNull(name)) { propGroup.Name = rdr.GetString(name); }

                    if (!rdr.IsDBNull(display)) { propGroup.DisplayValue = rdr.GetString(display); }

                    propGroup.AssetTypeId = rdr.GetGuid(atId);

                    if (!rdr.IsDBNull(isInstance)) { propGroup.IsInstance = (bool)rdr.GetValue(isInstance); }

                    propGroup.Created = rdr.GetDateTime(created);

                    if (!rdr.IsDBNull(lastMod)) { propGroup.LastModified = rdr.GetDateTime(lastMod); }

                    propGroup.CreatedBy = rdr.GetGuid(createdBy);

                    if (!rdr.IsDBNull(lastModBy)) { propGroup.LastModifiedBy = rdr.GetGuid(lastModBy); }

                    //PropertyGroupPropertyRelationDal relationDal = new PropertyGroupPropertyRelationDal();
                    //PropertyDal pDal = new PropertyDal();

                    //foreach (PropertyGroupPropertyRelation relation in relationDal.PropertyGroupPropertyRelationList_GetByPropertyGroupId(propGroup.Id))
                    //{
                    //    propGroup.AddPropertyMember(relation);
                    //}

                    propGroup.IsNew = false;
                    propGroup.IsDirty = false;

                    values.Add(propGroup);
                }

                //foreach (PropertyGroup pg in values)
                //{
                //    pg.Properties = propDal.GetCollectionByPropertyGroupId(pg.Id);
                //}

            }

            return values;

        }

    }

}