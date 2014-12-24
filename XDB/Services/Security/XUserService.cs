
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Domains;
using XDB.Models;
using XDB.Common;
using XDB.Common.Constants;
using XDB.Common.Enumerations;
using XDB.Common.Exceptions;
using XDB.Common.Interfaces;
using XDB.Interfaces;

using XDB.Common.Interfaces;

namespace XDB.API
{

    public class XUserService : XBaseService, IXUserService
    {

        public XUserService() : base(ECommonObjectType.XUser) { }

        //private XUserLayer bizLayer = new XUserLayer();

        //private static XUserManager instance;
        //public static XUserManager Instance
        //{
        //    get
        //    {
        //        if (instance == null)
        //        {
        //            instance = new XUserManager();
        //        }
        //        return instance;
        //    }
        //}

        public bool HasPermission(Guid userId, ECommonObjectType coType, ESystemActionType actionType)
        {
            if (new XRoleService().ContainsUser(XRoleIds.Admin, userId)) { return true; }
            return false;
        }

        //public static new bool ValidId(Guid id) { return new MemberLayer().ValidId(id); }
        //public static DateTime Created(Guid id) { return new MemberLayer().Created(id); }

    }

}