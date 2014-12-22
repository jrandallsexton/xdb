
using System;
using System.Collections.Generic;

using XDB.Common.Enumerations;

namespace XDB.Common.Interfaces
{

    public interface IXValueDomain<T> :IXBaseDomain where T : XBase, IXValue
    {
        Guid AssetId(Guid xValueId);
        void Delete(Guid id, Guid userId);
        IXValue Get(Guid id);
        IXValue Get(Guid xPropertyId, Guid xObjectId);
        IXValue GetPrevious(Guid xPropertyId, Guid xObjectId);
        string PopulateDisplayValue(IXValue xValue);
        void Save(T xValue, bool existingValueIsCaseSensitive);
        void Save(IList<T> values, Guid userId);
        IDictionary<Guid, string> PropertyValues_GetPotential(Guid xObjectTypeId, EXObjectRequestType requestType, Guid xPropertyId);
    }

}