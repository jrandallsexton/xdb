
using System;

using XDB.Common;
using XDB.Common.Enumerations;
using XDB.Common.Interfaces;

namespace XDB.Models
{

    public class XDate : XBase, IXDate
    {

        public string Value { get; set; }

        public string SqlDatePart { get; set; }

        public int Order { get; set; }

    }

}