
using System;
using System.Collections.Generic;

using XDB.DataObjects;
using XDB.Enumerations;
using XDB.Exceptions;
using XDB.DAL;
using XDB.Interfaces;

namespace XDB.BLL
{

    internal class XRoleLayer : XBaseLayer
    {

        private XRoleDal dal = new XRoleDal();

        public XRoleLayer() : base(ECommonObjectType.XRole) { }

        //public XRoleLayer(EApplicationInstance target)
        //    : base(ECommonObjectType.XRole)
        //{
        //    this.dal = new XRoleDal(Config.DbConnStringByInstance(target));
        //}

        public XRole Get(Guid id)
        {
            return this.dal.Get(id);
        }

        public bool IsValidId(Guid id)
        {
            return this.dal.IsValidId(id);
        }

        public bool ContainsUser(Guid roleId, Guid userId)
        {
            return this.dal.ContainsUser(roleId, userId);
        }

    }

}