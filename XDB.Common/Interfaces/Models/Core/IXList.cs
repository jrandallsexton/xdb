
using System;
using System.Collections.Generic;

namespace XDB.Common.Interfaces
{

    public interface IXList
    {
        bool AddValue(IXListValue value);
        bool AllowNewValues { get; set; }
        string GetDisplayValueById(Guid pickListValueId);
        string GetValueById(Guid picklistValueId);
        bool IsMemberList { get; set; }
        string ServiceMethod { get; set; }
        string ServicePassword { get; set; }
        string ServiceUrl { get; set; }
        string ServiceUsername { get; set; }
        IList<IXListValue> Values { get; }
    }

}