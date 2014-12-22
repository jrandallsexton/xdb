
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Common.Interfaces;
using XDB.Common;

namespace XDB.Common.Extensions
{

    public static class COLayerExtension
    {
        public static DateTime xCreated(this XBaseDomain coLayer, Guid id) { return coLayer.cDal.Created(id); }
        public static Guid xCreatedBy(this XBaseDomain coLayer, Guid id) { return coLayer.cDal.CreatedBy(id); }

        public static DateTime? xLastModified(this XBaseDomain coLayer, Guid id) { return coLayer.cDal.LastModified(id); }
        public static Guid? xLastModifiedBy(this XBaseDomain coLayer, Guid id) { return coLayer.cDal.LastModifiedBy(id); }

        public static bool xIsValidId(this XBaseDomain coLayer, Guid id) { return coLayer.cDal.IsValidId(id); }

        public static Guid xId(this XBaseDomain coLayer, string name) { return coLayer.cDal.Id(name); }
        public static string xName(this XBaseDomain coLayer, Guid id) { return coLayer.cDal.Name(id); }
        public static string xDisplayValue(this XBaseDomain coLayer, Guid id) { return coLayer.cDal.DisplayValue(id); }
        public static string xDescription(this XBaseDomain coLayer, Guid id) { return coLayer.cDal.Description(id); }

        public static IDictionary<Guid, string> xGetDictionary(this XBaseDomain coLayer) { return coLayer.cDal.GetDictionary(); }

    }

}