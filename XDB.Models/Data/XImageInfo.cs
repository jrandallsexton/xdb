using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Common.Interfaces;

namespace XDB.Models
{
    public struct XImageInfo : IXImageInfo
    {
        public int height { get; set; }
        public int width { get; set; }
    }
}
