
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.DAL;
using XDB.Enumerations;
using XDB.Extensions;
using XDB.Interfaces;

namespace XDB.BLL
{

    public class XBaseLayer : IXBaseBll
    {

        internal XBaseLayer(ECommonObjectType objectType) { this.cDal = new XBaseDal(objectType); }

        public XBaseDal cDal { get; set; }

        public DateTime Created(Guid id) { return this.xCreated(id); }
        public Guid CreatedBy(Guid id) { return this.xCreatedBy(id); }

        public DateTime? LastModified(Guid id) { return this.xLastModified(id); }
        public Guid? LastModifiedBy(Guid id) { return this.xLastModifiedBy(id); }

        public Guid Id(string name) { return this.xId(name); }
        public string Name(Guid id) { return this.xName(id); }
        public string DisplayValue(Guid id) { return this.xDisplayValue(id); }
        public string Description(Guid id) { return this.xDescription(id); }

        public bool ValidId(Guid id) { return this.xIsValidId(id); }

        public IDictionary<Guid, string> GetDictionary() { return this.xGetDictionary(); }

    }

}