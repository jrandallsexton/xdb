
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Constants;
using XDB.DataObjects;

namespace XDB.Tests.Unit.Factories
{

    public class XListFactory
    {

        public XList BuildXListWithOnlyOneValue()
        {
            XList list = new XList("xlStates", "States", "List of United States", false, XUserIds.Admin);
            list.AddValue(new XListValue(list.Id, "AL", "Alabama", 0, XUserIds.Admin));

            return list;
        }

    }

}