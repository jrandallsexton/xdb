
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

    internal class XPropertyDal : XBaseDal
    {

        public XPropertyDal() : base(ECommonObjectType.XProperty) { }

        internal XProperty Get(Guid id)
        {

            XProperty property = null;

            List<SqlParameter> paramList = paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", id));

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.Property_Get, paramList))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return null; }

                int Name = rdr.GetOrdinal("Name");
                int DisplayValue = rdr.GetOrdinal("DisplayValue");
                int Description = rdr.GetOrdinal("Description");
                int DataTypeId = rdr.GetOrdinal("DataTypeId");
                int IsSystem = rdr.GetOrdinal("IsSystem");
                int SystemTypeId = rdr.GetOrdinal("SystemTypeId");
                int Precision = rdr.GetOrdinal("Precision");
                int PickListId = rdr.GetOrdinal("PickListId");
                int RoleId = rdr.GetOrdinal("RoleId");
                int IsOrdered = rdr.GetOrdinal("IsOrdered");
                int AllowMultiValue = rdr.GetOrdinal("AllowMultiValue");
                int Singular = rdr.GetOrdinal("Singular");
                int Plural = rdr.GetOrdinal("Plural");
                int AssetTypeId = rdr.GetOrdinal("AssetTypeId");
                int AssetTypeIsInstance = rdr.GetOrdinal("AssetTypeIsInstance");
                int Created = rdr.GetOrdinal("Created");
                int CreatedBy = rdr.GetOrdinal("CreatedBy");
                //int Approved = rdr.GetOrdinal("Approved");
                //int ApprovedBy = rdr.GetOrdinal("ApprovedBy");
                int LastModified = rdr.GetOrdinal("LastModified");
                int LastModifiedBy = rdr.GetOrdinal("LastModifiedBy");
                int Deleted = rdr.GetOrdinal("Deleted");
                int DeletedBy = rdr.GetOrdinal("DeletedBy");

                property = new XProperty();

                rdr.Read();

                property.Id = id;

                if (!rdr.IsDBNull(Name)) { property.Name = rdr.GetString(Name); }

                if (!rdr.IsDBNull(DisplayValue)) { property.DisplayValue = rdr.GetString(DisplayValue); }

                if (!rdr.IsDBNull(Description)) { property.Description = rdr.GetString(Description); }

                if (!rdr.IsDBNull(DataTypeId)) { property.DataType = EnumerationOps.EDataTypeFromValue((Int16)rdr.GetByte(DataTypeId)); }

                if (!rdr.IsDBNull(IsSystem)) { property.IsSystem = (bool)rdr[IsSystem]; }

                if (!rdr.IsDBNull(SystemTypeId)) { property.SystemType = EnumerationOps.ESystemTypeFromValue(rdr.GetInt32(SystemTypeId)); }

                if (!rdr.IsDBNull(Precision)) { property.Precision = (Int16)rdr[Precision]; }

                if (!rdr.IsDBNull(PickListId)) { property.PickListId = rdr.GetGuid(PickListId); }

                if (!rdr.IsDBNull(RoleId)) { property.RoleId = rdr.GetGuid(RoleId); }

                if (!rdr.IsDBNull(IsOrdered)) { property.IsOrdered = (bool)rdr[IsOrdered]; }

                if (!rdr.IsDBNull(AllowMultiValue)) { property.AllowMultiValue = (bool)rdr[AllowMultiValue]; }

                if (!rdr.IsDBNull(Singular)) { property.Singular = rdr.GetString(Singular); }

                if (!rdr.IsDBNull(Plural)) { property.Plural = rdr.GetString(Plural); }

                if (!rdr.IsDBNull(AssetTypeId)) { property.AssetTypeId = rdr.GetGuid(AssetTypeId); }

                if (!rdr.IsDBNull(AssetTypeIsInstance)) { property.AssetTypeIsInstance = (bool)rdr[AssetTypeIsInstance]; }

                property.Created = rdr.GetDateTime(Created);
                property.CreatedBy = rdr.GetGuid(CreatedBy);

                if (!rdr.IsDBNull(LastModified)) { property.LastModified = rdr.GetDateTime(LastModified); }
                if (!rdr.IsDBNull(LastModifiedBy)) { property.LastModifiedBy = rdr.GetGuid(LastModifiedBy); }

                if (!rdr.IsDBNull(Deleted)) { property.Deleted = rdr.GetDateTime(Deleted); }
                if (!rdr.IsDBNull(DeletedBy)) { property.DeletedBy = rdr.GetGuid(DeletedBy); }

                property.IsNew = false;
                property.IsDirty = false;

            }

            return property;

        }

        internal bool Save(XProperty property)
        {

            if ((!property.IsDirty) || (!property.IsNew)) { return true; }

            List<SqlParameter> paramList = new List<SqlParameter>();

            paramList.Add(new SqlParameter("@Id", property.Id));
            paramList.Add(new SqlParameter("@DataTypeId", property.DataType.GetHashCode()));
            paramList.Add(new SqlParameter("@Precision", property.Precision));

            paramList.Add(new SqlParameter() { ParameterName = "@PickListId", Value = property.PickListId.HasValue ? property.PickListId : null });

            paramList.Add(new SqlParameter("@AllowMultiValue", property.AllowMultiValue));
            paramList.Add(new SqlParameter("@IsOrdered", property.IsOrdered));
            paramList.Add(new SqlParameter("@Name", property.Name));

            paramList.Add(new SqlParameter() { ParameterName = "@DisplayValue", Value = string.IsNullOrEmpty(property.DisplayValue) ? null : property.DisplayValue });
            paramList.Add(new SqlParameter() { ParameterName = "@Description", Value = string.IsNullOrEmpty(property.Description) ? null : property.Description });

            paramList.Add(new SqlParameter("@AssetTypeId", property.AssetTypeId));
            paramList.Add(new SqlParameter("@AssetTypeIsInstance", property.AssetTypeIsInstance));

            paramList.Add(new SqlParameter("@Created", property.Created));
            paramList.Add(new SqlParameter("@CreatedBy", property.CreatedBy));

            paramList.Add(new SqlParameter() { ParameterName = "@LastModified", Value = property.LastModified.HasValue ? property.LastModified : null });
            paramList.Add(new SqlParameter() { ParameterName = "@LastModifiedBy", Value = property.LastModifiedBy.HasValue ? property.LastModifiedBy : null });

            paramList.Add(new SqlParameter() { ParameterName = "@Deleted", Value = property.Deleted.HasValue ? property.Deleted : null });
            paramList.Add(new SqlParameter() { ParameterName = "@DeletedBy", Value = property.DeletedBy.HasValue ? property.DeletedBy : null });

            paramList.Add(new SqlParameter("@RoleId", property.RoleId));
            paramList.Add(new SqlParameter("@IsSystem", property.IsSystem));
            paramList.Add(new SqlParameter("@SystemTypeId", property.SystemType.GetHashCode()));

            if (base.ExecuteSql(StoredProcs.Property_Save, paramList))
            {

                property.IsNew = false;
                property.IsDirty = false;

                return true;

            }

            return false;

        }

        /// <summary>
        /// Deletes a property instance/record matching the specified id
        /// </summary>
        /// <param name="propertyId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        internal bool Delete(Guid propertyId, Guid userId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>
                    {
                        new SqlParameter("@Id", propertyId),
                        new SqlParameter("@DeletedBy", userId)
                    };

            return base.ExecuteSql(StoredProcs.Property_Delete, paramList);

        }

        internal EDataType DataType(Guid propertyId)
        {
            string sql = "SELECT [DataTypeId] FROM [Properties] WITH (NoLock) WHERE [Id] = @Id";
            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", propertyId.ToString()));

            int value = base.ExecuteScalarInLine(sql, paramList);

            if (value > -1)
            {
                return (EDataType)Enum.Parse(typeof(EDataType), value.ToString());
            }
            else
            {
                return EDataType.Undefined;
            }
        }

        internal ESystemType SystemType(Guid propertyId)
        {
            string sql = "SELECT [SystemTypeId] FROM [Properties] WITH (NoLock) WHERE [Id] = @Id";
            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", propertyId.ToString()));

            int value = base.ExecuteScalarInLine(sql, paramList);

            if (value > -1)
            {
                return (ESystemType)Enum.Parse(typeof(ESystemType), value.ToString());
            }
            else
            {
                return ESystemType.NotApplicable;
            }
        }

        internal Guid GetIdByName(string propertyName)
        {
            string sql = "SELECT [Id] FROM [Properties] WITH (NoLock) WHERE [Name] = @Name";
            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Name", propertyName));
            return base.ExecuteScalarGuidInLine(sql, paramList);
        }

        internal Dictionary<Guid, XProperty> GetObjectDictionary(List<Guid> propertyIds)
        {
            string propIds = Helpers.ListOfGuidToCommaDelimString(propertyIds);

            Dictionary<Guid, XProperty> values = new Dictionary<Guid, XProperty>();

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@PropertyIds", propIds));

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.Properties_GetObjectDictionary, paramList))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return values; }

                int Id = rdr.GetOrdinal("Id");
                int Name = rdr.GetOrdinal("Name");
                int DisplayValue = rdr.GetOrdinal("DisplayValue");
                int Description = rdr.GetOrdinal("Description");
                int DataTypeId = rdr.GetOrdinal("DataTypeId");
                int IsSystem = rdr.GetOrdinal("IsSystem");
                int SystemTypeId = rdr.GetOrdinal("SystemTypeId");
                int Precision = rdr.GetOrdinal("Precision");
                int PickListId = rdr.GetOrdinal("PickListId");
                int RoleId = rdr.GetOrdinal("RoleId");
                int IsOrdered = rdr.GetOrdinal("IsOrdered");
                int AllowMultiValue = rdr.GetOrdinal("AllowMultiValue");
                int Singular = rdr.GetOrdinal("Singular");
                int Plural = rdr.GetOrdinal("Plural");
                int AssetTypeId = rdr.GetOrdinal("AssetTypeId");
                int AssetTypeIsInstance = rdr.GetOrdinal("AssetTypeIsInstance");
                int Created = rdr.GetOrdinal("Created");
                int CreatedBy = rdr.GetOrdinal("CreatedBy");
                //int Approved = rdr.GetOrdinal("Approved");
                //int ApprovedBy = rdr.GetOrdinal("ApprovedBy");
                int LastModified = rdr.GetOrdinal("LastModified");
                int LastModifiedBy = rdr.GetOrdinal("LastModifiedBy");
                int Deleted = rdr.GetOrdinal("Deleted");
                int DeletedBy = rdr.GetOrdinal("DeletedBy");

                while (rdr.Read())
                {

                    XProperty property = new XProperty();

                    property.Id = rdr.GetGuid(Id);

                    if (!rdr.IsDBNull(Name)) { property.Name = rdr.GetString(Name); }

                    if (!rdr.IsDBNull(DisplayValue)) { property.DisplayValue = rdr.GetString(DisplayValue); }

                    if (!rdr.IsDBNull(Description)) { property.Description = rdr.GetString(Description); }

                    if (!rdr.IsDBNull(DataTypeId)) { property.DataType = EnumerationOps.EDataTypeFromValue((Int16)rdr.GetByte(DataTypeId)); }

                    if (!rdr.IsDBNull(IsSystem)) { property.IsSystem = (bool)rdr[IsSystem]; }

                    if (!rdr.IsDBNull(SystemTypeId)) { property.SystemType = EnumerationOps.ESystemTypeFromValue(rdr.GetInt32(SystemTypeId)); }

                    if (!rdr.IsDBNull(Precision)) { property.Precision = (Int16)rdr[Precision]; }

                    if (!rdr.IsDBNull(PickListId)) { property.PickListId = rdr.GetGuid(PickListId); }

                    if (!rdr.IsDBNull(RoleId)) { property.RoleId = rdr.GetGuid(RoleId); }

                    if (!rdr.IsDBNull(IsOrdered)) { property.IsOrdered = (bool)rdr[IsOrdered]; }

                    if (!rdr.IsDBNull(AllowMultiValue)) { property.AllowMultiValue = (bool)rdr[AllowMultiValue]; }

                    if (!rdr.IsDBNull(Singular)) { property.Singular = rdr.GetString(Singular); }

                    if (!rdr.IsDBNull(Plural)) { property.Plural = rdr.GetString(Plural); }

                    if (!rdr.IsDBNull(AssetTypeId)) { property.AssetTypeId = rdr.GetGuid(AssetTypeId); }

                    if (!rdr.IsDBNull(AssetTypeIsInstance)) { property.AssetTypeIsInstance = (bool)rdr[AssetTypeIsInstance]; }

                    property.Created = rdr.GetDateTime(Created);
                    property.CreatedBy = rdr.GetGuid(CreatedBy);

                    if (!rdr.IsDBNull(LastModified)) { property.LastModified = rdr.GetDateTime(LastModified); }
                    if (!rdr.IsDBNull(LastModifiedBy)) { property.LastModifiedBy = rdr.GetGuid(LastModifiedBy); }

                    if (!rdr.IsDBNull(Deleted)) { property.Deleted = rdr.GetDateTime(Deleted); }
                    if (!rdr.IsDBNull(DeletedBy)) { property.DeletedBy = rdr.GetGuid(DeletedBy); }

                    property.IsNew = false;
                    property.IsDirty = false;

                    values.Add(property.Id, property);
                }

            }

            return values;
        }

        internal Dictionary<Guid, string> GetDictionaryByPickListId(Guid pickListId, bool includeDeleted)
        {

            Dictionary<Guid, string> props = new Dictionary<Guid, string>();

            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT [P].[Id], IsNull(P.[DisplayValue], P.[Name]) AS [Property]");
            sql.AppendLine("FROM [Properties] P WITH (NoLock)");
            sql.AppendLine("WHERE P.[PickListId] = @PickListId");

            if (!includeDeleted)
            {
                sql.AppendLine("AND ([P].[Deleted] IS NULL)");
            }

            sql.AppendLine("ORDER BY [Property]");

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@PickListId", pickListId));

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), paramList))
            {
                while (rdr.Read())
                {
                    if (!rdr.IsDBNull(0))
                    {
                        props.Add(rdr.GetGuid(0), rdr.GetString(1));
                    }
                }
            }

            return props;

        }

        internal Dictionary<Guid, string> Properties_GetByAssetTypeId(Guid assetTypeId,
                                                                    bool hasChildAssetTypes,
                                                                    bool includeInheritedPropeties,
                                                                    bool includeInheritedValueProperties,
                                                                    bool includeDefinitionProperties,
                                                                    bool includeInstanceProperties,
                                                                    bool excludeUnused)
        {
            Dictionary<Guid, string> props = new Dictionary<Guid, string>();

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@AssetTypeId", assetTypeId));

            StringBuilder sql = new StringBuilder();

            sql.AppendLine("SELECT [P].[Id],");
            sql.AppendLine("ISNULL(p.[DisplayValue], p.[Name]) as [Property Name]");
            sql.AppendLine("FROM [Properties] P WITH (NoLock)");
            sql.AppendLine("INNER JOIN AssetTypesProperties ATP WITH (NoLock) on ATP.[PropertyId] = P.[id]");

            if (excludeUnused)
            {
                sql.AppendLine("INNER JOIN [PropertyValues] PV WITH (NoLock) ON PV.[PropertyId] = P.[Id]");
            }

            sql.AppendLine("WHERE ATP.[AssetTypeId] = @AssetTypeId");

            if (!includeInheritedPropeties)
            {
                sql.AppendLine("AND ATP.[IsInherited] = 0");
            }

            if (!includeInheritedValueProperties)
            {
                sql.AppendLine("AND ATP.[IsInheritedValue] = 0");
            }

            if (includeDefinitionProperties)
            {
                if (!includeInstanceProperties)
                {
                    sql.AppendLine("AND ATP.[IsInstance] = 0");
                }
            }
            else
            {
                sql.AppendLine("AND ATP.[IsInstance] = 1");
            }

            sql.AppendLine("AND ATP.[Deleted] IS NULL");
            sql.AppendLine("AND (P.[DataTypeId] != 16)"); // TODO: Determine how to handle system dataType = AssetType
            sql.AppendLine("AND (P.[Deleted] IS NULL)");

            sql.AppendLine("UNION");

            sql.AppendLine("SELECT [P].[Id],");
            sql.AppendLine("ISNULL(p.[DisplayValue], p.[Name]) as [Property Name]");
            sql.AppendLine("FROM [Properties] P WITH (NoLock)");

            sql.AppendLine("WHERE (P.[IsSystem] = 1)");

            if (!includeInstanceProperties)
            {
                sql.AppendLine("AND (P.[AssetTypeIsInstance] = 0)");
            }

            sql.AppendLine("AND (P.[Deleted] IS NULL)");

            if (hasChildAssetTypes)
            {
                sql.AppendLine("UNION");
                sql.AppendLine("SELECT [P].[Id],");
                sql.AppendLine("ISNULL(p.[DisplayValue], p.[Name]) as [Property Name]");
                sql.AppendLine("FROM [Properties] P WITH (NoLock)");
                sql.AppendLine("WHERE (P.[DataTypeId] = 16)");
                sql.AppendLine("AND (P.[Deleted] IS NULL)");
            }

            sql.AppendLine("ORDER BY [Property Name]");

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), paramList))
            {
                while (rdr.Read())
                {
                    if ((rdr.IsDBNull(0)) || (rdr.IsDBNull(1))) continue;
                    var id = rdr.GetGuid(0);
                    if (props.ContainsKey(id)) { continue; }
                    props.Add(id, rdr.GetString(1));
                }
            }

            return props;

        }

        internal Dictionary<Guid, string> GetDictionary(bool includeDeleted)
        {

            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@IncludeDeleted", includeDeleted));

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.Properties_GetDictionary, paramList))
            {
                if ((rdr == null) || (!rdr.HasRows)) { return values; }

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

        /// <summary>
        /// Gets a list of all properties that rely on an external source for their values (i.e. picklists, those that represent roles)
        /// This would exclude types such as int, float, datetime, etc.  If the user can type a value into the property, it is not
        /// a free-entry property and as such will not be returned in this list
        /// </summary>
        /// <param name="assetTypeIds"></param>
        /// <param name="includeInheritedPropeties"></param>
        /// <returns></returns>
        internal Dictionary<Guid, string> Properties_GetNonFreeEntry(List<Guid> assetTypeIds, EAssetRequestType requestType, bool includeInheritedPropeties)
        {

            if ((assetTypeIds == null) || (assetTypeIds.Count == 0)) { return new Dictionary<Guid, string>(); }

            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT DISTINCT P.[Id],");
            sql.AppendLine("ISNULL(p.[DisplayValue], p.[Name]) as [Property Name]");
            sql.AppendLine("FROM [Properties] P WITH (NoLock)");
            sql.AppendLine("INNER JOIN [AssetTypesProperties] ATP WITH (NoLock) ON ATP.[PropertyId] = P.[Id]");
            sql.AppendLine("WHERE ATP.[AssetTypeId] IN (");

            List<Guid> allAssetTypeIds = new List<Guid>();
            foreach (Guid id in assetTypeIds) { allAssetTypeIds.Add(id); }

            if (includeInheritedPropeties)
            {
                foreach (Guid assetTypeId in assetTypeIds)
                {
                    Guid? parentAssetTypeId = new XObjectTypeDal().ParentId(assetTypeId);
                    while (parentAssetTypeId.HasValue)
                    {
                        if (!assetTypeIds.Contains(parentAssetTypeId.Value))
                        {
                            allAssetTypeIds.Add(parentAssetTypeId.Value);
                        }
                        parentAssetTypeId = new XObjectTypeDal().ParentId(parentAssetTypeId.Value);
                    }
                }
            }

            var index = 0;
            var atIdCount = allAssetTypeIds.Count;

            foreach (var assetTypeId in allAssetTypeIds)
            {
                if (index == (atIdCount - 1))
                {
                    sql.AppendLine("'" + assetTypeId.ToString() + "'");
                }
                else
                {
                    sql.AppendLine("'" + assetTypeId.ToString() + "',");
                }
                index++;
            }

            sql.AppendLine(")");
            sql.AppendLine("AND (P.[DataTypeId] NOT IN (2, 3, 4, 5, 7, 10, 12, 21, 22, 24, 25))");
            sql.AppendLine("AND (P.[Deleted] IS NULL)");
            sql.AppendLine("AND ([ATP].[Deleted] IS NULL)");

            if (requestType == EAssetRequestType.Definition)
            {
                sql.AppendLine("AND ([ATP].[IsInstance] = 0)");
            }

            sql.AppendLine("ORDER BY [Property Name]");

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

    }

}