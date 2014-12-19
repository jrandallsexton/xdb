
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

    internal class XRoleDal : XBaseDal
    {

        public XRoleDal() : base(ECommonObjectType.XRole) { }

        public XRoleDal(string connString) : this() { this.ConnectionString = connString; }

        /// <summary>
        /// 1.5.0.6
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal XRole Get(string name)
        {
            string sql = "SELECT [Id] FROM [Roles] WHERE [Name] = @Name";

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Name", name));

            Guid id = base.ExecuteScalarGuidInLine(sql, paramList);

            return this.Get(id);
        }

        internal XRole Get(Guid Id)
        {

            XRole role = null;

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.Role_Get, new List<SqlParameter>() { new SqlParameter("@Id", Id) }))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return null; }

                #region get the role object

                role = new XRole();
                role.Id = Id;

                rdr.Read();
                role.Name = rdr.IsDBNull(0) ? string.Empty : rdr.GetString(0);
                role.Description = rdr.IsDBNull(1) ? string.Empty : rdr.GetString(1);
                role.IsSystem = rdr.IsDBNull(2) ? false : (bool)rdr.GetValue(2);
                role.IsActive = rdr.IsDBNull(3) ? false : (bool)rdr.GetValue(3);
                if (!rdr.IsDBNull(4)) { role.RoleLead = rdr.GetGuid(4); }
                role.Created = rdr.GetDateTime(5);
                role.CreatedBy = rdr.GetGuid(6);
                if (!rdr.IsDBNull(7)) role.LastModified = rdr.GetDateTime(7);
                if (!rdr.IsDBNull(8)) role.LastModifiedBy = rdr.GetGuid(8);
                if (!rdr.IsDBNull(9)) role.Deleted = rdr.GetDateTime(9);
                if (!rdr.IsDBNull(10)) role.DeletedBy = rdr.GetGuid(10);

                bool lastModifiedHadValue = role.LastModified.HasValue;
                DateTime? lastModifiedValue = null;
                if (lastModifiedHadValue) { lastModifiedValue = role.LastModified.Value; }

                #endregion

                #region get the role members

                //rdr.NextResult();

                //if ((rdr != null) && (rdr.HasRows))
                //{
                //    while (rdr.Read())
                //    {
                //        role.Members.Add(new RoleMember(rdr.GetGuid(0), rdr.GetGuid(1), rdr.GetString(2), rdr.GetDateTime(3), rdr.GetGuid(4), false, false));
                //    }
                //}

                #endregion

                #region get the reports available to this role

                //rdr.NextResult();

                //if ((rdr != null) && (rdr.HasRows))
                //{
                //    while (rdr.Read())
                //    {
                //        role.Reports.Add(new RoleReport(rdr.GetGuid(0), rdr.GetGuid(1), rdr.GetString(2), rdr.GetDateTime(3), rdr.GetGuid(4), false, false));
                //    }
                //}

                #endregion

                #region get the asset types for this role (and their permissions)

                //rdr.NextResult();

                //if ((rdr != null) && (rdr.HasRows))
                //{
                //    while (rdr.Read())
                //    {

                //        Guid ratId = rdr.GetGuid(0);

                //        RoleAssetType rat = role.AssetType(ratId);

                //        if (rat == null)
                //        {
                //            // load the RoleAssetType
                //            rat = new RoleAssetType();
                //            rat.Id = ratId;
                //            rat.AssetTypeId = rdr.GetGuid(1);
                //            rat.IsInstance = (bool)rdr.GetValue(2);
                //            rat.IncludeChildren = (bool)rdr.GetValue(3);
                //            rat.IsDirty = false;
                //            rat.IsNew = false;
                //            rat.IsDeleted = false;
                //            role.AssetTypes.Add(rat);
                //        }

                //        // get the permission portion
                //        Guid rolePermissionId = rdr.GetGuid(4);

                //        RolePermission perm = rat.Permission(rolePermissionId);

                //        if (perm == null)
                //        {
                //            // load the RolePermission
                //            perm = new RolePermission();
                //            perm.Id = rolePermissionId;
                //            perm.PermissionId = rdr.GetInt32(5);
                //            perm.IsIncludesPermission = (bool)rdr.GetValue(6);
                //            perm.FilterExpression = rdr.IsDBNull(7) ? string.Empty : rdr.GetString(7);
                //            perm.IsDirty = false;
                //            perm.IsNew = false;
                //            perm.IsDeleted = false;
                //            rat.Permissions.Add(perm);
                //        }

                //        // get the filter portion
                //        if (rdr.IsDBNull(8)) { continue; }

                //        Filter f = new Filter();
                //        f.Id = rdr.GetGuid(8);
                //        f.Order = rdr.GetInt32(9);
                //        f.PropertyId = rdr.GetGuid(10);
                //        f.Property = rdr.GetString(11);
                //        f.OperatorId = Core.Enumerations.EnumerationOps.EFilterOperatorFromValue(rdr.GetInt32(12));
                //        f.Operator = rdr.GetString(13);
                //        f.Value = rdr.IsDBNull(14) ? string.Empty : rdr.GetString(14);
                //        f.DisplayValue = rdr.IsDBNull(15) ? string.Empty : rdr.GetString(15);

                //        perm.Filters.Add(f);
                //    }
                //}

                #endregion

                if (lastModifiedHadValue)
                {
                    role.LastModified = lastModifiedValue.Value;
                }
                else
                {
                    role.LastModified = null;
                }

                role.IsNew = false;
                role.IsDirty = false;

            }

            return role;

        }

        //[Rework(Complete = false, Tested = false, Coverage = "0")]
        //internal bool Kill(Guid id)
        //{
        //    return base.ExecuteSql(StoredProcs.Role_Kill, new List<SqlParameter>() { new SqlParameter("@RoleId", id) });
        //}

        internal bool Save(XRole role, Guid userId)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();

            paramList.Add(new SqlParameter("@Id", role.Id));
            paramList.Add(new SqlParameter("@Name", role.Name));
            paramList.Add(new SqlParameter("@Description", role.Description));
            paramList.Add(new SqlParameter("@IsSystem", role.IsSystem));
            paramList.Add(new SqlParameter("@IsActive", role.IsActive));
            paramList.Add(new SqlParameter("@Created", role.Created));
            paramList.Add(new SqlParameter("@CreatedBy", role.CreatedBy));
            paramList.Add(new SqlParameter("@LastModified", role.LastModified));
            paramList.Add(new SqlParameter("@LastModifiedBy", role.LastModifiedBy));
            paramList.Add(new SqlParameter("@Deleted", role.Deleted));
            paramList.Add(new SqlParameter("@DeletedBy", role.DeletedBy));
            paramList.Add(new SqlParameter() { ParameterName = "@RoleLead", Value = role.RoleLead.HasValue ? role.RoleLead : null });

            if (base.ExecuteSql(StoredProcs.Role_Save, paramList))
            {

                //StringBuilder sql = new StringBuilder();

                // save the role members
                //foreach (RoleMember member in role.Members) { this.RoleMember_Save(member, role.Id); }

                // save the reports for this role
                //foreach (RoleReport rp in role.Reports) { this.RoleReport_Save(rp, role.Id); }

                // save the assetTypes
                //foreach (RoleAssetType rat in role.AssetTypes) { this.RoleAssetType_Save(rat, role.Id); }

            }

            return true;
        }

        //[Rework(Complete = false, Tested = false, Coverage = "0")]
        //private bool RolePermission_Save(RolePermission permission, Guid roleAssetTypeId)
        //{
        //    List<SqlParameter> paramList = new List<SqlParameter>
        //        {
        //            new SqlParameter("@Id", permission.Id),
        //            new SqlParameter("@RoleAssetTypesId", roleAssetTypeId),
        //            new SqlParameter("@PermissionId", permission.PermissionId),
        //            new SqlParameter("@IsIncludesFilter", permission.IsIncludesPermission),
        //            new SqlParameter() { ParameterName = "@FilterLogic", Value = string.IsNullOrEmpty(permission.FilterExpression) ? null : permission.FilterExpression },
        //            new SqlParameter("@Created", permission.Created),
        //            new SqlParameter("@CreatedBy", permission.CreatedBy),
        //            new SqlParameter("@Deleted", permission.Deleted),
        //            new SqlParameter("@DeletedBy", permission.DeletedBy)
        //        };

        //    if (!base.ExecuteSql(StoredProcs.RolePermission_Save, paramList)) { return false; }

        //    foreach (Filter f in permission.Filters)
        //    {
        //        if (!this.RoleFilter_Save(f, permission.Id)) { return false; }
        //    }

        //    return true;
        //}

        private bool RoleFilter_Save(XFilter filter, Guid rolePermissionId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>
                {
                    new SqlParameter("@Id", filter.Id),
                    new SqlParameter("@RRolePermissionsId", rolePermissionId),
                    new SqlParameter("@Order", filter.Order),
                    new SqlParameter("@PropertyId", filter.PropertyId),
                    new SqlParameter("@OperatorId", filter.OperatorId),
                    new SqlParameter() { ParameterName = "@Value", Value = string.IsNullOrEmpty(filter.Value) ? null : filter.Value }
                };

            return base.ExecuteSql(StoredProcs.RoleFilter_Save, paramList);
        }

        //[Rework(Complete = false, Tested = false, Coverage = "0")]
        //private bool RoleMember_Save(RoleMember relation, Guid roleId)
        //{
        //    List<SqlParameter> paramList = new List<SqlParameter>
        //        {
        //            new SqlParameter("@Id", relation.Id),
        //            new SqlParameter("@MemberId", relation.MemberId),
        //            new SqlParameter("@RoleId", roleId),
        //            new SqlParameter("@Created", relation.Created),
        //            new SqlParameter("@CreatedBy", relation.CreatedBy),
        //            new SqlParameter("@Deleted", relation.Deleted),
        //            new SqlParameter("@DeletedBy", relation.DeletedBy)
        //        };

        //    return base.ExecuteSql(StoredProcs.RoleMember_Save, paramList);
        //}

        //[Rework(Complete = false, Tested = false, Coverage = "0")]
        //private bool RoleReport_Save(RoleReport relation, Guid roleId)
        //{
        //    List<SqlParameter> paramList = new List<SqlParameter>
        //        {
        //            new SqlParameter("@Id", relation.Id),
        //            new SqlParameter("@ReportId", relation.ReportId),
        //            new SqlParameter("@RoleId", roleId),
        //            new SqlParameter("@Created", relation.Created),
        //            new SqlParameter("@CreatedBy", relation.CreatedBy),
        //            new SqlParameter("@Deleted", relation.Deleted),
        //            new SqlParameter("@DeletedBy", relation.DeletedBy)
        //        };

        //    return base.ExecuteSql(StoredProcs.RoleReport_Save, paramList);
        //}

        internal bool Delete(Guid id, Guid userId)
        {

            List<SqlParameter> paramList = new List<SqlParameter>
                {
                    new SqlParameter("@Id", id),
                    new SqlParameter("@DeletedBy", userId)
                };

            return base.ExecuteSql(StoredProcs.Role_Delete, paramList);

        }

        internal List<Guid> IdsForMember(Guid userId)
        {
            List<Guid> values = new List<Guid>();

            using (var rdr = base.OpenDataReader(StoredProcs.Roles_Get_By_MemberId, new List<SqlParameter> { new SqlParameter("@MemberId", userId) }))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return values; }

                while (rdr.Read()) { values.Add(rdr.GetGuid(0)); }
            }

            return values;
        }

        //[Rework(Complete = false, Tested = false, Coverage = "0")]
        //internal bool IsValidId(Guid id)
        //{
        //    return (base.ExecuteScalar(StoredProcs.RoleId_IsValid, new List<SqlParameter>() { new SqlParameter("@Id", id) }) == 1);
        //}

        internal bool ContainsUser(Guid roleId, Guid memberId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@RoleId", roleId));
            paramList.Add(new SqlParameter("@MemberId", memberId));

            return (base.ExecuteScalar(StoredProcs.Role_ContainsUser, paramList) == 1);

        }

        internal List<XRoleHelper> Roles_GetEditable(Guid userId)
        {
            List<XRoleHelper> values = new List<XRoleHelper>();

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.Roles_Get_Editable, new List<SqlParameter>() { new SqlParameter("@MemberId", userId) }))
            {

                if ((rdr != null) && (rdr.HasRows))
                {
                    while (rdr.Read())
                    {
                        values.Add(new XRoleHelper(rdr.GetGuid(0), rdr.GetString(1), rdr.GetGuid(2), EnumerationOps.EAssetRequestTypeFromValue(Convert.ToInt32(rdr.GetValue(3)))));
                    }
                }
            }

            return values;
        }

        internal Dictionary<Guid, string> Roles_GetDictionaryByMemberId(Guid userId)
        {

            Dictionary<Guid, string> roles = new Dictionary<Guid, string>();

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.Roles_Get_Dictionary_By_MemberId, new List<SqlParameter>() { new SqlParameter("@MemberId", userId) }))
            {
                if ((rdr == null) || (!rdr.HasRows)) { return roles; }

                while (rdr.Read()) { roles.Add(rdr.GetGuid(0), rdr.GetString(1)); }
            }

            return roles;

        }

        internal Dictionary<Guid, string> Roles_GetByView(Guid viewId)
        {

            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.Roles_Get_By_ViewId, new List<SqlParameter>() { new SqlParameter("@ViewId", viewId) }))
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

        internal List<Guid> GetMemberIds(Guid roleId)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@RoleId", roleId));

            List<Guid> values = new List<Guid>();

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.Role_Get_MemberIds, paramList))
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

        /// <summary>
        /// Whether or not the specified role has a certain permission to a particual assetType (instance or def)
        /// </summary>
        /// <param name="roleId">id of the role to check</param>
        /// <param name="assetTypeId">id of the assetType to check</param>
        /// <param name="requestType"></param>
        /// <param name="permission">id of the permission type we are looking for</param>
        /// <returns></returns>
        internal bool HasPermission(Guid roleId, Guid assetTypeId, Enumerations.EAssetRequestType requestType, Enumerations.EPermissionType permission)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@RoleId", roleId));
            paramList.Add(new SqlParameter("@AssetTypeId", assetTypeId));
            paramList.Add(new SqlParameter("@IsInstance", requestType == Enumerations.EAssetRequestType.Instance));
            paramList.Add(new SqlParameter("@PermissionId", permission.GetHashCode()));

            return base.ExecuteScalar(StoredProcs.Role_HasPermission, paramList) > 0;
        }

        internal bool AnyRoleHasPermission(List<Guid> roleIds, Guid permissionId, Guid assetTypeId)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT COUNT(*)");
            sql.AppendLine("FROM [RolesAssetTypesPermissions] RATP WITH (NoLock)");
            sql.AppendLine("INNER JOIN [Roles] R WITH (NoLock) ON R.[Id] = RATP.[RoleId]");
            sql.AppendLine("WHERE RATP.[Deleted] IS NULL");
            sql.AppendLine("AND R.[Deleted] IS NULL");
            sql.AppendLine("AND R.[Id] IN (");

            for (int i = 0; i < roleIds.Count; i++)
            {
                if (i == (roleIds.Count - 1))
                {
                    sql.AppendLine(string.Format("'{0}'", roleIds[i].ToString()));
                }
                else
                {
                    sql.AppendLine(string.Format("'{0}',", roleIds[i].ToString()));
                }
            }

            sql.AppendLine(") AND RATP.[PermissionId] = @PermissionId");
            sql.AppendLine("AND RATP.[AssetTypeId] = @AssetTypeId");

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@PermissionId", permissionId));
            paramList.Add(new SqlParameter("@AssetTypeId", assetTypeId));

            return base.ExecuteScalarInLine(sql.ToString(), paramList) > 0;
        }

    }

}