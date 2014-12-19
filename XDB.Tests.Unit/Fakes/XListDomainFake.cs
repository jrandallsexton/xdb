
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using XDB.API;
using XDB.BLL;
using XDB.DataObjects;
using XDB.Interfaces;

namespace XDB.Tests.Unit.Fakes
{

    public class XListDomainFake : IXListDomain 
    {
        public void Delete(Guid pickListId, Guid userId)
        {
            throw new NotImplementedException();
        }

        public XList Get(Guid id)
        {
            throw new NotImplementedException();
        }

        public IDictionary<Guid, string> GetDictionary(bool includeDeleted)
        {
            throw new NotImplementedException();
        }

        public void Save(XList picklist, Guid userId)
        {
            throw new NotImplementedException();
        }
    }

}