
using System;

namespace XDB.Common.Interfaces
{

    public interface IXListValue
    {
        string BGColor { get; set; }
        int Index { get; set; }
        Guid XListId { get; set; }
        string Value { get; set; }
    }

}