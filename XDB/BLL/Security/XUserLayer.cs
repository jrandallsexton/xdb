
using System;
using System.Collections.Generic;

using XDB.DataObjects;
using XDB.Enumerations;
using XDB.Exceptions;
using XDB.DAL;
using XDB.Interfaces;

namespace XDB.BLL
{

    /// <summary>
    /// Primary entry point for manipulating <see cref="Member"/> instances; also referred to as Users
    /// </summary>
    internal class XUserLayer : XBaseLayer
    {

        private readonly XUserDal dal = new XUserDal();

        public XUserLayer() : base(ECommonObjectType.XUser) { }

        //public MemberLayer(EApplicationInstance target)
        //{
        //    var connString = SystemFrameworkHelper.DbConnStringByInstance(target);
        //    this.dal = new MemberDal(connString);
        //}

        public XUser Get(Guid id)
        {

            var member = this.dal.Get(id);

            if (member == null) { return null; }

            //if (!member.IsAdmin)
            //{
            //    RoleLayer rLayer = new RoleLayer();
            //    foreach (KeyValuePair<Guid, string> kvp in rLayer.Roles_GetDictionaryByMemberId(id))
            //    {
            //        if (!rLayer.HasPermission(kvp.Key, EPermissionType.BulkUpload)) continue;
            //        member.AllowBulkUploads = true;
            //        break;
            //    }
            //}
            //else
            //{
            //    member.AllowBulkUploads = true;
            //}

            return member;
        }

        public XUser GetByUserId(string userId)
        {
            var id = this.dal.GetMemberId(userId);
            return this.Get(id);
        }

        public bool Save(XUser member, Guid userId)
        {
            this.Validate(member);

            var isNew = member.IsNew;

            if (this.dal.Save(member, userId))
            {
                if (!isNew) return true;
                // TODO: Create the corresponding Asset
                //// create a new asset linked to this user
                //// force the id to match
                //Asset newAsset = new Asset(member.Id, member.UserId, AssetTypeIds.User, null, MemberIds.Admin);
                //newAsset.Approved = newAsset.Created;
                //newAsset.ApprovedBy = newAsset.CreatedBy;
                //newAsset.DisplayValue = member.DisplayValue;
                //newAsset.IsNew = true;
                //newAsset.IsDirty = true;
                //return new AssetLayer().Save(newAsset);
            }

            return false;
        }

        /// <summary>
        /// Deletes the specified user from the system
        /// </summary>
        /// <param name="memberId">id of the user to delete</param>
        /// <param name="userId">id of the user performing this action</param>
        /// <returns>true if successful; false otherwise</returns>
        public bool Delete(Guid memberId, Guid userId)
        {
            if (this.dal.Member_Delete(memberId, userId))
            {
                // TODO: delete the associated asset
                //return new AssetLayer().Delete(memberId, userId);
            }
            return false;
        }

        public Guid GetMemberId(string userId)
        {
            return this.dal.GetMemberId(userId);
        }

        //public Dictionary<Guid, string> GetUserIdDictionary()
        //{
        //    return this._dal.GetUserIdDictionary();
        //}

        private void Validate(XUser member)
        {
            if (string.IsNullOrEmpty(member.UserId)) { throw new LogicalException("UserId cannot be null", "UserId"); }
            //if (this.IsValidId(member.CreatedBy) == false) { throw new LogicalException("Invalid user id", "CreatedBy"); }
        }

        //public bool MemberIsInRole(Guid userId, Guid roleId)
        //{
        //    return new RoleLayer().ContainsMember(userId, roleId);
        //}

        public string GetUserId(Guid memberId)
        {
            return this.dal.GetUserId(memberId);
        }

        //public Dictionary<Guid, string> Members_GetDictionary(bool includeDeleted)
        //{
        //    return this.dal.Members_GetDictionary(null, includeDeleted);
        //}

        //public Dictionary<Guid, string> Members_GetDictionary(Guid roleId)
        //{
        //    return this.dal.Members_GetDictionary(roleId, false);
        //}

        //public Dictionary<Guid, string> Members_GetDictionaryNotInRole(Guid roleId)
        //{
        //    return this.dal.Members_GetDictionaryNotInRole(roleId);
        //}

        public new string DisplayValue(Guid memberId)
        {
            XUser m = this.Get(memberId);
            if (m != null)
            {
                if ((string.IsNullOrEmpty(m.FirstName) && (string.IsNullOrEmpty(m.LastName)))) { return m.UserId; }
                if (m.MiddleInitial.HasValue)
                {
                    return string.Format("{0}, {1} {2} [{3}]", m.LastName, m.FirstName, m.MiddleInitial.Value.ToString(), m.UserId);
                }
                else
                {
                    return string.Format("{0}, {1} [{2}]", m.LastName, m.FirstName, m.UserId);
                }
            }
            return string.Empty;
        }

        public bool UserPreference_Save(Guid userId, Guid optionType, string optionValue)
        {
            return this.dal.UserPreference_Save(userId, optionType, optionValue);
        }

        public bool MarkAsUpdated(Guid id, Guid byId)
        {
            return this.dal.MarkAsUpdated(id, byId);
        }

        //public bool MemberCanAddAssets(Guid userId, Guid assetTypeId, EAssetRequestType requestType)
        //{
        //    if (this.IsAdmin(userId)) { return true; }
        //    return this.dal.MemberCanAddAssets(userId, assetTypeId, requestType);
        //}

        //public bool MemberHasPermission(Guid userId, Guid assetTypeId, EPermissionType permission, EAssetRequestType requestType)
        //{

        //    if (this.IsAdmin(userId)) { return true; }

        //    RoleLayer roleLayer = new RoleLayer();
        //    Dictionary<Guid, string> roles = roleLayer.Roles_GetDictionaryByMemberId(userId);

        //    if ((roles == null) || (roles.Count == 0)) { return false; }

        //    List<Guid> roleIds = new List<Guid>();
        //    foreach (KeyValuePair<Guid, string> kvp in roles)
        //    {
        //        if (!roleIds.Contains(kvp.Key)) { roleIds.Add(kvp.Key); }
        //    }

        //    return roleLayer.AnyRoleHasPermission(roleIds, permission, assetTypeId);

        //}

        //public bool IsAdmin(Guid memberId)
        //{
        //    return this.MemberIsInRole(memberId, Constants.RoleIds.Admin);
        //}

    }

}