
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

    public class XPropertyRepository<T> : XBaseDal, IXPropertyRepository<T> where T : XBase, IXProperty
    {

        public XPropertyRepository() : base(ECommonObjectType.XProperty) { }

        IXProperty IXPropertyRepository<T>.Get(Guid id)
        {
            throw new NotImplementedException();
        }

        public XProperty Get(Guid id)
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

        public void Save(T xProp)
        {

            if ((!xProp.IsDirty) || (!xProp.IsNew)) { return; }

            List<SqlParameter> paramList = new List<SqlParameter>();

            paramList.Add(new SqlParameter("@Id", xProp.Id));
            paramList.Add(new SqlParameter("@DataTypeId", xProp.DataType.GetHashCode()));
            paramList.Add(new SqlParameter("@Precision", xProp.Precision));

            paramList.Add(new SqlParameter() { ParameterName = "@PickListId", Value = xProp.PickListId.HasValue ? xProp.PickListId : null });

            paramList.Add(new SqlParameter("@AllowMultiValue", xProp.AllowMultiValue));
            paramList.Add(new SqlParameter("@IsOrdered", xProp.IsOrdered));
            paramList.Add(new SqlParameter("@Name", xProp.Name));

            paramList.Add(new SqlParameter() { ParameterName = "@DisplayValue", Value = string.IsNullOrEmpty(xProp.DisplayValue) ? null : xProp.DisplayValue });
            paramList.Add(new SqlParameter() { ParameterName = "@Description", Value = string.IsNullOrEmpty(xProp.Description) ? null : xProp.Description });

            paramList.Add(new SqlParameter("@AssetTypeId", xProp.AssetTypeId));
            paramList.Add(new SqlParameter("@AssetTypeIsInstance", xProp.AssetTypeIsInstance));

            paramList.Add(new SqlParameter("@Created", xProp.Created));
            paramList.Add(new SqlParameter("@CreatedBy", xProp.CreatedBy));

            paramList.Add(new SqlParameter() { ParameterName = "@LastModified", Value = xProp.LastModified.HasValue ? xProp.LastModified : null });
            paramList.Add(new SqlParameter() { ParameterName = "@LastModifiedBy", Value = xProp.LastModifiedBy.HasValue ? xProp.LastModifiedBy : null });

            paramList.Add(new SqlParameter() { ParameterName = "@Deleted", Value = xProp.Deleted.HasValue ? xProp.Deleted : null });
            paramList.Add(new SqlParameter() { ParameterName = "@DeletedBy", Value = xProp.DeletedBy.HasValue ? xProp.DeletedBy : null });

            paramList.Add(new SqlParameter("@RoleId", xProp.RoleId));
            paramList.Add(new SqlParameter("@IsSystem", xProp.IsSystem));
            paramList.Add(new SqlParameter("@SystemTypeId", xProp.SystemType.GetHashCode()));

            base.ExecuteSql(StoredProcs.Property_Save, paramList);

            xProp.IsNew = false;
            xProp.IsDirty = false;

        }

        /// <summary>
        /// Deletes a property instance/record matching the specified id
        /// </summary>
        /// <param name="propertyId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public void Delete(Guid propertyId, Guid userId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>
                    {
                        new SqlParameter("@Id", propertyId),
                        new SqlParameter("@DeletedBy", userId)
                    };

            base.ExecuteSql(StoredProcs.Property_Delete, paramList);

        }

        public EDataType DataType(Guid propertyId)
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

        public ESystemType SystemType(Guid propertyId)
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

        public Guid GetIdByName(string propertyName)
        {
            string sql = "SELECT [Id] FROM [Properties] WITH (NoLock) WHERE [Name] = @Name";
            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Name", propertyName));
            return base.ExecuteScalarGuidInLine(sql, paramList);
        }

        public IDictionary<Guid, XProperty> GetObjectDictionary(List<Guid> propertyIds)
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

        public IDictionary<Guid, string> GetDictionaryByPickListId(Guid pickListId, bool includeDeleted)
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

        public IDictionary<Guid, string> Properties_GetByAssetTypeId(Guid assetTypeId,
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

        public IDictionary<Guid, string> GetDictionary(bool includeDeleted)
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
        public IDictionary<Guid, string> Properties_GetNonFreeEntry(List<Guid> assetTypeIds, EAssetRequestType requestType, bool includeInheritedPropeties)
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
                    Guid? parentAssetTypeId = new XObjectTypeRepository<XObjectType>().ParentId(assetTypeId);
                    while (parentAssetTypeId.HasValue)
                    {
                        if (!assetTypeIds.Contains(parentAssetTypeId.Value))
                        {
                            allAssetTypeIds.Add(parentAssetTypeId.Value);
                        }
                        parentAssetTypeId = new XObjectTypeRepository<XObjectType>().ParentId(parentAssetTypeId.Value);
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