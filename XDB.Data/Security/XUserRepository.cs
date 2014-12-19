
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Common;
using XDB.Common.Constants;
using XDB.Common.Interfaces;
using XDB.Common.Enumerations;
using XDB.Common.Exceptions;

using XDB.Models;

namespace XDB.Repositories
{

    public class XUserRepository : XBaseDal
    {

        public XUserRepository() : base(ECommonObjectType.XUser) { }

        public XUser Get(Guid id)
        {

            XUser member = null;

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", id));

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.User_Get, paramList))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return null; }

                member = new XUser();

                rdr.Read();

                member.UserId = rdr.GetString(0);
                member.LastName = rdr.IsDBNull(1) ? string.Empty : rdr.GetString(1);
                member.FirstName = rdr.IsDBNull(2) ? string.Empty : rdr.GetString(2);

                // TODO: Fix this middle initial problem
                //if (!drdSql.IsDBNull(drdSql.GetOrdinal("MName"))) _member.MiddleInitial = (char)drdSql.GetByte(drdSql.GetOrdinal("MName")); 
                if (!rdr.IsDBNull(4)) member.IsSystem = (bool)rdr[4];

                member.Created = rdr.GetDateTime(5);
                member.CreatedBy = rdr.GetGuid(6);

                if (!rdr.IsDBNull(7)) { member.LastModified = rdr.GetDateTime(7); }
                if (!rdr.IsDBNull(8)) { member.LastModifiedBy = rdr.GetGuid(8); }

                if (!rdr.IsDBNull(9)) { member.Deleted = rdr.GetDateTime(9); }
                if (!rdr.IsDBNull(10)) { member.DeletedBy = rdr.GetGuid(10); }

                member.Id = id;
                member.IsNew = false;
                member.IsDirty = false;

                return member;

            }

        }

        public XUser Member_Get(string userId)
        {
            string sql = "SELECT [Id] FROM [Members] WITH (NoLock) WHERE [UserId] = @UserId";
            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@UserId", userId));
            Guid memberId = base.ExecuteScalarGuidInLine(sql, paramList);
            return this.Get(memberId);
        }

        public bool Save(XUser member, Guid userId)
        {

            if (!member.IsDirty) { return true; }

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", member.Id));
            paramList.Add(new SqlParameter("@UserId", member.UserId));
            paramList.Add(new SqlParameter("@LName", member.LastName));
            paramList.Add(new SqlParameter("@FName", member.FirstName));

            if (member.MiddleInitial.HasValue)
            {
                paramList.Add(new SqlParameter("@MName", member.MiddleInitial.Value.ToString()));
            }
            else
            {
                paramList.Add(new SqlParameter("@MName", null));
            }

            paramList.Add(new SqlParameter("@Created", member.Created));
            paramList.Add(new SqlParameter("@CreatedBy", member.CreatedBy));

            paramList.Add(new SqlParameter("@LastModified", member.LastModified));
            paramList.Add(new SqlParameter("@LastModifiedBy", member.LastModifiedBy));

            if (base.ExecuteSql(StoredProcs.Member_Save, paramList))
            {

                member.IsNew = false;
                member.IsDirty = false;

                return true;

            }
            else
            {
                return false;
            }

        }

        //[Rework(Complete = true, Tested = false, Coverage = "0")]
        //internal override bool Save<TCommonObj>(TCommonObj member, Guid userId)
        //{

        //    if (!member.IsDirty) { return true; }

        //    List<SqlParameter> paramList = new List<SqlParameter>();
        //    paramList.Add(new SqlParameter("@Id", member.Id));
        //    paramList.Add(new SqlParameter("@UserId", member.UserId));
        //    paramList.Add(new SqlParameter("@LName", member.LastName));
        //    paramList.Add(new SqlParameter("@FName", member.FirstName));

        //    if (member.MiddleInitial.HasValue)
        //    {
        //        paramList.Add(new SqlParameter("@MName", member.MiddleInitial.Value.ToString()));
        //    }
        //    else
        //    {
        //        paramList.Add(new SqlParameter("@MName", null));
        //    }

        //    paramList.Add(new SqlParameter("@Created", member.Created));
        //    paramList.Add(new SqlParameter("@CreatedBy", member.CreatedBy));

        //    paramList.Add(new SqlParameter("@LastModified", member.LastModified));
        //    paramList.Add(new SqlParameter("@LastModifiedBy", member.LastModifiedBy));

        //    if (base.ExecuteSql(StoredProcs.Member_Save, paramList))
        //    {

        //        member.IsNew = false;
        //        member.IsDirty = false;

        //        return true;

        //    }
        //    else
        //    {
        //        return false;
        //    }

        //}

        public bool Member_Delete(Guid memberId, Guid userId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>
                {
                    new SqlParameter("@Id", memberId),
                    new SqlParameter("@DeletedBy", userId)
                };

            return base.ExecuteSql(StoredProcs.Member_Delete, paramList);
        }

        //[Rework(Complete = false, Tested = false, Coverage = "0")]
        //internal Dictionary<Guid, string> Members_GetDictionary(Guid? roleId, bool includeDeleted)
        //{

        //    if (roleId.HasValue)
        //    {
        //        return this.MembersInRole(roleId.Value);
        //    }
        //    else
        //    {
        //        return base.GetDictionary(StoredProcs.MemberDictionary, new List<SqlParameter>());
        //    }

        //}

        //[Rework(Complete = false, Tested = false, Coverage = "0")]
        //private Dictionary<Guid, string> MembersInRole(Guid roleId)
        //{
        //    return base.GetDictionary(StoredProcs.MemberDictionary_InRole,
        //                              new List<SqlParameter> {new SqlParameter("@RoleId", roleId)});
        //}

        //[Rework(Complete = false, Tested = false, Coverage = "0")]
        //internal Dictionary<Guid, string> Members_GetDictionaryNotInRole(Guid roleId)
        //{
        //    return base.GetDictionary(StoredProcs.MemberDictionary_NotInRole,
        //                              new List<SqlParameter> { new SqlParameter("@RoleId", roleId) });
        //}

        public string GetUserId(Guid memberId)
        {
            const string sql = "SELECT [UserId] FROM [Members] WITH (NoLock) WHERE [Id] = @Id";
            return base.ExecuteScalarStringInLine(sql, new List<SqlParameter> { new SqlParameter("@Id", memberId) });
        }

        public Guid GetMemberId(string userId)
        {
            const string sql = "SELECT [Id] FROM [Members] WITH (NoLock) WHERE [UserId] = @UserId";
            return base.ExecuteScalarGuidInLine(sql, new List<SqlParameter> { new SqlParameter("@UserId", userId) });
        }

        public bool UserPreference_Save(Guid userId, Guid optionTypeId, string optionValue)
        {
            // make sure it doesn't exist already
            string sql = "SELECT COUNT(*) FROM [MemberOptions]";
            sql += string.Format("WHERE [MemberId] = '{0}'", userId.ToString());
            sql += string.Format(" AND [OptionTypeId] = '{0}'", optionTypeId.ToString());
            sql += string.Format(" AND [OptionValue] = '{0}'", optionValue);

            if (base.ExecuteScalar(sql) == 0)
            {
                sql = "INSERT INTO [MemberOptions] ([Id], [MemberId], [OptionTypeId], [OptionValue]) VALUES (NEWID(), ";
                sql += string.Format("'{0}', '{1}', '{2}')", userId.ToString(), optionTypeId.ToString(), optionValue);
                return base.ExecuteSql(sql);
            }

            return true;
        }

        public bool MarkAsUpdated(Guid id, Guid byId)
        {

            string sql = "UPDATE [Members] SET [LastModified] = GetDate(), [LastModifiedby] = @ById WHERE [Id] = @Id";
            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", id));
            paramList.Add(new SqlParameter("@ById", byId));
            return base.ExecuteInLineSql(sql, paramList);
        }

        public bool MemberCanAddAssets(Guid userId, Guid assetTypeId, EAssetRequestType requestType)
        {

            if (requestType == EAssetRequestType.Both)
            {
                throw new LogicalException("Request must be for adding instances or adding definitions");
            }

            StringBuilder sql = new StringBuilder();
            sql.AppendLine("select COUNT(RATP.[Id])");
            sql.AppendLine("FROM [RolesAssetTypesPermissions] RATP WITH (NoLock)");
            sql.AppendLine("INNER JOIN [Permissions] P WITH (NoLock) ON P.[Id] = RATP.[PermissionId]");
            sql.AppendLine("INNER JOIN [RolesMembers] RM WITH (NoLock) ON RM.[RoleId] = RATP.[RoleId]");
            sql.AppendLine("WHERE RM.[MemberId] = @UserId");
            sql.AppendLine("AND RM.[Deleted] IS NULL");
            sql.AppendLine("AND RATP.[Deleted] IS NULL");
            sql.AppendLine("AND P.[Value] = @PermissionId");
            sql.AppendLine("AND RATP.[AssetTypeId] = @AssetTypeId");

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@AssetTypeId", assetTypeId));
            paramList.Add(new SqlParameter("@UserId", userId));

            if (requestType == EAssetRequestType.Definition)
            {
                paramList.Add(new SqlParameter("@PermissionId", EPermissionType.Create.GetHashCode()));
            }
            else if (requestType == EAssetRequestType.Instance)
            {
                paramList.Add(new SqlParameter("@PermissionId", EPermissionType.Create.GetHashCode()));
            }

            return base.ExecuteScalarInLine(sql.ToString(), paramList) > 0;

        }

    }

}