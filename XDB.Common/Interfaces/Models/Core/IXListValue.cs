
using System;

namespace XDB.Common.Interfaces
{

    public interface IXListValue
    {
        string BGColor { get; set; }
        int Index { get; set; }
        Guid PickListId { get; set; }
        string Value { get; set; }
    }

}