
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Common.Interfaces;
using XDB.Common;

namespace XDB.Common.Extensions
{

    internal static class COLayerExtension
    {
        internal static DateTime xCreated(this XBaseDomain coLayer, Guid id) { return coLayer.cDal.Created(id); }
        internal static Guid xCreatedBy(this XBaseDomain coLayer, Guid id) { return coLayer.cDal.CreatedBy(id); }

        internal static DateTime? xLastModified(this XBaseDomain coLayer, Guid id) { return coLayer.cDal.LastModified(id); }
        internal static Guid? xLastModifiedBy(this XBaseDomain coLayer, Guid id) { return coLayer.cDal.LastModifiedBy(id); }

        internal static bool xIsValidId(this XBaseDomain coLayer, Guid id) { return coLayer.cDal.IsValidId(id); }

        internal static Guid xId(this XBaseDomain coLayer, string name) { return coLayer.cDal.Id(name); }
        internal static string xName(this XBaseDomain coLayer, Guid id) { return coLayer.cDal.Name(id); }
        internal static string xDisplayValue(this XBaseDomain coLayer, Guid id) { return coLayer.cDal.DisplayValue(id); }
        internal static string xDescription(this XBaseDomain coLayer, Guid id) { return coLayer.cDal.Description(id); }

        internal static IDictionary<Guid, string> xGetDictionary(this XBaseDomain coLayer) { return coLayer.cDal.GetDictionary(); }

    }

}