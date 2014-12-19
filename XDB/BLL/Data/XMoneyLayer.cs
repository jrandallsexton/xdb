
using System;
using System.Collections.Generic;

using XDB.DataObjects;
using XDB.Enumerations;
using XDB.Exceptions;
using XDB.DAL;

namespace XDB.BLL
{

    public class XMoneyLayer
    {

        private XMoneyDal dal = new XMoneyDal();

        public XMoneyLayer() { }

        public XMoneyLayer(EApplicationInstance target)
        {
            string connString = Config.DbConnStringByInstance(target);
            this.dal = new XMoneyDal(connString);
        }

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