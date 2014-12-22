
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Common;

namespace XDB.Common.Extensions
{

    public static class COApiExtension
    {
        public static DateTime yCreated(this XBaseService coApi, Guid id) { return coApi.cBll.xCreated(id); }
        public static Guid yCreatedBy(this XBaseService coApi, Guid id) { return coApi.cBll.xCreatedBy(id); }

        public static DateTime? yLastModified(this XBaseService coApi, Guid id) { return coApi.cBll.xLastModified(id); }
        public static Guid? yLastModifiedBy(this XBaseService coApi, Guid id) { return coApi.cBll.xLastModifiedBy(id); }

        public static bool yIsValidId(this XBaseService coApi, Guid id) { return coApi.cBll.xIsValidId(id); }

        public static Guid yId(this XBaseService coApi, string name) { return coApi.cBll.xId(name); }
        public static string yName(this XBaseService coApi, Guid id) { return coApi.cBll.xName(id); }
        public static string yDisplayValue(this XBaseService coApi, Guid id) { return coApi.cBll.xDisplayValue(id); }
        public static string yDescription(this XBaseService coApi, Guid id) { return coApi.cBll.xDescription(id); }

        public static IDictionary<Guid, string> yGetDictionary(this XBaseService coApi) { return coApi.cBll.xGetDictionary(); }
    }

}