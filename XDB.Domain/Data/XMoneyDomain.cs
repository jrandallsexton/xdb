
using System;
using System.Collections.Generic;

using XDB.Models;
using XDB.Common.Enumerations;
using XDB.Common.Exceptions;
using XDB.Common.Interfaces;
using XDB.Repositories;

namespace XDB.Domains
{

    public class XMoneyDomain
    {

        private XMoneyRepository dal = new XMoneyRepository();

        public XMoneyDomain() { }

        public XMoney Get(Guid id)
        {
            return this.dal.Get(id);
        }

        public bool CurrencyValue_Save(XMoney value)
        {
            if (value.SymbolId.CompareTo(new Guid()) == 0)
            {
                throw new LogicalException("Currency symbol not specified.");
            }
            return this.dal.CurrencyValue_Save(value);
        }

    }

}