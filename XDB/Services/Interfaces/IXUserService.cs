
using System;

using XDB.Enumerations;

namespace XDB.Interfaces
{

    public interface IXUserService
    {
        bool HasPermission(Guid userId, ECommonObjectType coType, ESystemActionType actionType);
    }

}