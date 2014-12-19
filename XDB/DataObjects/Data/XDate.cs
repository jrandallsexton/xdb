using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDB.DataObjects
{

    public class XDate
    {

        public Guid Id { get; set; }

        public string Value { get; set; }

        public string SqlDatePart { get; set; }

        public string DisplayValue { get; set; }

        public int Order { get; set; }

    }

}