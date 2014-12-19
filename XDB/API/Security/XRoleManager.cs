
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.BLL;
using XDB.Enumerations;
using XDB.Extensions;
using XDB.Interfaces;

namespace XDB.API
{

    public class XRoleManager : XBaseApi
    {

        public XRoleManager() : base(ECommonObjectType.XRole) { }

        private XRoleLayer bizLayer = new XRoleLayer();

        private static XRoleManager instance;
        public static XRoleManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new XRoleManager();
                }
                return instance;
            }
        }

        public bool ContainsUser(Guid roleId, Guid userId)
        {
            return this.bizLayer.ContainsUser(roleId, userId);
        }

        //public Role Clone(Guid id, bool cloneMembers)
        //{
        //    Role r = new Role();
        //    r.Id = id;
        //    r.Name = this.Name;
        //    r.Description = this.Description;
        //    r.Created = this.Created;
        //    r.CreatedBy = this.CreatedBy;
        //    r.IsActive = this.IsActive;
        //    r.IsSystem = this.IsSystem;
        //    //r.Members = new List<RoleMember>();
        //    //if (cloneMembers)
        //    //{
        //    //    foreach (RoleMember rm in this.Members) { r.Members.Add(rm.Clone(Guid.NewGuid())); }
        //    //}

        //    return r;
        //}

        //[Rework(Complete = false, Tested = false, Coverage = "0")]
        //public bool RoleAssetType_Save(RoleAssetType relation, Guid roleId)
        //{
        //    List<SqlParameter> paramList = new List<SqlParameter>
        //        {
        //            new SqlParameter("@Id", relation.Id),
        //            new SqlParameter("@RoleId", roleId),
        //            new SqlParameter("@AssetTypeId", relation.AssetTypeId),
        //            new SqlParameter("@IsInstance", relation.IsInstance),
        //            new SqlParameter("@IncludeChildren", relation.IncludeChildren),
        //            new SqlParameter("@Created", relation.Created),
        //            new SqlParameter("@CreatedBy", relation.CreatedBy),
        //            new SqlParameter("@Deleted", relation.Deleted),
        //            new SqlParameter("@DeletedBy", relation.DeletedBy)
        //        };

        //    return base.ExecuteSql(StoredProcs.RoleAssetType_Save, paramList);
        //}
    }

}