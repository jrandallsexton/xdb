
using System;
using System.Collections.Generic;

using XDB.Models;
using XDB.Common;
using XDB.Common.Enumerations;
using XDB.Common.Exceptions;
using XDB.Common.Interfaces;
using XDB.Repositories;

namespace XDB.Domains
{

    internal class XRoleDomain : XBaseDomain
    {

        private XRoleRepository dal = new XRoleRepository();

        public XRoleDomain() : base(ECommonObjectType.XRole) { }

        //public XRoleLayer(EApplicationInstance target)
        //    : base(ECommonObjectType.XRole)
        //{
        //    this.dal = new XRoleDal(Config.DbConnStringByInstance(target));
        //}

        public XRole Get(Guid id)
        {
            return this.dal.Get(id);
        }

        //public bool IsValidId(Guid id)
        //{
        //    return this.dal.IsValidId(bas)
        //}

        public bool ContainsUser(Guid roleId, Guid userId)
        {
            return this.dal.ContainsUser(roleId, userId);
        }

    }

}