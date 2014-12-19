
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Common;
using XDB.Common.Interfaces;

namespace XDB.Models
{

    /// <summary>
    /// Corresponding objects found in the database table CurrencyValues
    /// </summary>
    public class XMoney : XBase, IXMoney
    {

        public Guid SymbolId { get; set; }

        public string SymbolAscii { get; set; }

        public string SymbolText { get; set; }

        public decimal Amount { get; set; }

        public XMoney() { }

        public XMoney(Guid symbolId, decimal value)
        {
            this.Amount = value;
            this.Id = Guid.NewGuid();
            this.SymbolId = symbolId;
        }

    }

}