
using System;

using XDB.Common.Interfaces;

namespace XDB.Common.Interfaces
{

    public interface IXListService
    {
        void Delete(Guid userId, Guid id);
        IXList Get(Guid id);
        void Save(Guid userId, IXList xList);
    }

}