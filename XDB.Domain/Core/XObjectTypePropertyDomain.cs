
using System;
using System.Collections.Generic;

using XDB.Models;
using XDB.Common;
using XDB.Common.Enumerations;
using XDB.Common.Exceptions;
using XDB.Common.Extensions;
using XDB.Common.Interfaces;
using XDB.Repositories;

namespace XDB.Domains
{

    public class XObjectTypePropertyDomain<T> : XBaseDomain, IXObjectTypePropertyDomain<T> where T : XBase, IXObjectTypeProperty
    {
        public XObjectTypePropertyDomain() : base(ECommonObjectType.XObjectTypeProperty) { }
    }

}