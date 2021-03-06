﻿
using System;
using System.Collections.Generic;

using XDB.Common;

namespace XDB.Common.Interfaces
{

    public interface IXBaseDomain
    {

        XBaseDal cDal { get; set; }

        Guid Id(string name);
        string Name(Guid id);
        string DisplayValue(Guid id);
        string Description(Guid id);

        DateTime Created(Guid id);
        Guid CreatedBy(Guid id);

        DateTime? LastModified(Guid id);
        Guid? LastModifiedBy(Guid id);

        bool ValidId(Guid id);

        //IList<T> GetAll<T>() where T : class, new();
        IDictionary<Guid, string> GetDictionary();

    }

}