
using System;
using System.Collections.Generic;

using XDB.Common.Enumerations;

namespace XDB.Common.Interfaces
{

    public interface IXValueRepository<T> where T : XBase, IXValue
    {
        Guid AssetId(Guid xValueId);
        void Delete(Guid id, Guid userId);
        string FormatPropertyValue(IXProperty xProp, string value);
        IXValue Get(Guid id);
        IXValue Get(Guid xPropertyId, Guid xObjectId);
        IXValue GetPrevious(Guid xPropertyId, Guid xObjectId);
        string PopulateDisplayValueHtml(T xValue, IXProperty xProp);
        bool PropertyValue_Delete(Guid xObjectId, Guid propertyId, Guid userId);
        bool PropertyValue_Exists(Guid xPropertyId, Guid xObjectId, string value, bool isCaseSensitive);
        IXValue PropertyValue_Get(Guid propertyValueId, string spName);
        void Save(T xValue, bool deleteExisting, bool checkExistingIsCaseSensitive);
        IList<string> PropertyValues_GetPotentialByAssetTypeIds(List<Guid> xObjectTypeIds, ESystemType systemType);
        void Save(IList<T> xValues, IDictionary<Guid, IXProperty> xProperties);
        void Save(T xValue);
    }
}
