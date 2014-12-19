
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.BLL;

namespace XDB.Extensions
{

    internal static class COLayerExtension
    {
        internal static DateTime xCreated(this XBaseLayer coLayer, Guid id) { return coLayer.cDal.Created(id); }
        internal static Guid xCreatedBy(this XBaseLayer coLayer, Guid id) { return coLayer.cDal.CreatedBy(id); }

        internal static DateTime? xLastModified(this XBaseLayer coLayer, Guid id) { return coLayer.cDal.LastModified(id); }
        internal static Guid? xLastModifiedBy(this XBaseLayer coLayer, Guid id) { return coLayer.cDal.LastModifiedBy(id); }

        internal static bool xIsValidId(this XBaseLayer coLayer, Guid id) { return coLayer.cDal.IsValidId(id); }

        internal static Guid xId(this XBaseLayer coLayer, string name) { return coLayer.cDal.Id(name); }
        internal static string xName(this XBaseLayer coLayer, Guid id) { return coLayer.cDal.Name(id); }
        internal static string xDisplayValue(this XBaseLayer coLayer, Guid id) { return coLayer.cDal.DisplayValue(id); }
        internal static string xDescription(this XBaseLayer coLayer, Guid id) { return coLayer.cDal.Description(id); }

        internal static IDictionary<Guid, string> xGetDictionary(this XBaseLayer coLayer) { return coLayer.cDal.GetDictionary(); }

    }

}