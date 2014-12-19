
using System;
using System.Collections.Generic;

using XDB.DataObjects;
using XDB.Enumerations;
using XDB.DAL;

namespace XDB.BLL
{

    /// <summary>
    /// Primary entry point for manipulating Property objects
    /// </summary>
    internal class XPropertyLayer : XBaseLayer
    {

        private XPropertyDal dal = new XPropertyDal();

        public XPropertyLayer() : base(ECommonObjectType.XProperty) { }

        //public PropertyLayer(EApplicationInstance target)
        //{
        //    this.dal = new PropertyDal(SystemFrameworkHelper.DbConnStringByInstance(target));
        //}

        /// <summary>
        /// Gets an instance of a property matching the specified id
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public XProperty Get(Guid Id)
        {
            return this.dal.Get(Id);
        }

        public bool Save(XProperty property, Guid userId)
        {
            return this.dal.Save(property);
        }

        public bool Delete(Guid propertyId, Guid userId)
        {
            //RulesEngine.Validate(property, ESystemActionType.Delete, userId);
            //// TODO: Finish the prevention of deleting all system properties
            //if (propertyId.CompareTo(Constants.PropertyIds.AssetName) == 0)
            //{
            //    throw new LogicalException("Asset Name system property cannot be deleted.");
            //}

            //if (!new MemberLayer().IsValidId(userId))
            //{
            //    throw new LogicalException("Invalid user id.");
            //}

            return this.dal.Delete(propertyId, userId);
        }

        //public bool IsValidId(Guid id)
        //{
        //    return this._dal.IsValidId(id);
        //}

        //public bool Migrate(Guid propertyId, Core.Enumerations.EApplicationInstance target)
        //{
        //    throw new Exception("NOT IMPLEMENTED");
        //    //Property prop = this.Get(propertyId);

        //    //if (prop == null) { return false; }

        //    //if (prop.DataType == EDataType.PickList)
        //    //{
        //    //    // ensure the picklist exists in the target instance
        //    //    if (!new PickListLayer(target).IsValidId(prop.PickListId.Value))
        //    //    {
        //    //        PickList pl = new PickListLayer().Get(prop.PickListId.Value);
        //    //        string plName = string.IsNullOrEmpty(pl.DisplayValue) ? pl.Name : pl.DisplayValue;
        //    //        throw new LogicalException(string.Format("The PickList in-use by this property ({0}) does not exist on the target instance.", plName));
        //    //    }
        //    //}

        //    //prop.IsDirty = true;
        //    //prop.IsNew = true;

        //    //return new PropertyLayer(target).Save(prop);
        //}

        ////public bool PropertyList_Save(PropertyList values)
        ////{
        ////    this.Validate(values);
        ////    return this._dal.PropertyList_Save(values);
        ////}

        //public EDataType DataType(Guid propertyId)
        //{
        //    return this._dal.DataType(propertyId);
        //}

        //public ESystemType SystemType(Guid propertyId)
        //{
        //    return this._dal.SystemType(propertyId);
        //}

        public Dictionary<Guid, XProperty> GetObjectDictionary(List<Guid> propertyIds)
        {
            return this.dal.GetObjectDictionary(propertyIds);
        }

        //public Dictionary<Guid, string> GetDictionary(bool includeDeleted, bool includeUnapproved)
        //{
        //    return this._dal.GetDictionary(includeDeleted, includeUnapproved);
        //}

        ////private void Validate(PropertyList properties)
        ////{
        ////    foreach (Property prop in properties)
        ////    {
        ////        this.Validate(prop);
        ////    }
        ////}

        //public Guid Id(string propertyName)
        //{
        //    return this._dal.GetIdByName(propertyName);
        //}

        //public string DisplayValue(Guid id)
        //{
        //    return this._dal.DisplayValue(id);
        //}

        //public Dictionary<Guid, string> GetDictionaryByPickListId(Guid pickListId, bool includeDeleted)
        //{
        //    return this._dal.GetDictionaryByPickListId(pickListId, includeDeleted);
        //}

        //public Dictionary<Guid, string> GetDictionaryByAssetTypeId(Guid assetTypeId,
        //                                                            EAssetRequestType requestType,
        //                                                            bool includeInheritedPropeties,
        //                                                            bool includeInheritedValueProperties,
        //                                                            bool includeDefinitionProperties,
        //                                                            bool includeInstanceProperties,
        //                                                            bool excludeUnused)
        //{
        //    bool hasChildAssetTypes = new AssetTypeLayer().HasChildAssetTypes(assetTypeId);

        //    Dictionary<Guid, string> properties = this._dal.Properties_GetByAssetTypeId(assetTypeId,
        //                                                         hasChildAssetTypes,
        //                                                         includeInheritedPropeties,
        //                                                         includeInheritedValueProperties,
        //                                                         includeDefinitionProperties,
        //                                                         includeInstanceProperties,
        //                                                         excludeUnused);

        //    bool systemAssetNamePropertyFound = false;
        //    bool systemInstanceOfPropertyFound = false;
        //    bool systemInstanceOfDescPropertyFound = false;

        //    Guid? propertyIdAssetName = null;
        //    Guid? propertyIdInstanceOf = null;
        //    Guid? propertyIdInstanceOfDesc = null;

        //    List<Guid> propertyIds = new List<Guid>();

        //    foreach (KeyValuePair<Guid, string> kvp in properties)
        //    {
        //        if (!propertyIds.Contains(kvp.Key)) { propertyIds.Add(kvp.Key); }
        //    }

        //    Dictionary<Guid, Property> props = this.GetDictionary(propertyIds);
        //    foreach (KeyValuePair<Guid, Property> kvp in props)
        //    {
        //        Property prop = kvp.Value;
        //        if ((prop != null) && (prop.IsSystem))
        //        {
        //            switch (prop.SystemType)
        //            {
        //                case ESystemType.AssetName:
        //                    systemAssetNamePropertyFound = true;
        //                    propertyIdAssetName = kvp.Key;
        //                    break;
        //                case ESystemType.InstanceOf:
        //                    systemInstanceOfPropertyFound = true;
        //                    propertyIdInstanceOf = kvp.Key;
        //                    break;
        //                case ESystemType.InstanceOfDescription:
        //                    systemInstanceOfDescPropertyFound = true;
        //                    propertyIdInstanceOfDesc = kvp.Key;
        //                    break;
        //            }
        //        }
        //    }

        //    if (systemAssetNamePropertyFound)
        //    {
        //        string label = new AssetTypeLayer().ReportingLabel(assetTypeId, requestType == EAssetRequestType.Instance, false);
        //        if (!string.IsNullOrEmpty(label))
        //        {
        //            properties[propertyIdAssetName.Value] = label;
        //        }
        //    }

        //    if (systemInstanceOfPropertyFound)
        //    {
        //        string label = new AssetTypeLayer().ReportingLabel(assetTypeId, requestType == EAssetRequestType.Definition, false);
        //        if (!string.IsNullOrEmpty(label))
        //        {
        //            properties[propertyIdInstanceOf.Value] = label;
        //        }
        //    }

        //    if (systemInstanceOfDescPropertyFound)
        //    {
        //        string label = new AssetTypeLayer().ReportingLabel(assetTypeId, requestType == EAssetRequestType.Definition, false);
        //        if (!string.IsNullOrEmpty(label))
        //        {
        //            properties[propertyIdInstanceOfDesc.Value] = label + " Description";
        //        }
        //    }

        //    if (systemAssetNamePropertyFound ||
        //        systemInstanceOfDescPropertyFound ||
        //        systemInstanceOfPropertyFound)
        //    {
        //        return Helpers.Sort(properties);
        //    }

        //    return properties;
        //}

        ///// <summary>
        ///// Gets a list of all properties that rely on an external source for their values (i.e. picklists, those that represent roles)
        ///// This would exclude types such as int, float, datetime, etc.  If the user can type a value into the property, it is not
        ///// a free-entry property and as such will not be returned in this list
        ///// </summary>
        ///// <param name="assetTypeIds"></param>
        ///// <param name="requestType"></param>
        ///// <param name="includeInheritedPropeties"></param>
        ///// <returns></returns>
        //public Dictionary<Guid, string> Properties_GetNonFreeEntry(List<Guid> assetTypeIds, EAssetRequestType requestType, bool includeInheritedPropeties)
        //{
        //    return this._dal.Properties_GetNonFreeEntry(assetTypeIds, requestType, includeInheritedPropeties);
        //}

    }

}