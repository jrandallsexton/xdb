
using System;
using System.Collections.Generic;

using XDB.BLL;

namespace XDB.Interfaces
{

    public interface IXBaseApi
    {
        
        //IXBaseBll bll { get; set; }
        XBaseLayer cBll { get; set; }

        Guid Id(string name);
        string Name(Guid id);
        string DisplayValue(Guid id);
        string Description(Guid id);

        DateTime Created(Guid id);
        Guid CreatedBy(Guid id);

        DateTime? LastModified(Guid id);
        Guid? LastModifiedBy(Guid id);

        bool ValidId(Guid id);

        IDictionary<Guid, string> GetDictionary();
        //bool Save<TCommonObj>(Guid userId, TCommonObj t) where TCommonObj : CommonObject;
        //Dictionary<Guid, T> GetObjectDictionary<S>(List<Guid> ids);// where T : CommonObject;

    }

}