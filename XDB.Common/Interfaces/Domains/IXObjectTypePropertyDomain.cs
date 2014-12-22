
using System;
using System.Collections.Generic;

using XDB.Common.Enumerations;

namespace XDB.Common.Interfaces
{

    public interface IXObjectTypePropertyDomain<T> : IXBaseDomain where T : XBase, IXObjectTypeProperty
    {

    }
}
