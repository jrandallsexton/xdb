﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Enumerations;

namespace XDB.DataObjects
{

    public class XRoleHelper
    {

        public Guid RoleId { get; set; }
        public string RoleName { get; set; }
        public Guid AssetTypeId { get; set; }
        public EAssetRequestType RequestType { get; set; }

        public XRoleHelper(Guid roleId, string roleName, Guid atId, EAssetRequestType requestType)
        {
            this.RoleId = roleId;
            this.RoleName = roleName;
            this.AssetTypeId = atId;
            this.RequestType = requestType;
        }

    }

}