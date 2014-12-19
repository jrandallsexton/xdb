
using System;
using System.Collections.Generic;

using XDB.Models;
using XDB.Common.Enumerations;
using XDB.Common.Exceptions;
using XDB.Common.Interfaces;
using XDB.Repositories;

namespace XDB.Domains
{

    public class XValueDomain
    {

        private XValueRepository dal = new XValueRepository();

        public XValueDomain() { }

        public Guid AssetId(Guid propertyValueId)
        {
            return this.dal.AssetId(propertyValueId);
        }

        public XValue Get(Guid id)
        {
            return this.dal.Get(id);
        }

        public XValue Get(Guid propertyId, Guid assetId)
        {
            return this.dal.Get(propertyId, assetId);
        }

        public XValue GetPrevious(Guid propertyId, Guid assetId)
        {
            return this.dal.GetPrevious(propertyId, assetId);
        }

        internal void Save(XValue propertyValue)
        {
            XProperty prop = new XPropertyDomain().Get(propertyValue.PropertyId);

            if (this.PropertyValueIsValid(prop, propertyValue)) {
                this.dal.Save(propertyValue);
            }
        }

        public void PropertyValue_Save(XValue propertyvalue, bool existingValueIsCaseSensitive)
        {
            XProperty prop = new XPropertyDomain().Get(propertyvalue.PropertyId);
            // since we're saving a single property value, we'll always delete the existing value
            if (this.PropertyValueIsValid(prop, propertyvalue)) {
                this.dal.PropertyValue_Save(propertyvalue, true, existingValueIsCaseSensitive);
            }
        }

        public void Delete(Guid id, Guid userId)
        {
            this.dal.Delete(id, userId);
        }

        /// <summary>
        /// Gets a complete list of all values that exist and have ever existed for
        /// a specified asset property
        /// </summary>
        /// <param name="propertyId">id of the property</param>
        /// <param name="assetId">id of the asset</param>
        /// <returns>complete historical list of property values</returns>
        //public PropertyValueList PropertyValues_Get(Guid propertyId, Guid assetId, ESortOrder creationDateOrder)
        //{
        //    return this.dal.PropertyValues_Get(propertyId, assetId, creationDateOrder);
        //}

        public void PropertyValueList_Save(IList<IXValue> values, Guid userId)
        {

            //Helpers.Log("PropertyValueLayer", "PropertyValueList_Save()");

            List<Guid> propIds = new List<Guid>();

            foreach (XValue pv in values)
            {
                if (!propIds.Contains(pv.PropertyId)) { propIds.Add(pv.PropertyId); }
            }

            IDictionary<Guid, XProperty> properties = new XPropertyDomain().GetObjectDictionary(propIds);

            this.PropertyValuesAreValid(values, properties);
            this.dal.Save(values, properties);

            //// need to delete previous values
            //foreach (PropertyValue value in propertyValues)
            //{
            //    Guid assetId = value.AssetId.HasValue ? value.AssetId.Value : new Guid();
            //    Guid plvId = value.PickListValueId.HasValue ? value.PickListValueId.Value : new Guid();
            //    this.dal.PropertyValue_Delete(assetId, plvId, value.PropertyId, value.CreatedBy);
            //} 

            XObjectDomain aLayer = new XObjectDomain();

            foreach (XValue pv in values)
            {

                XProperty prop = properties[pv.PropertyId];

                if (prop.IsSystem)
                {
                    #region handling of system properties being affected by submitted property values

                    XObject a = aLayer.Asset_Get(pv.AssetId, userId);

                    switch (prop.SystemType)
                    {

                        case ESystemType.AssetName:

                            #region ESystemType.AssetName

                            if (a != null)
                            {
                                a.Name = pv.Value;
                                a.LastModified = DateTime.Now;
                                a.LastModifiedBy = pv.CreatedBy;
                                aLayer.Save(a);
                            }

                            break;

                            #endregion

                        case ESystemType.AssetType:

                            #region ESystemType.AssetType

                            if (a != null)
                            {
                                // assetTypeId has changed.  Update any instances of this asset
                                if (aLayer.Assets_ChangeAssetType(a.Id, new Guid(pv.Value)))
                                {
                                    a.AssetTypeId = new Guid(pv.Value);
                                    a.LastModified = DateTime.Now;
                                    a.LastModifiedBy = pv.CreatedBy;
                                    aLayer.Save(a);
                                }
                            }

                            break;

                            #endregion

                        case ESystemType.Description:

                            #region ESystemType.Description

                            if (a != null)
                            {
                                a.Description = pv.Value;
                                a.LastModified = DateTime.Now;
                                a.LastModifiedBy = pv.CreatedBy;
                                aLayer.Save(a);
                            }

                            break;

                            #endregion
                    }

                    #endregion
                }
            }

        }

                //        XProperty prop = new XPropertyDal().Get(pv.PropertyId);
                //pv.Value = this.FormatPropertyValue(prop, pv.Value);
                //pv.DisplayValue = this.PopulateDisplayValue(pv.Value, prop);
                //pv.DisplayValueHtml = this.PopulateDisplayValueHtml(pv, prop);

        //public bool PropertyValueList_Delete(PropertyValueList propertyvalue, Guid userId)
        //{
        //    return this.dal.PropertyValueList_Delete(propertyvalue, userId);
        //}

        //public PropertyValueList PropertyValueList_GetByPropertyId(Guid propertyId)
        //{
        //    return this.dal.PropertyValueList_GetByPropertyId(propertyId);
        //}

        //public PropertyValueList PropertyValueList_GetByAssetId(Guid assetId)
        //{
        //    return this.dal.PropertyValueList_GetByAssetId(assetId);
        //}

        private bool PropertyValueIsValid(XProperty prop, XValue propertyValue)
        {

            if (propertyValue.Id.CompareTo(new Guid()) == 0) { throw new LogicalException("Id cannot be null", "Id"); }
            if (propertyValue.PropertyId.CompareTo(new Guid()) == 0) { throw new LogicalException("PropertyId cannot be null", "PropertyId"); }
            if (propertyValue.SubmittalGroupId == new Guid()) { throw new LogicalException("SubmittalGroupId cannot be null", "SubmittalGroupId"); }

            if (propertyValue.AssetId.CompareTo(new Guid()) == 0)
            {
                throw new LogicalException("AssetId is missing", "AssetId");
            }

            if (propertyValue.CreatedBy.CompareTo(new Guid()) == 0) { throw new LogicalException("'Created By' cannot be null", "CreatedBy"); }

            // Ensure the creator is valid
            if (new XUserDomain().ValidId(propertyValue.CreatedBy) == false)
            {
                throw new LogicalException("Invalid user id", "CreatedBy");
            }

            // Ensure that the property to which this value is assigned actually exists
            //Property prop = new PropertyLayer().Property_Get(propertyValue.PropertyId);
            if (prop == null)
            {
                throw new LogicalException("Invalid PropertyId. Property specified does not exist.", "PropertyId");
            }

            // this line will allow the 'clearing' of an existing value
            // if the property value's value is empty, it's valid as-is;
            // otherwise the value must be validated according to the property'd data type
            if (string.IsNullOrEmpty(propertyValue.Value)) { return true; }

            // TODO: This has to be re-implemented later after my demo

            if (prop.IsSystem)
            {
                switch (prop.SystemType)
                {
                    case ESystemType.AssetName:
                        break;
                    case ESystemType.AssetType:
                        Guid newGuid;
                        if (Guid.TryParse(propertyValue.Value, out newGuid))
                        {
                            return new XObjectTypeDomain().ValidId(newGuid);
                        }
                        return false;
                    case ESystemType.Created:
                        break;
                    case ESystemType.CreatedBy:
                        break;
                    case ESystemType.Deleted:
                        break;
                    case ESystemType.DeletedBy:
                        break;
                    case ESystemType.Description:
                        break;
                    case ESystemType.DisplayValue:
                        break;
                    case ESystemType.InstanceOf:
                        break;
                    case ESystemType.InstanceOfDescription:
                        // should never get here.  This cannot be set from the child
                        break;
                    case ESystemType.LastModified:
                        break;
                    case ESystemType.LastModifiedBy:
                        break;
                }
            }
            else
            {
                // Ensure that the value contained in this property value can be cast to the datatype specified by the property
                switch (prop.DataType)
                {
                    case EDataType.Date:
                    case EDataType.DateTime:
                        DateTime dtValue;
                        if (!DateTime.TryParse(propertyValue.Value, out dtValue))
                        {
                            throw new LogicalException(string.Format("For property '{0}', data type is '{1}' but provided value is not.  Value provided was '{2}'.", prop.Name, prop.DataType.ToString(), propertyValue.Value), "Value");
                        }
                        break;

                    case EDataType.Document:
                        // TODO: Implement
                        break;

                    case EDataType.Currency:

                        // TODO: Right now I'm depending on the client UI for currency validation
                        // NOT COOL

                        //char[] characters = propertyValue.Value.ToCharArray();
                        //List<char> currencySymbols = new List<char>();
                        //currencySymbols.Add('$');
                        //currencySymbols.Add('€');
                        //currencySymbols.Add('£');
                        //currencySymbols.Add('¥');

                        //foreach (char character in characters)
                        //{
                        //    if ((character != '.') && (character != ','))
                        //    {

                        //    }
                        //}
                        //propertyValue.Value = propertyValue.Value.Replace("$", string.Empty); // don't store anything but the actual number

                        //double inValue;
                        //bool parsed = Double.TryParse(propertyValue.Value, out inValue);

                        //if (!parsed)
                        //{
                        //    throw new LogicalException("Invalid currency value", prop.Name);
                        //}

                        break;

                    case EDataType.Float:
                        float floatValue;
                        if (!float.TryParse(propertyValue.Value, out floatValue))
                        {
                            throw new LogicalException(string.Format("For property '{0}', data type is '{1}' but provided value is not.  Value provided was '{2}'.", prop.Name, prop.DataType.ToString(), propertyValue.Value), "Value");
                        }
                        break;

                    case EDataType.Image:
                        // TODO: Implement
                        break;

                    case EDataType.Int:
                        int intValue;
                        if (!int.TryParse(propertyValue.Value, out intValue))
                        {
                            throw new LogicalException(string.Format("For property '{0}', data type is '{1}' but provided value is not.  Value provided was '{2}'.", prop.Name, prop.DataType.ToString(), propertyValue.Value), "Value");
                        }
                        break;

                    case EDataType.String:
                        // This will always work
                        break;

                    case EDataType.URL:
                        if (!string.IsNullOrEmpty(propertyValue.Value))
                        {
                            Guid urlId = new Guid(propertyValue.Value);
                            XUrl url = new XUrlDomain().Get(urlId);
                            if (url != null)
                            {
                                Uri testUrl;
                                if (!Uri.TryCreate(url.Url, UriKind.Absolute, out testUrl))
                                {
                                    throw new LogicalException(string.Format("For property '{0}', data type is '{1}' but provided value is not.  Value provided was '{2}'.", prop.Name, prop.DataType.ToString(), propertyValue.Value), "Value");
                                }
                            }
                        }

                        break;

                    case EDataType.User:

                        Guid userId;

                        if (Guid.TryParse(propertyValue.Value, out userId))
                        {
                            XUser member = new XUserDomain().Get(userId);
                            if (member == null)
                            {
                                throw new LogicalException(string.Format("For property '{0}', data type is '{1}' but the provided user id is invalid.  Value provided was '{2}'.", prop.Name, prop.DataType.ToString(), propertyValue.Value), "Value");
                            }
                        }
                        else
                        {
                            throw new LogicalException(string.Format("For property '{0}', data type is '{1}' but provided value is not.  Value provided was '{2}'.", prop.Name, prop.DataType.ToString(), propertyValue.Value), "Value");
                        }
                        break;
                }

            }

            return true;

        }

        private bool PropertyValuesAreValid(IList<IXValue> propertyValues, IDictionary<Guid, XProperty> properties)
        {

            Dictionary<Guid, int> propertyCounts = new Dictionary<Guid, int>();

            foreach (XValue pv in propertyValues)
            {

                XProperty prop = properties[pv.PropertyId];

                if (propertyCounts.ContainsKey(pv.PropertyId))
                {
                    // does this property allow multiple values?
                    if (!prop.AllowMultiValue)
                    {
                        throw new LogicalException(string.Format("Multiple values specified for {0}, but does not allow multi-values", prop.Name), prop.Name);
                    }
                }
                else
                {
                    propertyCounts.Add(pv.PropertyId, 1);
                }

                if (!this.PropertyValueIsValid(prop, pv)) { return false; }

            }
            return true;
        }

        //public StringBuilder PropertyValues_GetSql(string assetDisplayName,
        //                   List<Guid> assetTypeIds,
        //                   EAssetRequestType requestType,
        //                   List<Guid> propertyIds,
        //                   bool orderResults)
        //{
        //    return this.dal.PropertyValues_GetSql(assetDisplayName, assetTypeIds, requestType, propertyIds, orderResults);
        //}

        //public StringBuilder PropertyValues_GetSql(string assetDisplayName,
        //                               Guid assetTypeId,
        //                               List<Guid> assetIds,
        //                               List<Guid> propertyIds,
        //                               bool orderResults)
        //{
        //    AssetTypeLayer atLayer = new AssetTypeLayer();
        //    string defLabel = atLayer.ReportingLabel(assetTypeId, false, false);
        //    string insLabel = atLayer.ReportingLabel(assetTypeId, true, false);
        //    return this.dal.PropertyValues_GetSql(assetDisplayName, assetTypeId, assetIds, propertyIds, orderResults, defLabel, insLabel);
        //}

        ///// <summary>
        ///// Gets a dataset of all of the current values for the properties
        ///// requested for the specified assets
        ///// </summary>
        ///// <param name="assetIds"></param>
        ///// <param name="propertyIds"></param>
        ///// <returns></returns>
        //public DataSet PropertyValues_Get(string assetDisplayName, Guid assetTypeId, List<Guid> assetIds, List<Guid> propertyIds)
        //{
        //    AssetTypeLayer atLayer = new AssetTypeLayer();
        //    string defLabel = atLayer.ReportingLabel(assetTypeId, false, false);
        //    string insLabel = atLayer.ReportingLabel(assetTypeId, true, false);
        //    return this.dal.PropertyValues_Get(assetDisplayName, assetTypeId, assetIds, propertyIds, defLabel, insLabel);
        //}

        //public DataSet PropertyValues_GetByReportId(Guid reportId, string assetDisplayName, int page, int recordsPerPage, string sortColumn, string sortDir)
        //{
        //    return this.dal.PropertyValues_GetByReportId(reportId, assetDisplayName, page, recordsPerPage, sortColumn, sortDir);
        //}

        /////// <summary>
        /////// Gets a dataset of all of the current values for the properties
        /////// reuqested for an asset type
        /////// </summary>
        /////// <param name="assetTypeId"></param>
        /////// <param name="propertyIds"></param>
        /////// <returns></returns>
        //public DataSet PropertyValues_Get(EAssetRequestType requestType,
        //                                  string assetDisplayName,
        //                                  Guid assetTypeId,
        //                                  List<Guid> assetIds,
        //                                  List<Guid> propertyIds,
        //                                  int page,
        //                                  int recordsPerPage,
        //                                  string sortColumn,
        //                                  string sortDir, out string sqlText)
        //{
        //    AssetTypeLayer atLayer = new AssetTypeLayer();
        //    string defLabel = atLayer.GetReportingLabel(assetTypeId, false);
        //    string insLabel = atLayer.GetReportingLabel(assetTypeId, true);

        //    return this.dal.PropertyValues_Get(requestType, assetDisplayName, assetTypeId, assetIds, propertyIds, page, recordsPerPage, sortColumn, sortDir, defLabel, insLabel, out sqlText);
        //}

        //public DataSet PropertyValues_GetFromDBViews(EAssetRequestType requestType,
        //              string assetDisplayName,
        //              Guid assetTypeId,
        //              List<Guid> assetTypeIds,
        //              List<Guid> assetIds,
        //              List<ReportProperty> reportProperties,
        //              int page,
        //              int recordsPerPage,
        //              string sortColumn,
        //              string sortDir,
        //              out string sqlText)
        //{
        //    AssetTypeLayer atLayer = new AssetTypeLayer();
        //    string defLabel = atLayer.ReportingLabel(assetTypeId, false, false);
        //    string insLabel = atLayer.ReportingLabel(assetTypeId, true, false);
        //    return this.dal.PropertyValues_GetFromDatabaseViews(requestType, assetDisplayName, assetTypeId, assetTypeIds, assetIds, reportProperties, page, recordsPerPage, sortColumn, sortDir, defLabel, insLabel, out sqlText);
        //}

        //public DataSet PropertyValues_GetNewWithCustomFields(EAssetRequestType requestType,
        //                      string assetDisplayName,
        //                      Guid assetTypeId,
        //                      List<Guid> assetTypeIds,
        //                      List<Guid> assetIds,
        //                      List<ReportProperty> reportProperties,
        //                      int page,
        //                      int recordsPerPage,
        //                      string sortColumn,
        //                      string sortDir,
        //                      out string sqlText)
        //{
        //    AssetTypeLayer atLayer = new AssetTypeLayer();
        //    string defLabel = atLayer.ReportingLabel(assetTypeId, false, false);
        //    string insLabel = atLayer.ReportingLabel(assetTypeId, true, false);
        //    return this.dal.PropertyValues_GetNewWithCustomFields(requestType, assetDisplayName, assetTypeId, assetTypeIds, assetIds, reportProperties, page, recordsPerPage, sortColumn, sortDir, defLabel, insLabel, out sqlText);
        //}

        //public bool PropertyValue_DeleteForAnAsset(Guid assetId, Guid propertyId, string value, Guid userId)
        //{
        //    Property prop = new PropertyLayer().Get(propertyId);
        //    if (prop == null) { throw new Exception("Invalid property id"); }
        //    List<Guid> propertyIds = new List<Guid>();
        //    propertyIds.Add(propertyId);
        //    PropertyValueList values = this.PropertyValues_GetForAnAsset(assetId, propertyIds, false, false);
        //    if (values != null)
        //    {
        //        foreach (PropertyValue pv in values)
        //        {
        //            if (!this.Delete(pv.Id, userId)) { return false; }
        //        }
        //    }
        //    return true;
        //}

        //public PropertyValueList PropertyValues_GetByAssetType(Guid assetTypeId, List<Guid> propertyIds)
        //{
        //    PropertyValueList values = new PropertyValueList();

        //    foreach (KeyValuePair<Guid, string> asset in new AssetLayer().Assets_GetDictionary(assetTypeId, false, true, true))
        //    {
        //        foreach (Guid propertyId in propertyIds)
        //        {
        //            PropertyValue value = this.Get(propertyId, asset.Key);
        //            if (value != null) { values.Add(value); }
        //        }
        //    }

        //    return values;
        //}

        //public PropertyValueList PropertyValues_GetForAnAsset(Guid assetId, List<Guid> propertyIds, bool includeUnapproved, bool includeDeleted)
        //{
        //    PropertyValueList returnValues = new PropertyValueList();

        //    PropertyLayer propLayer = new PropertyLayer();
        //    AssetTypeLayer atLayer = new AssetTypeLayer();
        //    AssetLayer aLayer = new AssetLayer();

        //    List<Guid> assetPropertyIds = new List<Guid>();
        //    List<Guid> assetParentPropertyIds = new List<Guid>();

        //    // within the context of the AssetType of this AssetId,
        //    // we must determine whether or not the property values are to come from:
        //    // the asset itself
        //    // or
        //    // the parent definition
        //    // so ...

        //    Asset asset = aLayer.Get(assetId);
        //    if (asset == null) { return null; }

        //    // ... get the AssetTypeProperty relations for the determined AssetType
        //    AssetType assetType = atLayer.Get(asset.AssetTypeId);

        //    if (assetType == null) { return null; }

        //    List<Property> specialProperties = new List<Property>();

        //    Dictionary<Guid, Property> props = propLayer.GetDictionary(propertyIds);

        //    foreach (Guid propertyId in propertyIds)
        //    {
        //        Property prop = props[propertyId];

        //        if (prop.IsSystem)
        //        {
        //            switch (prop.SystemType)
        //            {
        //                case ESystemType.AssetName:
        //                case ESystemType.AssetType:
        //                case ESystemType.Description:
        //                case ESystemType.DisplayValue:
        //                case ESystemType.InstanceOf:
        //                case ESystemType.InstanceOfDescription:
        //                    specialProperties.Add(prop);
        //                    break;
        //            }
        //        }
        //        else
        //        {
        //            AssetTypePropertyRelation relation = this.AssetTypePropertyRelation_Get(assetType.Properties, propertyId);
        //            if (relation != null)
        //            {
        //                if ((relation.IsInheritedValue) && (asset.IsInstance))
        //                {
        //                    if (!assetParentPropertyIds.Contains(propertyId)) { assetParentPropertyIds.Add(propertyId); }
        //                }
        //                else if ((asset.IsInstance) && (!relation.IsInstance))
        //                {
        //                    if (!assetParentPropertyIds.Contains(propertyId)) { assetParentPropertyIds.Add(propertyId); }
        //                }
        //                else
        //                {
        //                    if (!assetPropertyIds.Contains(propertyId)) { assetPropertyIds.Add(propertyId); }
        //                }
        //            }
        //            else
        //            {
        //                // TODO: this is a problem; determine what I should do
        //                if (!assetPropertyIds.Contains(propertyId)) { assetPropertyIds.Add(propertyId); }
        //            }
        //        }
        //    }

        //    if (assetParentPropertyIds.Count > 0)
        //    {

        //        foreach (PropertyValue value in this.dal.PropertyValues_GetForAnAsset(assetId, assetPropertyIds, includeUnapproved, includeDeleted))
        //        {
        //            returnValues.Add(value);
        //        }

        //        Guid? parentId = null;

        //        if (aLayer.IsInstance(assetId))
        //        {
        //            Guid? tempId = aLayer.InstanceOfId(assetId);
        //            if (tempId.HasValue)
        //            {
        //                parentId = tempId.Value;
        //            }
        //        }
        //        else
        //        {
        //            parentId = aLayer.ParentId(assetId);
        //        }

        //        if (parentId.HasValue)
        //        {
        //            foreach (PropertyValue value in this.dal.PropertyValues_GetForAnAsset(parentId.Value, assetParentPropertyIds, includeUnapproved, includeDeleted))
        //            {
        //                returnValues.Add(value);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        returnValues = this.dal.PropertyValues_GetForAnAsset(assetId, assetPropertyIds, includeUnapproved, includeDeleted);
        //    }

        //    foreach (Property prop in specialProperties)
        //    {

        //        PropertyValue pv = null;

        //        switch (prop.SystemType)
        //        {
        //            case ESystemType.AssetName:
        //                string aName = string.IsNullOrEmpty(asset.DisplayValue) ? asset.Name : asset.DisplayValue;
        //                pv = new PropertyValue(prop.Id, assetId, aName, asset.CreatedBy);
        //                pv.Created = asset.Created;
        //                break;
        //            case ESystemType.AssetType:
        //                pv = new PropertyValue(prop.Id, assetId, asset.AssetTypeId.ToString(), asset.CreatedBy);
        //                pv.Created = asset.Created;
        //                pv.DisplayValue = assetType.Name;
        //                break;
        //            case ESystemType.Description:
        //                if (string.IsNullOrEmpty(asset.Description)) { break; }
        //                pv = new PropertyValue(prop.Id, assetId, asset.Description, asset.CreatedBy);
        //                pv.Created = asset.Created;
        //                break;
        //            case ESystemType.DisplayValue:
        //                pv = new PropertyValue(prop.Id, assetId, aLayer.DisplayValue(assetId), asset.CreatedBy);
        //                pv.Created = asset.Created;
        //                break;
        //            case ESystemType.InstanceOf:
        //                Guid? instanceOfId = aLayer.InstanceOfId(assetId);
        //                string instanceOfName = aLayer.Name(instanceOfId.Value);
        //                pv = new PropertyValue(prop.Id, assetId, instanceOfId.ToString(), asset.CreatedBy);
        //                pv.Created = asset.Created;
        //                pv.DisplayValue = instanceOfName;
        //                break;
        //            case ESystemType.InstanceOfDescription:
        //                Guid? ioId = aLayer.InstanceOfId(assetId);
        //                PropertyValue tempPv = new PropertyValueLayer().Get(Constants.PropertyIds.Description, ioId.Value);
        //                if (tempPv != null)
        //                {
        //                    string value = string.IsNullOrEmpty(tempPv.DisplayValue) ? tempPv.Value : tempPv.DisplayValue;
        //                    pv = new PropertyValue(prop.Id, assetId, value, aLayer.CreatedBy(ioId.Value));
        //                    pv.Created = tempPv.Created;
        //                    pv.CreatedBy = tempPv.CreatedBy;
        //                    pv.DisplayValue = value;
        //                }
        //                break;
        //        }

        //        if (pv != null)
        //        {
        //            pv.IsDirty = false;
        //            pv.IsNew = false;
        //            returnValues.Add(pv);
        //            returnValues.IsDirty = false;
        //        }
        //    }

        //    return returnValues;

        //}

        //private AssetTypePropertyRelation AssetTypePropertyRelation_Get(AssetTypePropertyRelationList relations, Guid propertyId)
        //{
        //    foreach (AssetTypePropertyRelation relation in relations)
        //    {
        //        if (relation.PropertyId.CompareTo(propertyId) == 0) { return relation; }
        //    }
        //    return null;
        //}

        //public Dictionary<Guid, string> PropertyValues_GetMatching(Guid assetTypeId, Guid propertyId, string propertyValue)
        //{

        //    Dictionary<Guid, string> values = new Dictionary<Guid, string>();

        //    Property prop = new PropertyLayer().Get(propertyId);

        //    if (prop != null)
        //    {
        //        if (prop.DataType == EDataType.PickList)
        //        {
        //            Guid propertyValueId = new PicklistValueLayer().GetIdByDisplayValue(prop.PickListId.Value, propertyValue);

        //            if (propertyValueId.CompareTo(new Guid()) == 0)
        //            {
        //                propertyValueId = new PicklistValueLayer().GetIdByValue(prop.PickListId.Value, propertyValue);

        //                if (propertyValueId.CompareTo(new Guid()) == 0) { return values; } // should not occur
        //            }

        //            return this.dal.PropertyValues_GetMatching(assetTypeId, propertyId, propertyValueId.ToString());

        //        }
        //        else if (prop.RoleId.HasValue)
        //        {
        //            // TODO: Revisit this logic and figure out what I was thinking
        //            //string userId;
        //            //int loc = propertyValue.IndexOf("[");
        //            //userId = propertyValue.Substring(loc + 1, propertyValue.Length - loc - 1);
        //            //loc = userId.IndexOf("]");
        //            //userId = userId.Substring(0, loc);

        //            //Guid memberId = new MemberLayer().GetMemberId(userId);
        //            //return this.dal.PropertyValues_GetMatching(assetTypeId, propertyId, memberId.ToString());
        //            return this.dal.PropertyValues_GetMatching(assetTypeId, propertyId, propertyValue);
        //        }
        //        else
        //        {
        //            return this.dal.PropertyValues_GetMatching(assetTypeId, propertyId, propertyValue);
        //        }
        //    }

        //    return values;

        //}

        //public Dictionary<Guid, string> PropertyValues_GetPotentialByAssetTypeIds(List<Guid> assetTypeIds, Guid propertyId, EAssetRequestType requestType)
        //{

        //    Dictionary<Guid, string> returnData = new Dictionary<Guid, string>();

        //    Property prop = new PropertyLayer().Get(propertyId);

        //    List<string> tempValues = null;
        //    if (prop.IsSystem)
        //    {
        //        return this.dal.GetDictionaryByAssetTypeIds(assetTypeIds, prop.SystemType);
        //    }
        //    else
        //    {
        //        tempValues = this.dal.PropertyValues_GetPotentialByAssetTypeIds(assetTypeIds, propertyId);
        //    }

        //    if ((tempValues != null) && (tempValues.Count > 0))
        //    {
        //        foreach (string val in tempValues)
        //        {
        //            Guid valId;
        //            if (Guid.TryParse(val, out valId))
        //            {
        //                returnData.Add(valId, this.PopulateDisplayValue(prop, val));
        //            }
        //            else
        //            {
        //                returnData.Add(Guid.NewGuid(), this.PopulateDisplayValue(prop, val));
        //            }
        //        }
        //    }

        //    return returnData;
        //}

        //public Dictionary<Guid, string> PropertyValues_GetPotentialByAssetIds(List<Guid> assetIds, Guid propertyId, Guid assetTypeId, EAssetRequestType requestType)
        //{
        //    if ((assetIds == null) || (assetIds.Count == 0)) { return new Dictionary<Guid, string>(); }

        //    AssetLayer aLayer = new AssetLayer();

        //    Property prop = new PropertyLayer().Get(propertyId);

        //    List<string> tempValues = null;
        //    Dictionary<Guid, string> returnData = new Dictionary<Guid, string>();

        //    // if the property is not a system type, we have to find the assetType-Property relation
        //    // in order to determine if it is an inherited value
        //    if (!prop.IsSystem)
        //    {
        //        #region non-system properties

        //        AssetTypePropertyRelation relation = new AssetTypePropertyRelationDal().AssetTypePropertyRelation_Get(assetTypeId, propertyId);

        //        if (relation == null)
        //        {

        //            // perhaps the relation belongs to the parent
        //            AssetTypeLayer atLayer = new AssetTypeLayer();

        //            Guid? parentId = atLayer.ParentId(assetTypeId);

        //            while (parentId.HasValue)
        //            {
        //                bool keepLooking = true;
        //                while (keepLooking)
        //                {
        //                    relation = new AssetTypePropertyRelationDal().AssetTypePropertyRelation_Get(parentId.Value, propertyId);
        //                    if (relation == null)
        //                    {
        //                        // does this parent have a parent?
        //                        parentId = atLayer.ParentId(parentId.Value);
        //                        if (parentId == null)
        //                        {
        //                            keepLooking = false;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        keepLooking = false;
        //                        parentId = null;
        //                    }
        //                }
        //            }

        //            if (relation == null)
        //            {
        //                return new Dictionary<Guid, string>(); // no parent relation; return no potential values - no relation found
        //            }

        //        }

        //        if ((!relation.IsInstance) && (requestType == EAssetRequestType.Instance))
        //        {
        //            List<Guid> instanceOfIds = aLayer.InstanceOfIds(assetIds);
        //            tempValues = this.dal.PropertyValues_GetPotential(instanceOfIds, propertyId);
        //        }
        //        else
        //        {
        //            tempValues = this.dal.PropertyValues_GetPotential(assetIds, propertyId);
        //        }

        //        #endregion
        //    }
        //    else
        //    {
        //        tempValues = this.dal.PropertyValues_GetPotential(assetIds, prop.SystemType);
        //    }

        //    if ((tempValues != null) && (tempValues.Count > 0))
        //    {
        //        switch (prop.DataType)
        //        {
        //            case EDataType.Asset:

        //                #region EDataType.Asset

        //                if (prop.IsSystem)
        //                {
        //                    switch (prop.SystemType)
        //                    {
        //                        case ESystemType.InstanceOf:
        //                            List<Guid> instanceOfIds = new List<Guid>();
        //                            foreach (Guid id in assetIds)
        //                            {
        //                                Guid? instanceOfId = aLayer.InstanceOfId(id);
        //                                if ((instanceOfId.HasValue) && (!instanceOfIds.Contains(instanceOfId.Value)))
        //                                {
        //                                    instanceOfIds.Add(instanceOfId.Value);
        //                                }
        //                            }
        //                            return aLayer.Assets_Get(instanceOfIds);
        //                    }
        //                }
        //                else
        //                {
        //                    return aLayer.Assets_Get(this.ListOfStringsToGuid(tempValues));
        //                }
        //                break;
        //                #endregion

        //            case EDataType.PickList:

        //                #region EDataType.PickList

        //                return new PicklistValueLayer().GetMatching(this.ListOfStringsToGuid(tempValues), prop.PickListId.Value);

        //                #endregion

        //            case EDataType.String:

        //                #region EDataType.String

        //                if (prop.IsSystem)
        //                {
        //                    switch (prop.SystemType)
        //                    {
        //                        case ESystemType.AssetType:

        //                            AssetTypeLayer atLayer = new AssetTypeLayer();

        //                            foreach (string val in tempValues) // these are actually asset type ids
        //                            {
        //                                Guid atId = new Guid(val);
        //                                if (!returnData.ContainsKey(atId))
        //                                {
        //                                    returnData.Add(atId, atLayer.Name(atId));
        //                                }
        //                            }

        //                            return returnData;
        //                    }
        //                }
        //                else
        //                {
        //                    foreach (string value in tempValues)
        //                    {
        //                        if (!string.IsNullOrEmpty(value))
        //                        {
        //                            returnData.Add(System.Guid.NewGuid(), value.ToString());
        //                        }
        //                    }
        //                }
        //                break;
        //                #endregion

        //            case EDataType.User:

        //                return aLayer.Assets_Get(this.ListOfStringsToGuid(tempValues));

        //            default:

        //                #region default

        //                foreach (string value in tempValues) { returnData.Add(System.Guid.NewGuid(), value.ToString()); }

        //                break;

        //                #endregion
        //        }
        //    }

        //    return new Dictionary<Guid, string>();

        //}

        /// <summary>
        /// Converts a list of strings (actually Guids) to a list of Guids
        /// </summary>
        /// <param name="strings"></param>
        /// <returns></returns>
        private List<Guid> ListOfStringsToGuid(List<string> strings)
        {
            List<Guid> values = new List<Guid>();
            foreach (string s in strings)
            {
                Guid g;
                if (Guid.TryParse(s, out g))
                {
                    if (!values.Contains(g)) { values.Add(g); }
                }
            }
            return values;
        }

        public Dictionary<Guid, string> PropertyValues_GetPotential(Guid assetTypeId, EAssetRequestType requestType, Guid propertyId)
        {
            SqlDatabaseLayer dbLayer = new SqlDatabaseLayer();

            PropertySelect rProp = new PropertySelect(Guid.NewGuid(), propertyId, new XPropertyDomain().DisplayValue(propertyId), 0);

            var sql = dbLayer.GetSqlString(assetTypeId, requestType, new List<PropertySelect> { rProp }, new List<XFilter>(), string.Empty, true, true);
            return this.dal.GetDictionary(sql.ToString());
        }

        //public List<string> PropertyValues_GetPotential_OLD(List<Guid> assetIds, Guid propertyId, Guid userId)
        //{
        //    if ((assetIds == null) || (assetIds.Count == 0)) { return new List<string>(); }

        //    AssetLayer aLayer = new AssetLayer();

        //    Asset a = aLayer.Asset_Get(assetIds[0], userId);

        //    Property prop = new PropertyLayer().Get(propertyId);

        //    if (prop.IsSystem)
        //    {
        //        //switch (prop.SystemType)
        //        //{

        //        //}
        //        List<Guid> instanceOfIds = new List<Guid>();
        //        foreach (Guid assetId in assetIds)
        //        {
        //            Guid? instanceOfId = aLayer.InstanceOfId(assetId);
        //            if (!instanceOfIds.Contains(instanceOfId.Value)) { instanceOfIds.Add(instanceOfId.Value); }
        //        }
        //        return this.dal.PropertyValues_GetPotential(instanceOfIds, propertyId);
        //    }
        //    else
        //    {
        //        AssetTypePropertyRelation relation = new AssetTypePropertyRelationDal().AssetTypePropertyRelation_Get(a.AssetTypeId, propertyId);

        //        if (relation == null)
        //        {

        //            // perhaps the relation belongs to the parent
        //            AssetTypeLayer atLayer = new AssetTypeLayer();

        //            Guid? parentId = atLayer.ParentId(a.AssetTypeId);

        //            while (parentId.HasValue)
        //            {
        //                bool keepLooking = true;
        //                while (keepLooking)
        //                {
        //                    relation = new AssetTypePropertyRelationDal().AssetTypePropertyRelation_Get(parentId.Value, propertyId);
        //                    if (relation == null)
        //                    {
        //                        // does this parent have a parent?
        //                        parentId = atLayer.ParentId(parentId.Value);
        //                        if (parentId == null)
        //                        {
        //                            keepLooking = false;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        keepLooking = false;
        //                        parentId = null;
        //                    }
        //                }
        //            }

        //            if (relation == null)
        //            {
        //                return new List<string>(); // no parent relation; return no potential values - no relation found
        //            }

        //        }

        //        if ((!relation.IsInstance) && (a.IsInstance))
        //        {
        //            List<Guid> instanceOfIds = new List<Guid>();
        //            foreach (Guid assetId in assetIds)
        //            {
        //                Guid? instanceOfId = aLayer.InstanceOfId(assetId);
        //                if (!instanceOfIds.Contains(instanceOfId.Value)) { instanceOfIds.Add(instanceOfId.Value); }
        //            }
        //            return this.dal.PropertyValues_GetPotential(instanceOfIds, propertyId);
        //        }
        //        else
        //        {
        //            return this.dal.PropertyValues_GetPotential(assetIds, propertyId);
        //        }
        //    }
        //}

        internal string PopulateDisplayValue(XProperty prop, string propertyValue)
        {

            if (string.IsNullOrEmpty(propertyValue)) { return string.Empty; }

            Guid selectedId;
            Guid.TryParse(propertyValue, out selectedId);

            if (prop != null)
            {
                if (prop.IsSystem)
                {
                    switch (prop.SystemType)
                    {
                        case ESystemType.AssetType: return new XObjectTypeDomain().Name(selectedId);

                        case ESystemType.CreatedBy:
                        case ESystemType.DeletedBy:
                            // get the formatted name of the user
                            XUser m = new XUserDomain().Get(selectedId);

                            if (m != null)
                            {
                                if (m.MiddleInitial.HasValue)
                                {
                                    return string.Format("{0}, {1} {2} [{3}]", m.LastName, m.FirstName, m.MiddleInitial.Value.ToString(), m.UserId);
                                }
                                else
                                {
                                    return string.Format("{0}, {1} [{2}]", m.LastName, m.FirstName, m.UserId);
                                }
                            }

                            break;
                        default:
                            return propertyValue;
                    }
                }
                else
                {
                    switch (prop.DataType)
                    {

                        case EDataType.Relation_ChildParent:
                        case EDataType.Relation_Other:
                        case EDataType.Relation_ParentChild:
                        case EDataType.Dependency: // get the display value or name of the asset
                        case EDataType.Asset: // get the display value or name of the asset

                            #region different properties whose values are an asset

                            XObject asset = new XObjectDomain().Get(selectedId);
                            if (asset != null)
                            {
                                return string.IsNullOrEmpty(asset.DisplayValue) ? asset.Name : asset.DisplayValue;
                            }

                            break;

                            #endregion

                        case EDataType.User:

                            #region user

                            // get the formatted name of the user
                            XUser m = new XUserDomain().Get(selectedId);

                            if (m != null)
                            {
                                if (m.MiddleInitial.HasValue)
                                {
                                    return string.Format("{0}, {1} {2} [{3}]", m.LastName, m.FirstName, m.MiddleInitial.Value.ToString(), m.UserId);
                                }
                                else
                                {
                                    return string.Format("{0}, {1} [{2}]", m.LastName, m.FirstName, m.UserId);
                                }
                            }

                            break;

                            #endregion

                        case EDataType.Document:

                            #region document

                            XDocument doc = new XDocumentDomain().Get(selectedId, true);

                            if (doc != null)
                            {
                                return doc.Title;
                            }
                            else
                            {
                                return string.Empty;
                            }

                            #endregion

                        case EDataType.Image:

                            #region image

                            XImage img = new XImageDomain().Get(selectedId, true);

                            if (img != null)
                            {
                                return img.Name;
                            }
                            else
                            {
                                return string.Empty;
                            }

                            #endregion

                        case EDataType.PickList:

                            throw new Exception("NOT IMPLEMENTED DUDE!!!!");
                        //return new PicklistDal().DisplayValue(prop.PickListId.Value, propertyValue);

                        case EDataType.Currency:

                            #region currency

                            XMoney cv = new XMoneyDomain().Get(selectedId);

                            if (cv != null)
                            {
                                if (cv.SymbolText.Length > 1)
                                {
                                    // put a space in between larger ones
                                    return string.Format("{0} {1}", cv.SymbolAscii, cv.Amount);
                                }
                                // no space for simple Dollar, Euro, Yen signs
                                return string.Format("{0}{1}", cv.SymbolAscii, cv.Amount);
                            }
                            return propertyValue;

                            #endregion

                        case EDataType.System_AssetType:

                            #region asset type

                            return new XObjectTypeDomain().Name(selectedId);

                            #endregion

                        case EDataType.URL:

                            #region url

                            XUrl url = new XUrlDomain().Get(selectedId);
                            if (url != null)
                            {
                                if (string.IsNullOrEmpty(url.Name))
                                {
                                    return url.Url;
                                }
                                else
                                {
                                    return string.Format("{0}|{1}", url.Name, url.Url);
                                }
                            }
                            return string.Empty;

                            #endregion

                        default:

                            return propertyValue;
                    }
                }

            }

            return string.Empty;
            //return this.dal.PopulateDisplayValue(propertyValue, prop);
        }

        public string PopulateDisplayValue(XValue propertyValue)
        {
            return this.PopulateDisplayValue(new XPropertyDomain().Get(propertyValue.PropertyId), propertyValue.Value);
        }

    }

}