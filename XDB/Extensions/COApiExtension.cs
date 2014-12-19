
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.API;

namespace XDB.Extensions
{

    internal static class COApiExtension
    {
        internal static DateTime yCreated(this XBaseApi coApi, Guid id) { return coApi.cBll.xCreated(id); }
        internal static Guid yCreatedBy(this XBaseApi coApi, Guid id) { return coApi.cBll.xCreatedBy(id); }

        internal static DateTime? yLastModified(this XBaseApi coApi, Guid id) { return coApi.cBll.xLastModified(id); }
        internal static Guid? yLastModifiedBy(this XBaseApi coApi, Guid id) { return coApi.cBll.xLastModifiedBy(id); }

        internal static bool yIsValidId(this XBaseApi coApi, Guid id) { return coApi.cBll.xIsValidId(id); }

        internal static Guid yId(this XBaseApi coApi, string name) { return coApi.cBll.xId(name); }
        internal static string yName(this XBaseApi coApi, Guid id) { return coApi.cBll.xName(id); }
        internal static string yDisplayValue(this XBaseApi coApi, Guid id) { return coApi.cBll.xDisplayValue(id); }
        internal static string yDescription(this XBaseApi coApi, Guid id) { return coApi.cBll.xDescription(id); }

        internal static IDictionary<Guid, string> yGetDictionary(this XBaseApi coApi) { return coApi.cBll.xGetDictionary(); }
    }

}