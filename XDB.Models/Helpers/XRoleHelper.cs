
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Common.Enumerations;

namespace XDB.Models
{

    public class XRoleHelper
    {

        public Guid RoleId { get; set; }
        public string RoleName { get; set; }
        public Guid AssetTypeId { get; set; }
        public EXObjectRequestType RequestType { get; set; }

        public XRoleHelper(Guid roleId, string roleName, Guid atId, EXObjectRequestType requestType)
        {
            this.RoleId = roleId;
            this.RoleName = roleName;
            this.AssetTypeId = atId;
            this.RequestType = requestType;
        }

    }

}