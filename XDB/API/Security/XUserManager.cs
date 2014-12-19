
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.BLL;
using XDB.Constants;
using XDB.Enumerations;
using XDB.Extensions;
using XDB.Interfaces;

namespace XDB.API
{

    public class XUserManager : XBaseApi
    {

        public XUserManager() : base(ECommonObjectType.XUser) { }

        private XUserLayer bizLayer = new XUserLayer();

        private static XUserManager instance;
        public static XUserManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new XUserManager();
                }
                return instance;
            }
        }

        public bool HasPermission(Guid userId, ECommonObjectType coType, ESystemActionType actionType)
        {
            if (new XRoleManager().ContainsUser(XRoleIds.Admin, userId)) { return true; }
            return false;
        }

        //public static new bool ValidId(Guid id) { return new MemberLayer().ValidId(id); }
        //public static DateTime Created(Guid id) { return new MemberLayer().Created(id); }

    }

}