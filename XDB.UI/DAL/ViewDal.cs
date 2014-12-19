
using System;
using System.Text;
using System.Collections.Generic;
using System.Data.SqlClient;

using XDB.UI.Constants;
using XDB.DAL;
using XDB.DataObjects;
using XDB.Enumerations;
using XDB.Exceptions;

using XDB.UI.DataObjects;

namespace XDB.UI.DAL
{

    internal class ViewDal : XSqlDal
    {

        public ViewDal() { }

        public ViewDal(string connString) { this.ConnectionString = connString; }

        //Collection by FK SPs
        private const string spViews_GetDictionaryByUserId = "spr_ViewDictionary_GetByUserId";
        private const string spView_GetPropertyIds = "spr_View_GetPropertyIds";

        private const string spView_GetDefaultProperties = "spr_View_GetDefaultProperties";

        internal View Get(Guid id)
        {

            View view = null;

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.VIEW_GET, new List<SqlParameter> { new SqlParameter("@Id", id) }))
            {

                if ((rdr == null) || (!rdr.HasRows)) return null;

                rdr.Read();

                view = new View();
                view.Id = id;

                int isInstance = rdr.GetOrdinal("IsInstance");
                int assetTypeId = rdr.GetOrdinal("AssetTypeId");
                int name = rdr.GetOrdinal("Name");
                int displayValue = rdr.GetOrdinal("DisplayValue");
                int description = rdr.GetOrdinal("Description");
                int driverCaption = rdr.GetOrdinal("DriverCaption");
                int newItemCaption = rdr.GetOrdinal("NewItemCaption");
                int confirmationLabel = rdr.GetOrdinal("ConfirmationLabel");
                int isReadOnly = rdr.GetOrdinal("IsReadOnly");
                int isStandAlone = rdr.GetOrdinal("IsStandAlone");
                int isCreation = rdr.GetOrdinal("IsCreation");
                int filterLogic = rdr.GetOrdinal("FilterLogic");
                int created = rdr.GetOrdinal("Created");
                int lastModified = rdr.GetOrdinal("LastModified");
                int deleted = rdr.GetOrdinal("Deleted");
                int createdBy = rdr.GetOrdinal("CreatedBy");
                int lastModifiedBy = rdr.GetOrdinal("LastModifiedBy");
                int deletedBy = rdr.GetOrdinal("DeletedBy");

                if (!rdr.IsDBNull(isInstance)) { view.IsInstance = (bool)rdr.GetValue(isInstance); }

                view.AssetTypeId = rdr.GetGuid(assetTypeId);

                if (!rdr.IsDBNull(name)) { view.Name = rdr.GetString(name); }

                if (!rdr.IsDBNull(displayValue)) { view.DisplayValue = rdr.GetString(displayValue); }

                if (!rdr.IsDBNull(description)) { view.Description = rdr.GetString(description); }

                if (!rdr.IsDBNull(driverCaption)) { view.DriverCaption = rdr.GetString(driverCaption); }

                if (!rdr.IsDBNull(newItemCaption)) { view.NewItemCaption = rdr.GetString(newItemCaption); }

                if (!rdr.IsDBNull(confirmationLabel)) { view.ConfirmationLabel = rdr.GetString(confirmationLabel); }

                if (!rdr.IsDBNull(isReadOnly)) { view.IsReadOnly = (bool)rdr.GetValue(isReadOnly); }

                if (!rdr.IsDBNull(isStandAlone)) { view.IsStandAlone = (bool)rdr.GetValue(isStandAlone); }

                if (!rdr.IsDBNull(isCreation)) { view.IsCreation = (bool)rdr.GetValue(isCreation); }

                if (!rdr.IsDBNull(filterLogic)) { view.FilterLogic = rdr.GetString(filterLogic); }

                view.Created = rdr.GetDateTime(created);

                if (!rdr.IsDBNull(lastModified)) { view.LastModified = rdr.GetDateTime(lastModified); }

                if (!rdr.IsDBNull(deleted)) { view.Deleted = rdr.GetDateTime(deleted); }

                view.CreatedBy = rdr.GetGuid(createdBy);

                if (!rdr.IsDBNull(lastModifiedBy)) { view.LastModifiedBy = rdr.GetGuid(lastModifiedBy); }

                if (!rdr.IsDBNull(deletedBy)) { view.DeletedBy = rdr.GetGuid(deletedBy); }

                // TODO: Remove this hard-coded 'true' value
                view.AllowCloning = true;

                //ViewPropertyGroupRelationDal relationDal = new ViewPropertyGroupRelationDal();
                PropertyGroupDal pgDal = new PropertyGroupDal();

                //foreach (ViewPropertyGroupRelation relation in relationDal.ViewPropertyGroupRelationList_GetByViewId(id))
                //{
                //    view.AddPropertyGroupMember(relation);
                //}
                view.PropertyGroups = pgDal.GetCollectionByViewId(view.Id);

                view.Filters = this.ViewFilters_Get(view.Id);

                view.IsNew = false;
                view.IsDirty = false;

                return view;

            }

        }

        internal bool HasPermission(Guid viewId, Guid memberId, EPermissionType permissionType)
        {
            List<SqlParameter> paramList = new List<SqlParameter>
                {
                    new SqlParameter("@ViewId", viewId),
                    new SqlParameter("@MemberId", memberId),
                    new SqlParameter("@PermissionId", permissionType.GetHashCode())
                };

            return (base.ExecuteScalar(StoredProcs.VIEW_HAS_PERMISSION, paramList) > 0);
        }

        internal bool Kill(Guid id)
        {
            return base.ExecuteSql(StoredProcs.VIEW_KILL, new List<SqlParameter> { new SqlParameter("@Id", id) });
        }

        internal DateTime? LastModified(Guid viewId)
        {
            const string sql = "SELECT [LastModified] FROM [Views] WHERE [Id] = @Id";
            return base.ExecuteScalarDateTime(sql, new List<SqlParameter> { new SqlParameter("@Id", viewId) });
        }

        internal Guid DefaultViewId(Guid assetTypeId, EAssetRequestType requestType)
        {

            string sql = string.Empty;

            switch (requestType)
            {
                case EAssetRequestType.Both:
                    throw new LogicalException("Cannot request a default view id for definitions and instances.");
                case EAssetRequestType.Definition:
                    sql = "SELECT [ViewId] FROM [AssetTypesViews] WITH (NoLock) WHERE ([AssetTypeId] = @Id) AND ([IsInstance] = 0)";
                    break;
                case EAssetRequestType.Instance:
                    sql = "SELECT [ViewId] FROM [AssetTypesViews] WITH (NoLock) WHERE ([AssetTypeId] = @Id) AND ([IsInstance] = 1)";
                    break;
            }

            return base.ExecuteScalarGuidInLine(sql, new List<SqlParameter>() { new SqlParameter("@Id", assetTypeId) });
        }

        internal bool Save(View view)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();

            if (!view.IsDirty) return true;

            paramList.Add(new SqlParameter("@Id", view.Id));
            paramList.Add(new SqlParameter("@Name", view.Name));
            paramList.Add(new SqlParameter("@DisplayValue", view.DisplayValue));
            paramList.Add(new SqlParameter("@Description", view.Description));
            paramList.Add(new SqlParameter("@IsInstance", view.IsInstance));
            paramList.Add(new SqlParameter("@AssetTypeId", view.AssetTypeId));

            paramList.Add(string.IsNullOrEmpty(view.DriverCaption)
                              ? new SqlParameter("@DriverCaption", null)
                              : new SqlParameter("@DriverCaption", view.DriverCaption));

            paramList.Add(string.IsNullOrEmpty(view.NewItemCaption)
                              ? new SqlParameter("@NewItemCaption", null)
                              : new SqlParameter("@NewItemCaption", view.NewItemCaption));

            paramList.Add(string.IsNullOrEmpty(view.ConfirmationLabel)
                              ? new SqlParameter("@ConfirmationLabel", null)
                              : new SqlParameter("@ConfirmationLabel", view.ConfirmationLabel));

            paramList.Add(new SqlParameter("@IsReadOnly", view.IsReadOnly));
            paramList.Add(new SqlParameter("@IsStandAlone", view.IsStandAlone));
            paramList.Add(new SqlParameter("@IsCreation", view.IsCreation));

            paramList.Add(string.IsNullOrEmpty(view.FilterLogic)
                              ? new SqlParameter("@FilterLogic", null)
                              : new SqlParameter("@FilterLogic", view.FilterLogic));

            paramList.Add(new SqlParameter("@Created", view.Created));
            paramList.Add(new SqlParameter("@LastModified", view.LastModified));
            paramList.Add(new SqlParameter("@CreatedBy", view.CreatedBy));
            paramList.Add(new SqlParameter("@LastModifiedBy", view.LastModifiedBy));
            paramList.Add(new SqlParameter("@Deleted", view.Deleted));
            paramList.Add(new SqlParameter("@DeletedBy", view.DeletedBy));

            if (base.ExecuteSql(StoredProcs.VIEW_SAVE, paramList))
            {
                //if (!new ViewPropertyGroupRelationDal(this.ConnectionString).ViewPropertyGroupRelationList_Save(view.PropertyGroupMembers)) { return false; }

                // Delete any existing ViewFilters
                this.ViewFilters_Delete(view.Id);

                // Save any current ViewFilters
                this.ViewFilters_Save(view.Id, view.Filters);

                view.IsNew = false;
                view.IsDirty = false;

                return true;
            }
            return false;
        }

        internal Dictionary<Guid, string> DefaultPropertyValues(Guid viewId)
        {

            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            using (SqlDataReader rdr = base.OpenDataReader(spView_GetDefaultProperties, new List<SqlParameter> { new SqlParameter("@ViewId", viewId) }))
            {

                if ((rdr == null) || (!rdr.HasRows)) return values;

                while (rdr.Read()) { values.Add(rdr.GetGuid(0), rdr.GetString(1)); }
            }

            return values;

        }

        internal bool Delete(Guid id, Guid userId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>
                    {
                        new SqlParameter("@Id", id),
                        new SqlParameter("@DeletedBy", userId)
                    };

            return base.ExecuteSql(StoredProcs.VIEW_DELETE, paramList);
        }

        private bool ViewFilters_Delete(Guid viewId)
        {
            const string sql = "DELETE FROM [ViewFilters] WHERE [ViewId] = @Id";
            return base.ExecuteInLineSql(sql, new List<SqlParameter> { new SqlParameter("@Id", viewId) });
        }

        private List<XFilter> ViewFilters_Get(Guid viewId)
        {
            List<XFilter> values = new List<XFilter>();

            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT VF.[Id], VF.[PropertyId], IsNull(P.[DisplayValue], P.[Name]) AS [Property],");
            sql.AppendLine("VF.[OperatorId], O.[Operator], VF.[Value], VF.[Order]");
            sql.AppendLine("FROM [ViewFilters] VF WITH (NoLock)");
            sql.AppendLine("INNER JOIN [Properties] P WITH (NoLock) ON P.[Id] = VF.[PropertyId]");
            sql.AppendLine("INNER JOIN [Operators] O WITH (NoLock) ON O.[Id] = VF.[OperatorId]");
            sql.AppendLine("WHERE VF.[ViewId] = @ViewId");
            sql.AppendLine("ORDER BY VF.[Order]");

            List<SqlParameter> paramList = new List<SqlParameter> { new SqlParameter("@ViewId", viewId) };

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), paramList))
            {
                if ((rdr != null) && (rdr.HasRows))
                {
                    while (rdr.Read())
                    {
                        Guid id = rdr.GetGuid(0);
                        Guid propertyId = rdr.GetGuid(1);
                        string property = rdr.GetString(2);
                        int operatorId = rdr.GetInt32(3);
                        string op = rdr.GetString(4);
                        string value = rdr.GetString(5);
                        int order = rdr.GetInt32(6);

                        XFilter filter = new XFilter(id, propertyId, property, EnumerationOps.EFilterOperatorFromValue(operatorId), op, value, order);
                        values.Add(filter);
                    }
                }
            }

            return values;
        }

        internal Dictionary<Guid, string> MemberFilters(Guid viewId, Guid memberId)
        {
            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            const string sql = "SELECT [Id], [Name] FROM [MemberViews] WHERE [MemberId] = @MemberId AND [ViewId] = @ViewId ORDER BY [Name]";

            List<SqlParameter> paramList = new List<SqlParameter>
                {
                    new SqlParameter("@MemberId", memberId),
                    new SqlParameter("@ViewId", viewId)
                };

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql, paramList))
            {
                if ((rdr == null) || (!rdr.HasRows)) return values;

                while (rdr.Read()) { values.Add(rdr.GetGuid(0), rdr.GetString(1)); }
            }

            return values;
        }

        //internal ClientObjects.AssetSearchClient MemberFilter_Get(Guid id)
        //{
        //    ClientObjects.AssetSearchClient client = null;

        //    using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.MEMBERVIEW_GET, new List<SqlParameter>() { new SqlParameter("@Id", id) }))
        //    {
        //        if ((rdr != null) && (rdr.HasRows))
        //        {
        //            rdr.Read();
        //            client = new ClientObjects.AssetSearchClient();
        //            client.id = id.ToString();
        //            client.userId = rdr.GetGuid(0).ToString();
        //            client.viewId = rdr.GetGuid(1).ToString();
        //            client.assetTypeId = rdr.GetGuid(2).ToString();

        //            bool isInstance = (bool)rdr.GetValue(3);
        //            client.assetRequestType = isInstance ? EAssetRequestType.Instance.GetHashCode() : EAssetRequestType.Definition.GetHashCode();
        //            client.filterName = rdr.GetString(4);
        //            client.filterExpression = rdr.GetString(5);
        //            if (!rdr.IsDBNull(6)) { client.filterString = rdr.GetString(6); }

        //            rdr.NextResult();

        //            List<ClientObjects.FilterClient> filters = new List<ClientObjects.FilterClient>();

        //            if (rdr.HasRows)
        //            {
        //                while (rdr.Read())
        //                {
        //                    ClientObjects.FilterClient f = new ClientObjects.FilterClient();
        //                    f.propertyId = rdr.GetGuid(0).ToString();
        //                    f.operatorId = rdr.GetInt32(1);
        //                    f.value = rdr.IsDBNull(2) ? string.Empty : rdr.GetString(2);
        //                    f.order = rdr.GetInt32(3);
        //                    filters.Add(f);
        //                }
        //            }

        //            client.filters = filters;
        //        }
        //    }

        //    return client;
        //}

        internal bool MemberFilter_Delete(Guid id)
        {

            StringBuilder sql = new StringBuilder();
            sql.AppendLine("DELETE FROM [MemberViewFilters] WHERE [MemberViewId] = @Id");
            sql.AppendLine("DELETE FROM [MemberViews] WHERE [Id] = @Id");

            return base.ExecuteInLineSql(sql.ToString(), new List<SqlParameter> { new SqlParameter("@Id", id) });

        }

        internal bool MemberFilter_Save(Guid id, Guid memberId, Guid viewId, string filterName, string filterExpression, string filterString, List<XFilter> filters)
        {
            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", id));
            paramList.Add(new SqlParameter("@MemberId", memberId));
            paramList.Add(new SqlParameter("@ViewId", viewId));
            paramList.Add(new SqlParameter("@Name", filterName));
            paramList.Add(new SqlParameter("@FilterExp", filterExpression));
            paramList.Add(new SqlParameter("@FilterString", filterString));

            if (base.ExecuteSql(StoredProcs.MEMBERVIEW_SAVE, paramList))
            {
                string sql = "DELETE FROM [MemberViewFilters] WHERE [MemberViewId] = @Id";
                paramList = new List<SqlParameter>();
                paramList.Add(new SqlParameter("@Id", id));

                base.ExecuteInLineSql(sql, paramList);

                foreach (XFilter f in filters)
                {
                    StringBuilder temp = new StringBuilder();
                    temp.AppendLine("INSERT INTO [MemberViewFilters] ([Id], [MemberViewId], [PropertyId], [OperatorId], [Value], [Order])");
                    temp.AppendLine("VALUES (@Id, @MemberViewId, @PropertyId, @OperatorId, @Value, @Order)");

                    paramList = new List<SqlParameter>();

                    foreach (XFilter filter in filters)
                    {
                        paramList.Clear();
                        paramList.Add(new SqlParameter("@Id", Guid.NewGuid()));
                        paramList.Add(new SqlParameter("@MemberViewId", id));
                        paramList.Add(new SqlParameter("@PropertyId", filter.PropertyId));
                        paramList.Add(new SqlParameter("@OperatorId", filter.OperatorId));
                        paramList.Add(new SqlParameter("@Value", filter.Value));
                        paramList.Add(new SqlParameter("@Order", filter.Order));
                        base.ExecuteInLineSql(temp.ToString(), paramList);
                    }

                    return true;
                }
            }

            return false;
        }

        private bool ViewFilters_Save(Guid viewId, List<XFilter> filters)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("INSERT INTO [ViewFilters] ([Id], [ViewId], [PropertyId], [OperatorId], [Value], [Order])");
            sql.AppendLine("VALUES (@Id, @ViewId, @PropertyId, @OperatorId, @Value, @Order)");

            List<SqlParameter> paramList = new List<SqlParameter>();

            foreach (XFilter filter in filters)
            {
                paramList.Clear();
                paramList.Add(new SqlParameter("@Id", Guid.NewGuid()));
                paramList.Add(new SqlParameter("@ViewId", viewId));
                paramList.Add(new SqlParameter("@PropertyId", filter.PropertyId));
                paramList.Add(new SqlParameter("@OperatorId", filter.OperatorId.GetHashCode()));
                paramList.Add(new SqlParameter("@Value", filter.Value));
                paramList.Add(new SqlParameter("@Order", filter.Order));
                base.ExecuteInLineSql(sql.ToString(), paramList);
            }

            return true;
        }

        internal bool IsAutoApprove(Guid viewId)
        {
            return true; // TODO: Implement the AutoApprove property for Views
        }

        internal bool ViewIsInstance(Guid viewId)
        {
            const string sql = "SELECT COUNT(*) FROM [Views] WITH (NoLock) WHERE [Id] = @ViewId AND [IsInstance] = 1 AND [Deleted] IS NULL";
            return base.ExecuteScalarInLine(sql, new List<SqlParameter> { new SqlParameter("@ViewId", viewId) }) == 1;
        }

        internal bool IsValidId(Guid id)
        {
            const string sql = "SELECT COUNT(*) FROM [Views] WITH (NoLock) WHERE [Id] = @Id AND [Deleted] IS NULL";
            return (base.ExecuteScalarInLine(sql, new List<SqlParameter> { new SqlParameter("@Id", id) }) == 1);
        }

        /// <summary>
        /// Gets a list of Views that are compatible with the specified asset type
        /// </summary>
        /// <param name="assetTypeId">id of the asset type the view should be compatible with</param>
        /// <param name="assetRequestType">asset instances or definitions?</param>
        /// <param name="includeNonStandAlone">whether or not to include only stand-alone views</param>
        /// <returns>a dictionary of Guid, string where the Guid is the id of the view and the string is the displayValue or name of the view</returns>
        internal Dictionary<Guid, string> GetDictionaryByAssetTypeId(Guid assetTypeId, EAssetRequestType assetRequestType, bool includeNonStandAlone)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT [Id], [DisplayValue] FROM [Views] WITH (NoLock)");
            sql.AppendLine("WHERE [AssetTypeId] = @AssetTypeId");
            sql.AppendLine("AND [Deleted] IS NULL");

            switch (assetRequestType)
            {
                case EAssetRequestType.Both:
                    break;
                case EAssetRequestType.Definition:
                    sql.AppendLine("AND [IsInstance] = 0");
                    break;
                case EAssetRequestType.Instance:
                    sql.AppendLine("AND [IsInstance] = 1");
                    break;
            }

            if (!includeNonStandAlone)
            {
                sql.AppendLine("AND [IsStandAlone] = 1");
            }

            sql.AppendLine("ORDER BY [DisplayValue]");

            List<SqlParameter> paramList = new List<SqlParameter> { new SqlParameter("@AssetTypeId", assetTypeId) };

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

        /// <summary>
        /// Gets a list of Views that are using the specified property group id
        /// </summary>
        /// <param name="propertyGroupId">id of the property group to match on</param>
        /// <returns>a dictionary of Guid, string where the Guid is the id of the view and the string is the displayValue or name of the view</returns>
        internal Dictionary<Guid, string> GetDictionaryByPropertyGroupId(Guid propertyGroupId)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT V.[Id], V.[DisplayValue]");
            sql.AppendLine("FROM [Views] V WITH (NoLock)");
            sql.AppendLine("INNER JOIN [ViewsPropertyGroups] VPG WITH (NoLock) ON VPG.[ViewId] = V.[Id]");
            sql.AppendLine("INNER JOIN [PropertyGroups] PG WITH (NoLock) ON PG.[Id] = VPG.[PropertyGroupId]");
            sql.AppendLine("WHERE VPG.[PropertyGroupId] = @PropertyGroupId");
            sql.AppendLine("AND VPG.[Deleted] IS NULL");
            sql.AppendLine("AND V.[Deleted] IS NULL");
            sql.AppendLine("AND PG.[Deleted] IS NULL");
            sql.AppendLine("ORDER BY V.[DisplayValue]");

            List<SqlParameter> paramList = new List<SqlParameter>
                {
                    new SqlParameter("@PropertyGroupId", propertyGroupId)
                };

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>a dictionary of Guid, string where the Guid is the id of the view and the string is the displayValue or name of the view</returns>
        internal Dictionary<Guid, string> GetDictionaryByUserId(Guid userId)
        {
            return base.GetDictionary(spViews_GetDictionaryByUserId, new List<SqlParameter> { new SqlParameter("@UserId", userId) });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>a dictionary of Guid, string where the Guid is the id of the view and the string is the displayValue or name of the view</returns>
        internal Dictionary<Guid, string> ViewGroups_Get(Guid userId)
        {
            return base.GetDictionary("spr_ViewGroups_GetByUserId", new List<SqlParameter> { new SqlParameter("@UserId", userId) });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>a dictionary of Guid, string where the Guid is the id of the view and the string is the displayValue or name of the view</returns>
        internal Dictionary<Guid, string> ViewsForReporting_GetDictionary(Guid userId)
        {
            return base.GetDictionary("spr_ViewDictionaryForReporting_GetByUserId", new List<SqlParameter> { new SqlParameter("@UserId", userId) });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="includeDeleted"></param>
        /// <param name="includeNonStandAlone"></param>
        /// <returns>a dictionary of Guid, string where the Guid is the id of the view and the string is the displayValue or name of the view</returns>
        internal Dictionary<Guid, string> GetDictionary(bool includeDeleted, bool includeNonStandAlone)
        {
            bool includeDisplayValues = true;
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT [Id], ");

            sql.AppendLine(includeDisplayValues
                               ? "[Name] + ' (' + [DisplayValue] + ')' FROM [Views] WITH (NoLock)"
                               : "[Name] FROM [Views]");

            bool whereClauseAdded = false;
            if (!includeDeleted)
            {
                sql.AppendLine("WHERE [Deleted] IS NULL");
                whereClauseAdded = true;
            }

            if (!includeNonStandAlone)
            {
                sql.AppendLine(whereClauseAdded ? "AND [IsStandAlone] = 1" : "WHERE [IsStandAlone] = 1");
            }

            sql.AppendLine("ORDER BY [Name]");

            return base.GetDictionary(sql.ToString());

        }

        /// <summary>
        /// Gets the asset type id of a specified view
        /// </summary>
        /// <param name="viewId">id of the view whose asset type id is wanted</param>
        /// <returns>id of the asset type associated with the specified view</returns>
        internal Guid AssetTypeId(Guid viewId)
        {
            const string sql = "SELECT [AssetTypeId] FROM [Views] WITH (NoLock) WHERE [Id] = @ViewId";
            return base.ExecuteScalarGuidInLine(sql, new List<SqlParameter> { new SqlParameter("@ViewId", viewId) });
        }

        /// <summary>
        /// Gets a distinct list of all Property Ids that are in a View
        /// </summary>
        /// <param name="viewId"></param>
        /// <returns></returns>
        internal List<Guid> PropertyIds(Guid viewId)
        {

            List<Guid> values = new List<Guid>();

            using (SqlDataReader rdr = base.OpenDataReader(spView_GetPropertyIds, new List<SqlParameter> { new SqlParameter("@ViewId", viewId) }))
            {
                if ((rdr == null) || (!rdr.HasRows)) return values;
                while (rdr.Read()) { values.Add(rdr.GetGuid(0)); }
            }

            return values;

        }

        //internal bool ViewClient_Save(ClientObjects.ViewClient view, Guid userId)
        //{
        //    BinaryFormatter formatter = new BinaryFormatter();
        //    MemoryStream memoryStream = new MemoryStream();
        //    formatter.Serialize(memoryStream, view);

        //    string sql = "INSERT INTO [UIViewClients] ([ViewId], [UserId], [Data]) VALUES (@ViewId, @UserId, @Data)";

        //    List<SqlParameter> paramList = new List<SqlParameter>
        //        {
        //            new SqlParameter("@ViewId", view.Id),
        //            new SqlParameter("@UserId", userId),
        //            new SqlParameter("@Data", memoryStream.ToArray())
        //        };

        //    return base.ExecuteInLineSql(sql, paramList);
        //}

        internal bool ViewClient_Delete(Guid userId, Guid viewId)
        {
            string sql = "DELETE FROM [UIViewClients] WHERE [ViewId] = @ViewId AND [UserId] = @UserId";
            List<SqlParameter> paramList = new List<SqlParameter>
                {
                    new SqlParameter("@ViewId", viewId),
                    new SqlParameter("@UserId", userId)
                };
            return base.ExecuteInLineSql(sql, paramList);
        }

        //internal ClientObjects.ViewClient ViewClient_Get(Guid viewId, Guid userId)
        //{
        //    string sql = "SELECT [Data] FROM [UIViewClients] WHERE [ViewId] = @ViewId AND [UserId] = @UserId";

        //    List<SqlParameter> paramList = new List<SqlParameter>
        //        {
        //            new SqlParameter("@ViewId", viewId),
        //            new SqlParameter("@UserId", userId)
        //        };

        //    using (SqlDataReader rdr = base.OpenDataReaderInLine(sql, paramList))
        //    {
        //        if ((rdr != null) && (rdr.HasRows))
        //        {
        //            rdr.Read();

        //            byte[] data = (Byte[])rdr.GetValue(0);

        //            MemoryStream ms = new MemoryStream();

        //            ms.Write(data, 0, data.Length);

        //            ms.Position = 0;

        //            BinaryFormatter bf = new BinaryFormatter();

        //            ClientObjects.ViewClient value = bf.Deserialize(ms) as ClientObjects.ViewClient;

        //            return value;
        //        }
        //    }

        //    return null;
        //}

        //internal bool AssetClient_Save(ClientObjects.AssetClient asset, Guid viewId, Guid userId)
        //{
        //    BinaryFormatter formatter = new BinaryFormatter();
        //    MemoryStream memoryStream = new MemoryStream();
        //    formatter.Serialize(memoryStream, asset);

        //    string sql = "INSERT INTO [dbo.[AssetClients] ([AssetId], [ViewId], [UserId], [Data]) VALUES (@AssetId, @ViewId, @UserId, @Data)";

        //    List<SqlParameter> paramList = new List<SqlParameter>
        //        {
        //            new SqlParameter("@AssetId", asset.Id),
        //            new SqlParameter("@ViewId", viewId),
        //            new SqlParameter("@UserId", userId),
        //            new SqlParameter("@Data", memoryStream.ToArray())
        //        };

        //    return base.ExecuteInLineSql(sql, paramList);
        //}

        internal bool AssetClients_Delete(Guid assetId)
        {
            const string sql = "DELETE FROM [dbo].[AssetClients] WHERE [AssetId] = @AssetId";
            List<SqlParameter> paramList = new List<SqlParameter> { new SqlParameter("@AssetId", assetId) };
            return base.ExecuteInLineSql(sql, paramList);
        }

        //internal ClientObjects.AssetClient AssetClient_Get(Guid assetId, Guid viewId, Guid userId)
        //{
        //    const string sql = "SELECT [Data] FROM [dbo].[AssetClients] WHERE [AssetId] = @AssetId AND [ViewId] = @ViewId AND [UserId] = @UserId";

        //    List<SqlParameter> paramList = new List<SqlParameter>
        //        {
        //            new SqlParameter("@AssetId", assetId),
        //            new SqlParameter("@ViewId", viewId),
        //            new SqlParameter("@UserId", userId)
        //        };

        //    using (SqlDataReader rdr = base.OpenDataReaderInLine(sql, paramList))
        //    {
        //        if ((rdr == null) || (!rdr.HasRows)) return null;

        //        rdr.Read();

        //        byte[] data = (Byte[])rdr.GetValue(0);

        //        MemoryStream ms = new MemoryStream();

        //        ms.Write(data, 0, data.Length);

        //        ms.Position = 0;

        //        BinaryFormatter bf = new BinaryFormatter();

        //        ClientObjects.AssetClient value = bf.Deserialize(ms) as ClientObjects.AssetClient;

        //        return value;
        //    }

        //}

    }

}