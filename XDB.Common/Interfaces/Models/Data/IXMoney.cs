
using System;

namespace XDB.Common.Interfaces
{

    public interface IXMoney
    {
        decimal Amount { get; set; }
        string SymbolAscii { get; set; }
        Guid SymbolId { get; set; }
        string SymbolText { get; set; }
    }

}