
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Common.Enumerations;
using XDB.Common.Extensions;
using XDB.Common.Interfaces;

//using Ninject.Modules;

namespace XDB.Common
{

    public class XBaseService : IXBaseService
    {

        public XBaseService(ECommonObjectType objectType) { this.cBll = new XBaseDomain(objectType); }

        public XBaseDomain cBll { get; set; }
        //public IXBaseBll bll { get; set; }

        public DateTime Created(Guid id) { return this.yCreated(id); }
        public Guid CreatedBy(Guid id) { return this.yCreatedBy(id); }

        public DateTime? LastModified(Guid id) { return this.yLastModified(id); }
        public Guid? LastModifiedBy(Guid id) { return this.yLastModifiedBy(id); }

        public Guid Id(string name) { return this.yId(name); }
        public string Name(Guid id) { return this.yName(id); }
        public string DisplayValue(Guid id) { return this.yDisplayValue(id); }
        public string Description(Guid id) { return this.yDescription(id); }

        public bool ValidId(Guid id) { return this.yIsValidId(id); }

        public IDictionary<Guid, string> GetDictionary() { return this.yGetDictionary(); }

        //public override void Load()
        //{
        //    //Bind<IXListService>().To<XListService>();
        //    //Bind<IXListDomain>().To<XListDomain>();
        //    //Bind<IXListRepository>().To<XListRepository>();
        //}

    }

}
