
using System;
using System.Collections.Generic;

using XDB.DataObjects;
using XDB.Enumerations;
using XDB.DAL;

namespace XDB.BLL
{

    /// <summary>
    /// Provides a public means of working with <see cref="XObject"/> instances in this framework.
    /// Most activities will probably be centered around this class.
    /// </summary>
    internal class XObjectLayer : XBaseLayer
    {

        private XObjectDal dal = new XObjectDal();

        public XObjectLayer() : base(ECommonObjectType.XObject) { }

        //public AssetLayer(EApplicationInstance target)
        //{
        //    this.dal = new AssetDal(SystemFrameworkHelper.DbConnStringByInstance(target));
        //}

        /// <summary>
        /// Determines whether or not an asset exists based on the specified name and type
        /// </summary>
        /// <param name="assetName">name of the asset</param>
        /// <param name="assetTypeId">type id of the asset</param>
        /// <returns>true if asset exists; otherwise false</returns>
        public bool AssetExists(string assetName, Guid assetTypeId)
        {
            Guid? assetId = this.GetIdByName(assetName, assetTypeId);
            return assetId.HasValue;
        }

        //public Dictionary<Guid, string> GetDictionaryByNames(List<string> assetNames, List<Guid> assetTypeIds, EAssetRequestType requestType)
        //{
        //    return this.dal.GetDictionaryByNames(assetNames, assetTypeIds, requestType);
        //}

        //public Dictionary<Guid, string> Assets_GetMatchingByAssetTypeId(Guid userId, Guid assetTypeId, EAssetRequestType requestType, string filterExpression, List<Filter> filters, bool includeDesc)
        //{
        //    List<ReportProperty> props = new List<ReportProperty>
        //        {
        //            new ReportProperty(Guid.NewGuid(), Core.Constants.PropertyIds.AssetName, "Asset Name")
        //        };

        //    SqlDatabaseLayer dbLayer = new SqlDatabaseLayer();

        //    StringBuilder sql = dbLayer.GetSqlString(assetTypeId, requestType, props, filters, filterExpression, true, false);

        //    return dbLayer.GetDictionary(sql.ToString());
        //}

        //public Dictionary<string, Dictionary<Guid, string>> Assets_GetGrouped(Guid assetTypeId, Guid propertyId, bool includeChildAssetTypes, EAssetRequestType requestType)
        //{

        //    List<Guid> assetTypeIds = new List<Guid>() { assetTypeId };
        //    List<Guid> assetIds = new List<Guid>();

        //    if (includeChildAssetTypes)
        //    {
        //        AssetTypeLayer atLayer = new AssetTypeLayer();
        //        foreach (Guid atId in atLayer.AssetType_GetChildren(assetTypeId, true))
        //        {
        //            if (!assetTypeIds.Contains(atId)) { assetTypeIds.Add(atId); }
        //        }
        //    }

        //    foreach (Guid id in assetTypeIds)
        //    {
        //        foreach (Guid assetId in this.GetAssetIdsByAssetTypeId(id, requestType))
        //        {
        //            if (!assetIds.Contains(assetId)) { assetIds.Add(assetId); }
        //        }
        //    }

        //    Property prop = new PropertyLayer().Get(propertyId);
        //    if (prop != null)
        //    {
        //        switch (prop.DataType)
        //        {
        //            case EDataType.PickList:
        //                return this.dal.Assets_GetGroupedByPickListValues(assetIds, propertyId);
        //            default:
        //                return new Dictionary<string, Dictionary<Guid, string>>();
        //        }
        //    }
        //    return new Dictionary<string, Dictionary<Guid, string>>();
        //}

        //public Dictionary<Guid, string> Assets_GetByPropertyValues(Guid assetTypeId, Dictionary<Guid, string> propsAndValues)
        //{
        //    return this.dal.Assets_GetByPropertyValues(assetTypeId, propsAndValues);
        //}

        //public Dictionary<Guid, string> Assets_GetByPropertyValues(Dictionary<Guid, string> propsAndValues)
        //{
        //    return this.dal.Assets_GetByPropertyValues(propsAndValues);
        //}

        /// <summary>
        /// Gets an asset matching on the asset's name and type
        /// </summary>
        /// <param name="assetName">name of the asset</param>
        /// <param name="assetTypeId">type id of the asset</param>
        /// <returns></returns>
        public XObject Asset_Get(string assetName, Guid assetTypeId)
        {
            Guid? assetId = this.GetIdByName(assetName, assetTypeId);
            if (assetId.HasValue)
            {
                return this.Get(assetId.Value);
            }
            return null;
        }

        public XObject Asset_Get(Guid assetId, Guid userId)
        {
            return this.dal.Get(assetId);

            //Asset a = this.dal.Get(assetId);

            //if (a != null)
            //{
            //    if (new MemberLayer().IsAdmin(userId))
            //    {
            //        a.AllowDelete = true;
            //        a.AllowEdit = true;
            //    }
            //    else
            //    {

            //        // default permissions to false; next section will adjust if allowed
            //        a.AllowDelete = false;
            //        a.AllowEdit = false;

            //        //RoleLayer rLayer = new RoleLayer();

            //        //// need a list of Roles that the current user is a member of
            //        //List<Guid> roleIds = rLayer.IdsForMember(userId);

            //        //Guid assetTypeId = new AssetTypeLayer().GetIdByAssetId(assetId);

            //        //foreach (KeyValuePair<Guid, string> kvp in roles)
            //        //{
            //        //    if (this.MatchesRoleFilters(assetId, assetTypeId, kvp.Key, userId))
            //        //    {
            //        //        if (a.IsInstance)
            //        //        {
            //        //            a.AllowDelete = rLayer.HasPermission(kvp.Key, EPermissionType.Delete);
            //        //            a.AllowEdit = rLayer.HasPermission(kvp.Key, EPermissionType.Edit);
            //        //        }
            //        //        else
            //        //        {
            //        //            a.AllowDelete = rLayer.HasPermission(kvp.Key, EPermissionType.Delete);
            //        //            a.AllowEdit = rLayer.HasPermission(kvp.Key, EPermissionType.Edit);
            //        //        }
            //        //    }
            //        //}
            //    }
            //}

            //return a;

        }

        #region alpha list

        public Guid AssetTypeId(Guid assetId)
        {
            return this.dal.AssetTypeId(assetId);
        }

        //public Asset Clone(Guid assetId, string newAssetName, List<Guid> propertyIds, Guid userId)
        //{
        //    Asset sourceAsset = this.Asset_Get(assetId, userId);
        //    if (sourceAsset == null) { throw new LogicalException("Invalid source asset id for cloning."); }

        //    Asset newAsset = new Asset();
        //    newAsset.Id = System.Guid.NewGuid();
        //    newAsset.Name = newAssetName;
        //    newAsset.Description = string.Empty;
        //    newAsset.Created = DateTime.Now;
        //    newAsset.CreatedBy = userId;
        //    newAsset.IsNew = true;
        //    newAsset.InstanceOfId = sourceAsset.InstanceOfId;
        //    newAsset.AssetTypeId = sourceAsset.AssetTypeId;
        //    newAsset.IsDirty = true;

        //    if (this.Save(newAsset))
        //    {
        //        PropertyValueList sourceValues = null;

        //        if ((propertyIds != null) && (propertyIds.Count > 0))
        //        {
        //            sourceValues = new PropertyValueLayer().PropertyValues_GetForAnAsset(assetId, propertyIds, false, false);

        //            if ((sourceValues != null) && (sourceValues.Count > 0))
        //            {

        //                PropertyValueSubmittal submittal = new PropertyValueSubmittal(newAsset.Id, newAsset.Name, userId);

        //                foreach (PropertyValue pv in sourceValues)
        //                {
        //                    PropertyValue newPv = new PropertyValue();
        //                    newPv.Id = System.Guid.NewGuid();
        //                    newPv.AssetId = newAsset.Id;
        //                    newPv.Created = submittal.Created;
        //                    newPv.CreatedBy = userId;
        //                    newPv.DisplayValue = pv.DisplayValue;
        //                    newPv.DisplayValueHtml = pv.DisplayValueHtml;
        //                    newPv.IsDirty = true;
        //                    newPv.IsNew = true;
        //                    newPv.Order = pv.Order;
        //                    newPv.Property = pv.Property;
        //                    newPv.PropertyId = pv.PropertyId;
        //                    newPv.SubmittalGroupId = submittal.Id;
        //                    newPv.Value = pv.Value;

        //                    submittal.PropertyValues.Add(newPv);
        //                }

        //                PropertyValue pvName = new PropertyValue(Constants.PropertyIds.AssetName, assetId, newAssetName, userId);
        //                pvName.Approved = submittal.Created;
        //                pvName.ApprovedBy = submittal.CreatedBy;

        //                PropertyValueSubmittalLayer pvsLayer = new PropertyValueSubmittalLayer();
        //                if (pvsLayer.Save(submittal, true, userId))
        //                {
        //                    return newAsset;
        //                }
        //            }
        //        }

        //        return newAsset;
        //    }

        //    return null;
        //}

        public bool Create(Guid userId, Guid assetId, string assetName, string assetDisplayValue, Guid assetTypeId, Guid? parentId, string description)
        {
            //bool isAutoApprove = new MemberLayer().MemberHasPermission(userId, assetTypeId, EPermissionType.AutoApproveNewAssetDefs);
            bool isAutoApprove = true;

            XObject newAsset = new XObject(assetId, assetName, assetTypeId, null, userId);
            newAsset.Description = description;
            if (!string.IsNullOrEmpty(assetDisplayValue)) { newAsset.DisplayValue = assetDisplayValue; }

            if (isAutoApprove)
            {
                newAsset.Approved = DateTime.Now;
                newAsset.ApprovedBy = userId;
            }

            //if (parentId.HasValue)
            //{
            //    // create the relation between this asset and the parent
            //    AssetRelation relation = new AssetRelation(parentId.Value, newAsset.Id, EAssetRelationType.ParentChild, userId);

            //    if (isAutoApprove)
            //    {
            //        relation.Approved = DateTime.Now;
            //        relation.ApprovedBy = userId;
            //    }

            //    newAsset.AssetMembers.Add(relation);
            //}

            return this.Save(newAsset);

        }

        public bool CreateInstance(Guid userId, Guid assetId, string assetName, Guid assetTypeId, Guid instanceOfId)
        {
            //bool isAutoApprove = new MemberLayer().MemberIsInRole(userId, new RoleLayer().GetRoleByName("Admin").Id);
            bool isAutoApprove = true;

            XObject newAsset = new XObject(assetId, assetName, assetTypeId, instanceOfId, userId);

            if (isAutoApprove)
            {
                newAsset.Approved = DateTime.Now;
                newAsset.ApprovedBy = userId;
            }

            return this.Save(newAsset);
        }

        /// <summary>
        /// Deletes an instance of an asset
        /// </summary>
        /// <param name="assetId">id of the asset to be deleted</param>
        /// <param name="userId">id of the user deleting this asset</param>
        /// <returns>true if successful; false otherwise</returns>
        public bool Delete(Guid objectId, Guid userId)
        {
            // if the asset is referenced by other assets (i.e. assetId is a product code) ...
            // disallow deletion
            return this.dal.Delete(objectId, userId);
        }

        /// <summary>
        /// Gets an instance of an asset matching the specified id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public XObject Get(Guid id)
        {
            return this.dal.Get(id);
        }

        //public Asset Get(Guid assetId, Guid userId)
        //{

        //}

        /// <summary>
        /// Gets a list of Ids of Assets for the specified AssetType
        /// </summary>
        /// <param name="assetTypeId"></param>
        /// <param name="typeClass"></param>
        /// <param name="includeChildAssetTypes"></param>
        /// <returns></returns>
        /// <remarks>1.05.06</remarks>
        //public List<Guid> GetAssetIdsByAssetTypeId(Guid assetTypeId, EAssetTypeClass typeClass, bool includeChildAssetTypes)
        //{
        //    return this.dal.AssetIds_Get(assetTypeId, typeClass, includeChildAssetTypes);
        //}

        /// <summary>
        /// Gets the id of an asset matching on the specified name and type
        /// </summary>
        /// <param name="assetName">name of the asset</param>
        /// <param name="assetTypeId">type id of the asset</param>
        /// <returns>id of the asset if exists; empty Guid otherwise</returns>
        public Guid? GetIdByName(string assetName, Guid assetTypeId)
        {
            return this.dal.Asset_GetId(assetName, assetTypeId);
        }

        public List<Guid> GetIdsByName(string assetName)
        {
            return this.dal.GetIdsByName(assetName);
        }

        public List<Guid> GetIdsByName(string assetName, Guid assetTypeId, bool searchChildAssetTypes)
        {
            if ((assetTypeId.CompareTo(new Guid()) != 0) && (searchChildAssetTypes))
            {
                List<Guid> assetTypeIds = new XObjectTypeLayer().AssetType_GetChildren(assetTypeId, true);
                assetTypeIds.Add(assetTypeId);
                return this.dal.AssetIds_Get(assetName, assetTypeIds);
            }
            else
            {
                return this.GetIdsByName(assetName);
            }
        }

        /// <summary>
        /// Gets a list of all asset ids and names who appear as a PropertyValue.Value for the provided property id
        /// </summary>
        /// <param name="propertyId"></param>
        /// <returns></returns>
        //public Dictionary<Guid, string> GetDictionaryFromPropertyValue(Guid propertyId)
        //{
        //    return this.dal.GetDictionaryFromPropertyValue(propertyId);
        //}

        //public Dictionary<Guid, string> GetDictionaryFromPropertyValue(Guid assetTypeId, Guid propertyId)
        //{
        //    return this.dal.GetDictionaryFromPropertyValue(assetTypeId, propertyId);
        //}

        public Guid? InstanceOfId(Guid assetId)
        {
            return this.dal.InstanceOfId(assetId);
        }

        public List<Guid> InstanceOfIds(List<Guid> assetIds)
        {
            return this.dal.InstanceOfIds(assetIds);
        }

        public bool IsInstance(Guid assetId)
        {
            return this.dal.IsInstance(assetId);
        }

        //public bool Kill(Guid assetId, Guid userId)
        //{
        //    if (!new MemberLayer().IsAdmin(userId)) { throw new LogicalException("Invalid permission to kill assets."); }
        //    return (this.dal.Kill(assetId) && new SqlDatabaseLayer().RemoveFromGenTables(assetId));
        //}

        /// <summary>
        /// Marks an item as having been update.  Generally called when propertyvalues have been saved for an <see cref="XObject"/>
        /// </summary>
        /// <param name="assetId">id of the <see cref="XObject"/></param>
        /// <param name="userId">if of the <see cref="Member"/> performing the update</param>
        /// <returns>true if successful; false otherwise</returns>
        public bool MarkAsUpdated(Guid assetId, Guid userId)
        {
            if (this.dal.MarkAsUpdated(assetId, userId))
            {
               // System.Threading.ThreadPool.QueueUserWorkItem(o => new SqlDatabaseLayer().UpdateGenTables(assetId));
                return true;
            }
            return false;
        }

        //public bool MatchesRoleFilters(Guid assetId, Guid assetTypeId, Guid roleId, EPermissionType permissionType)
        //{
        //    string roleViewName = new SqlDatabaseLayer().FormatGeneratedRoleViewName(roleId, assetTypeId, permissionType);
        //    return this.dal.MatchesRoleFilters(assetId, roleViewName);
        //}

        //public bool MatchesRoleFilters(Guid assetId, Guid assetTypeId, Guid roleId, Guid? userId)
        //{
        //    return this.dal.MatchesRoleFilters(assetId, assetTypeId, roleId, userId);
        //}

        //public bool Migrate(int tabIndex, Guid assetId, Core.Enumerations.EApplicationInstance source, Core.Enumerations.EApplicationInstance target)
        //{
            
        //    AssetLayer srcAssetLayer = new AssetLayer(source);
        //    AssetLayer tgtAssetLayer = new AssetLayer(target);

        //    PickListLayer srcPLLayer = new PickListLayer(source);
        //    PickListLayer tgtPLLayer = new PickListLayer(target);

        //    PickListValueLayer tgtPLVLayer = new PickListValueLayer(target);

        //    PropertyValueLayer srcPropValLayer = new PropertyValueLayer(source);
        //    PropertyValueLayer tgtPropValLayer = new PropertyValueLayer(target);

        //    Console.WriteLine("{0}Entered Migrate:\t{1}", new String('\t', tabIndex), srcAssetLayer.Name(assetId));

        //    tabIndex++;

        //    Asset asset = srcAssetLayer.Get(assetId);

        //    if (asset == null) { return false; }

        //    if (!tgtAssetLayer.IsValidId(assetId))
        //    {
        //        asset.IsNew = true;
        //        asset.IsDirty = true;

        //        tgtAssetLayer.Save(asset);
        //    }

        //    PropertyValueList srcPropVals = srcPropValLayer.PropertyValueList_GetByAssetId(assetId);

        //    Console.WriteLine("{0}Retrieved {1} property values for {2}", new String('\t', tabIndex), srcPropVals.Count, srcAssetLayer.Name(assetId));
        //    tabIndex++;

        //    PropertyValueSubmittal submittal = new PropertyValueSubmittal(asset.Id, asset.Name, Core.Constants.MemberIds.Admin);

        //    PropertyLayer propLayer = new PropertyLayer(target);

        //    foreach (PropertyValue pv in srcPropVals)
        //    {

        //        if (pv.Deleted.HasValue) { continue; }

        //        Property prop = propLayer.Get(pv.PropertyId);
                
        //        if (prop != null)
        //        {

        //            PropertyValue pvTarget = tgtPropValLayer.Get(pv.Id);
        //            if (pvTarget != null) { continue; }

        //            tabIndex--;
        //            Console.WriteLine("{0}{1}:\t{2}", new String('\t', tabIndex), prop.DataType, prop.DisplayValue);
        //            tabIndex++;

        //            switch (prop.DataType)
        //            {

        //                case EDataType.PickList:
        //                    #region PickList

        //                    Guid plvId;
        //                    if (Guid.TryParse(pv.Value, out plvId))
        //                    {
        //                        // does this value exist in the target pickList?
        //                        if (tgtPLVLayer.Get(plvId) == null)
        //                        {
        //                            Console.WriteLine("\tMigrating PickList:\t{0}", srcPLLayer.Name(prop.PickListId.Value));
        //                            srcPLLayer.Migrate(prop.PickListId.Value, target);
        //                        }
        //                    }
        //                    break;

        //                    #endregion

        //                case EDataType.User:
        //                    #region User

        //                    Guid uId;
        //                    if (Guid.TryParse(pv.Value, out uId))
        //                    {
        //                        Member m = new MemberLayer().Get(uId);
        //                        if (m != null)
        //                        {
        //                            m.IsDirty = true;
        //                            m.IsNew = true;
        //                            new MemberLayer(target).Save(m);
        //                            this.Migrate(tabIndex, m.Id, source, target);
        //                        }
        //                    }
        //                    break;

        //                    #endregion

        //                case EDataType.Dependency:
        //                case EDataType.Relation_ChildParent:
        //                case EDataType.Relation_Other:
        //                case EDataType.Relation_ParentChild:
        //                case EDataType.Asset:
        //                    #region Asset

        //                    Guid aId;
        //                    if (Guid.TryParse(pv.Value, out aId))
        //                    {
        //                        Asset a = srcAssetLayer.Get(aId);
        //                        Console.WriteLine("{0}{1}:\t{1}", new String('\t', tabIndex), prop.DataType, a.Name);
        //                        tabIndex++;
        //                        if (a != null)
        //                        {

        //                            // does the asset have a parent?  and if so, does it exist?
        //                            Guid? parentId = srcAssetLayer.ParentId(aId);
        //                            if (parentId.HasValue)
        //                            {
        //                                if (!tgtAssetLayer.IsValidId(parentId.Value))
        //                                {
        //                                    Console.WriteLine("{0}Migrating Parent:\t{1}", new String('\t', tabIndex), srcAssetLayer.Name(parentId.Value));
        //                                    srcAssetLayer.Migrate(tabIndex, parentId.Value, source, target);
        //                                }
        //                            }

        //                            a.IsDirty = true;
        //                            a.IsNew = true;
        //                            tgtAssetLayer.Save(a);
        //                            srcAssetLayer.Migrate(tabIndex, a.Id, source, target);
        //                        }
        //                    }
        //                    break;

        //                    #endregion

        //                case EDataType.Document:
        //                    #region Doc

        //                    Guid docId;
        //                    if (Guid.TryParse(pv.Value, out docId))
        //                    {
        //                        Document doc = new DocumentLayer(source).Get(string.Empty, docId, false);
        //                        Document tgtDoc = new DocumentLayer(target).Get(string.Empty, docId, true);

        //                        if ((doc != null) && (tgtDoc == null))
        //                        {
        //                            doc.IsDirty = true;
        //                            doc.IsNew = true;
        //                            new DocumentLayer(target).Save(doc);
        //                        }
        //                    }
        //                    break;

        //                    #endregion

        //                case EDataType.Currency:
        //                    #region Currency

        //                    Guid cvId;
        //                    if (Guid.TryParse(pv.Value, out cvId))
        //                    {
        //                        CurrencyValue cv = new CurrencyValueLayer().CurrencyValue_Get(cvId);
        //                        if (cv != null)
        //                        {
        //                            new CurrencyValueLayer(target).CurrencyValue_Save(cv);
        //                        }
        //                    }
        //                    break;

        //                    #endregion

        //                case EDataType.Image:
        //                    #region Image

        //                    Guid imgId;
        //                    if (Guid.TryParse(pv.Value, out imgId))
        //                    {
        //                        Image img = new ImageLayer(source).Get(imgId, false);
        //                        Image tgtImg = new ImageLayer(target).Get(imgId, true);

        //                        if ((img != null) && (tgtImg == null))
        //                        {
        //                            img.IsDirty = true;
        //                            img.IsNew = true;
        //                            new ImageLayer(target).Save(img);
        //                        }
        //                    }
        //                    break;

        //                    #endregion

        //                default:
        //                    // do nothing; just save the property value
        //                    break;
        //            }

        //            submittal.PropertyValues.Add(pv.Clone(submittal.Id, Core.Constants.MemberIds.Admin));
        //        }
        //        else
        //        {
        //            Console.WriteLine("Property not found!");
        //        }

        //    }

        //    if (submittal.PropertyValues.Count > 0)
        //    {
        //        Console.WriteLine("{0}Saving {1} property values for {2}", new String('\t', tabIndex), submittal.PropertyValues.Count, asset.Name);
        //        Console.WriteLine("{0}Exited Migrate:\t{1}", new String('\t', tabIndex), srcAssetLayer.Name(assetId));
        //        return new PropertyValueSubmittalLayer(target).Save(submittal, true, Core.Constants.MemberIds.Admin);
        //    }

        //    return true;
        //}

        public Guid? ParentId(Guid assetId)
        {
            return this.dal.ParentId(assetId);
        }
        
        /// <summary>
        /// Renames an asset.  Should not be used alone.  Consumed by PropertyValueSubmittalLayer.PropertyValueSubmittal_Approve()
        /// </summary>
        /// <param name="assetId">id of the asset to be renamed</param>
        /// <param name="newName">the new name for the asset</param>
        /// <returns></returns>
        internal bool Rename(Guid assetId, string newName)
        {
            return this.dal.Asset_Rename(assetId, newName);
        }

        /// <summary>
        /// Saves an instance of an asset; method also performs validation
        /// </summary>
        /// <param name="asset">an asset instance</param>
        /// <returns>true if successful; false otherwise; also may throw LogicalException due to
        /// business rule violations</returns>
        public bool Save(XObject asset)
        {

            bool isNew = !this.ValidId(asset.Id); // don't rely on Asset.IsNew - check the Asset's Id

            if (this.dal.Save(asset))
            {
                //if (SystemFrameworkHelper.NotificationsEnabled && isNew)
                //{
                //    return new NotificationLayer().Notification_SendForAssetCreation(asset);
                //}
                return true;
            }

            return false;
        }

        #endregion

        //public List<Asset> Asset_Get(string assetName, bool includeInstances)
        //{
        //    return this.dal.Asset_Get(assetName, includeInstances);
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assetTypeId"></param>
        /// <param name="requestType"></param>
        /// <param name="searchTerm">optional</param>
        /// <returns></returns>
        //public Dictionary<Guid, string> GetDictionary(Guid assetTypeId, EAssetRequestType requestType, string searchTerm)
        //{
        //    string tblName = new SqlDatabaseLayer().FormatGeneratedTableName(assetTypeId, (requestType == EAssetRequestType.Instance), false);
        //    string assetTypeName = new AssetTypeLayer().Name(assetTypeId);
        //    return this.dal.GetDictionary(tblName, assetTypeName, searchTerm);
        //}

        /// <summary>
        /// Gets all assets of the specified type
        /// </summary>
        /// <param name="assetTypeId"></param>
        /// <param name="includeDescriptions"></param>
        /// <param name="searchChildren"></param>
        /// <param name="useDisplayValues"></param>
        /// <returns></returns>
        //public Dictionary<Guid, string> Assets_GetDictionary(Guid assetTypeId, bool includeDescriptions, bool searchChildren, bool useDisplayValues)
        //{

        //    AssetTypeLayer atLayer = new AssetTypeLayer();

        //    if (Constants.AssetTypeIds.User.CompareTo(assetTypeId) == 0)
        //    {
        //        return new MemberLayer().Members_GetDictionary(false);
        //    }

        //    //else if (atLayer.GetNameById(assetTypeId) == "Address")
        //    //{
        //    //    // TODO: http://developer.yahoo.com/geo/placefinder/
        //    //    return this.dal.Addresses_Get();
        //    //}
        //    else
        //    {
        //        // TODO: Determine correctness of hard-coded 'false'
        //        if (atLayer.HasAssets(assetTypeId, EAssetRequestType.Both, false))
        //        {
        //            return this.dal.Assets_GetDictionary(assetTypeId, includeDescriptions, useDisplayValues);
        //        }
        //        else
        //        {
        //            if (searchChildren)
        //            {
        //                List<Guid> childIds = atLayer.AssetType_GetChildren(assetTypeId, true);
        //                return this.dal.Assets_GetDictionary(childIds, EAssetRequestType.Definition, includeDescriptions);
        //            }
        //            else
        //            {
        //                return new Dictionary<Guid, string>();
        //            }
        //        }
        //    }
        //}

        //public Dictionary<Guid, string> AssetInstances_Get(List<Guid> assetTypeIds)
        //{
        //    return this.dal.AssetInstances_Get(assetTypeIds);
        //}

        //public int Count(Guid assetTypeId, EAssetRequestType requestType, bool includeChildren)
        //{
        //    SqlDatabaseLayer dbLayer = new SqlDatabaseLayer();
        //    string tblName = dbLayer.FormatGeneratedTableName(assetTypeId, (requestType == EAssetRequestType.Instance), false);
        //    return dbLayer.Count(tblName);
        //}

        //public Dictionary<string, string> Assets_Get(Guid assetTypeId, bool includeChildren, EAssetRequestType requestType)
        //{
        //    if (requestType == EAssetRequestType.Both) { throw new Exception("Not allowed."); }
        //    return this.dal.Assets_Get(assetTypeId, includeChildren, requestType);
        //}

        /// <summary>
        /// Gets all asset instances of the specified type
        /// </summary>
        /// <param name="assetTypeId">the type of asset instances to retrieve</param>
        /// <param name="includeChildren">include assetTypes that are children of the supplied asset type id</param>
        /// <returns></returns>
        //public Dictionary<Guid, string> AssetInstances_Get(Guid assetTypeId, bool includeChildren)
        //{
        //    if (includeChildren)
        //    {
        //        List<Guid> ids = new AssetTypeLayer().AssetType_GetChildren(assetTypeId, true);
        //        return this.dal.AssetInstances_Get(ids);
        //    }
        //    else
        //    {
        //        return this.dal.AssetInstances_Get(assetTypeId);
        //    }
        //}

        //public Dictionary<Guid, string> AssetInstances_GetForAUser(Guid userId, Guid assetTypeId, Guid viewId)
        //{
        //    RoleLayer rLayer = new RoleLayer();

        //    AssetTypeLayer atLayer = new AssetTypeLayer();

        //    if (rLayer.ContainsMember(userId, Core.Constants.RoleIds.Admin))
        //    {
        //        // TODO: Determine correctness of hard-coded 'false'
        //        if (atLayer.HasAssets(assetTypeId, EAssetRequestType.Instance, false))
        //        {
        //            return this.AssetInstances_Get(assetTypeId, false);
        //        }
        //        else
        //        {
        //            return this.AssetInstances_Get(assetTypeId, true);
        //        }
        //    }
        //    else
        //    {
        //        // need all the roles with access to this view AND this user is a member
        //        List<Guid> roleIds = new List<Guid>();
        //        foreach (KeyValuePair<Guid, string> kvp in rLayer.Roles_GetByView(viewId))
        //        {
        //            roleIds.Add(kvp.Key);
        //        }
        //        return this.dal.Assets_Get(userId, assetTypeId, null, roleIds, EAssetRequestType.Instance);
        //    }
        //}

        //public Dictionary<Guid, string> Assets_Get(Guid userId, Guid parentAssetTypeId, List<Guid> childAssetTypeIds, List<Guid> roleIds, EAssetRequestType requestType)
        //{
        //    return this.dal.Assets_Get(userId, parentAssetTypeId, childAssetTypeIds, roleIds, requestType);
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId">a valid member id</param>
        /// <param name="assetTypeId"></param>
        /// <param name="viewId">a valid view id</param>
        /// <returns></returns>
        //public Dictionary<Guid, string> Assets_GetForAUser(Guid userId, Guid assetTypeId, Guid viewId)
        //{
        //    // TODO: Put this logic into a flowchart and check into source control for future reference

        //    ViewLayer viewLayer = new ViewLayer();
        //    MemberLayer memberLayer = new MemberLayer();
        //    RoleLayer roleLayer = new RoleLayer();
        //    PropertyLayer propLayer = new PropertyLayer();
        //    PropertyValueLayer pvLayer = new PropertyValueLayer();
        //    AssetLayer assetLayer = new AssetLayer();
        //    AssetTypeLayer atLayer = new AssetTypeLayer();

        //    View vw = viewLayer.View_Get(viewId, userId);
        //    if (vw == null) { throw new LogicalException("Invalid ViewId", "viewId"); }

        //    // 1. Is the user an admin?  If so, return all of the assets
        //    Role role = roleLayer.GetRoleByName("Admin");
        //    if (role == null) { throw new LogicalException("Invalid Role Name", "Admin"); }

        //    if (memberLayer.MemberIsInRole(userId, role.Id))
        //    {

        //        AssetType at = atLayer.AssetType_Get(assetTypeId);
        //        if (at == null) { throw new LogicalException("Invalid AssetTypeId", "assetTypeId"); }

        //        if (vw.IsInstance)
        //        {
        //            if ((at.AssetTypes != null) && (at.AssetTypes.Count > 0))
        //            {
        //                List<Guid> assetTypes = new List<Guid>();
        //                foreach (KeyValuePair<Guid, string> kvp in atLayer.AssetType_GetLowestLevelParents(assetTypeId))
        //                {
        //                    assetTypes.Add(kvp.Key);
        //                }
        //                return this.Assets_GetInstances(assetTypes);
        //            }
        //            else
        //            {
        //                return this.Assets_GetInstances(assetTypeId, false); // TODO: Not sure about including child asset types or not
        //            }                        
        //        }
        //        else
        //        {

        //            Dictionary<Guid, string> assetsOfViewsAssetType = this.Assets_GetDictionary(assetTypeId, false);

        //            if ((assetsOfViewsAssetType != null) && (assetsOfViewsAssetType.Count > 0))
        //            {
        //                return this.Assets_GetDictionary(assetTypeId, false);
        //            }

        //            if ((at.AssetTypes != null) && (at.AssetTypes.Count > 0))
        //            {
        //                List<Guid> assetTypes = new List<Guid>();
        //                foreach (KeyValuePair<Guid, string> kvp in atLayer.AssetType_GetLowestLevelParents(assetTypeId))
        //                {
        //                    assetTypes.Add(kvp.Key);
        //                }
        //                return this.Assets_GetDictionary(assetTypes);
        //            }
        //            else
        //            {
        //                return this.Assets_GetDictionary(assetTypeId, false);
        //            }                        
        //        }                    
        //    }
        //    else
        //    {

        //        // 2. Get all of the roles with which this view is associated
        //        Dictionary<Guid, string> viewRoles = roleLayer.Roles_GetByView(viewId);

        //        // 3. Get all of the roles with which this member is associated
        //        Dictionary<Guid, string> userRoles = roleLayer.Roles_Get(userId, false);

        //        // create a container for the final list of roles
        //        List<Guid> allRoles = new List<Guid>();

        //        // 4a. Filter out view roles not associated with the user
        //        foreach (KeyValuePair<Guid, string> tempRole in viewRoles)
        //        {
        //            if (userRoles.ContainsKey(tempRole.Key))
        //            {
        //                if (!allRoles.Contains(tempRole.Key)) { allRoles.Add(tempRole.Key); }
        //            }
        //        }

        //        // 4b. Filter out user roles not associated with the view
        //        foreach (KeyValuePair<Guid, string> tempRole in userRoles)
        //        {
        //            if (viewRoles.ContainsKey(tempRole.Key))
        //            {
        //                if (!allRoles.Contains(tempRole.Key)) { allRoles.Add(tempRole.Key); }
        //            }
        //        }

        //        // 5. Now we have a list of all of the roles for this view to which the user has permissions
        //        // Next, we need a list of all of the properties associated with this view
        //        // that are of DataType = Role and that role is in the list of role ids
        //        // produced at the end of step 4b.

        //        List<Guid> targetProperties = new List<Guid>();

        //        foreach (PropertyGroup pg in viewLayer.View_Get(viewId, userId).PropertyGroups)
        //        {
        //            foreach (Property prop in pg.Properties)
        //            {
        //                if ((prop.DataType == EDataType.User) && (prop.RoleId.HasValue) && (allRoles.Contains(prop.RoleId.Value)))
        //                {
        //                    if (!targetProperties.Contains(prop.Id))
        //                    {
        //                        targetProperties.Add(prop.Id);
        //                    }
        //                }
        //            }
        //        }

        //        // 6. Get a list of all assets whose current PropertyValue.Value
        //        // for any properties obtained in step 5, equals the supplied member id

        //        Dictionary<Guid, string> targetValues = new Dictionary<Guid, string>();

        //        foreach (Guid prop in targetProperties)
        //        {
        //            targetValues.Add(prop, userId.ToString());
        //        }

        //        if (targetValues.Count > 0)
        //        {
        //            return this.Assets_GetByPropertyValues(assetTypeId, targetValues);
        //        }
        //        else
        //        {
        //            return new Dictionary<Guid, string>();
        //        }                    

        //    }
        //}

        //public Dictionary<Guid, string> Assets_GetMatching(List<Guid> assetTypeIds,
        //                                                   string filterExpression,
        //                                                   List<Filter> propsAndValues,
        //                                                   EAssetRequestType requestType)
        //{

        //    PropertyLayer pLayer = new PropertyLayer();
        //    Dictionary<Guid, Property> properties = new Dictionary<Guid, Property>();

        //    // if the request type is Instance, but some of the filter properties are from the Definitions ...
        //    if (requestType == EAssetRequestType.Instance)
        //    {

        //        AssetTypeLayer atLayer = new AssetTypeLayer();

        //        for (int i = 0; i < propsAndValues.Count; i++)
        //        {
        //            Filter filter = propsAndValues[i];
        //            Property prop = pLayer.Get(filter.PropertyId);
        //            if (!properties.ContainsKey(prop.Id)) { properties.Add(prop.Id, prop); }

        //            switch (prop.DataType)
        //            {
        //                case EDataType.Asset:
        //                    if (prop.IsSystem)
        //                    {

        //                    }
        //                    else
        //                    {
        //                        foreach (AssetTypePropertyRelation relation in atLayer.AssetTypePropertyRelations_Get(assetTypeIds, filter.PropertyId))
        //                        {
        //                            if (relation != null)
        //                            {
        //                                if (!relation.IsInstance)
        //                                {
        //                                    filter.FromInstanceOfAsset = true;
        //                                    // request for assets is for instances, but the property is at the definition level
        //                                }
        //                            }
        //                        }
        //                        //foreach (Guid assetTypeId in assetTypeIds)
        //                        //{
        //                        //    AssetTypePropertyRelation relation = atLayer.AssetTypePropertyRelation_Get(assetTypeId, filter.PropertyId);
        //                        //    if (relation != null)
        //                        //    {
        //                        //        if (!relation.IsInstance)
        //                        //        {
        //                        //            filter.FromInstanceOfAsset = true;
        //                        //            // request for assets is for instances, but the property is at the definition level
        //                        //        }
        //                        //    }
        //                        //}
        //                    }
        //                    break;
        //                //case EDataType.System_AssetName:
        //                //    // ignore this one
        //                //    break;
        //                case EDataType.System_AssetType:
        //                    // ignore this one
        //                    break;
        //                case EDataType.System_Description:
        //                    // ignore this one
        //                    break;
        //                //case EDataType.System_InstanceOf:
        //                //    // don't think we can ignore, but not sure how to handle it
        //                //    break;
        //                case EDataType.System_InstanceOfDesc:
        //                    // don't think we can ignore, but not sure how to handle it
        //                    break;
        //                default:

        //                    foreach (Guid assetTypeId in assetTypeIds)
        //                    {
        //                        AssetTypePropertyRelation relation = atLayer.AssetTypePropertyRelation_Get(assetTypeId, filter.PropertyId);
        //                        if (relation != null)
        //                        {
        //                            if (!relation.IsInstance)
        //                            {
        //                                filter.FromInstanceOfAsset = true;
        //                                // request for assets is for instances, but the property is at the definition level
        //                            }
        //                        }
        //                    }

        //                    break;

        //            }
        //        }
        //    }
        //    else
        //    {
        //        for (int i = 0; i < propsAndValues.Count; i++)
        //        {
        //            Filter filter = propsAndValues[i];
        //            Property prop = pLayer.Get(filter.PropertyId);
        //            if (!properties.ContainsKey(prop.Id)) { properties.Add(prop.Id, prop); }
        //        }
        //    }

        //    return this.dal.Assets_GetMatching(assetTypeIds, filterExpression, propsAndValues, properties, requestType);

        //}

        //public Dictionary<Guid, string> Assets_GetMatching(List<Guid> assetIds, Guid propertyId, string propertyValue, bool includeDescriptions)
        //{
        //    if ((assetIds == null) || (assetIds.Count == 0)) { return new Dictionary<Guid, string>(); }

        //    Property prop = new PropertyLayer().Get(propertyId);
        //    if (prop == null) { return new Dictionary<Guid, string>(); }

        //    Asset a = this.Get(assetIds[0]);

        //    AssetTypePropertyRelation relation = null;

        //    bool useInstanceOfValues = false;
        //    bool exactMatch = true;

        //    if (prop.IsSystem)
        //    {
        //        useInstanceOfValues = a.IsInstance;
        //        exactMatch = false;
        //        includeDescriptions = true;

        //        switch (prop.SystemType)
        //        {
        //            case ESystemType.AssetType:
        //                return this.dal.Assets_GetMatching(assetIds, new Guid(propertyValue), includeDescriptions);
        //            case ESystemType.InstanceOf:
        //                return this.dal.Assets_GetByInstanceOfId(new Guid(propertyValue));
        //            case ESystemType.CreatedBy:
        //            case ESystemType.DeletedBy:
        //            case ESystemType.LastModifiedBy:
        //                return this.dal.Assets_GetMatching(assetIds, prop.SystemType, propertyValue);
        //            default:
        //                return this.dal.Assets_GetMatching(assetIds, propertyId, propertyValue, includeDescriptions, useInstanceOfValues, exactMatch);
        //        }
        //    }
        //    else
        //    {
        //        relation = new AssetTypePropertyRelationDal().AssetTypePropertyRelation_Get(a.AssetTypeId, prop.Id);
        //        if (relation == null) { return new Dictionary<Guid, string>(); }
        //        useInstanceOfValues = (!relation.IsInstance) && (a.IsInstance);

        //        return this.dal.Assets_GetMatching(assetIds, propertyId, propertyValue, includeDescriptions, useInstanceOfValues, exactMatch);
        //    }

        //}

        /// <summary>
        /// Gets a list of assets who are children of the specified asset id
        /// For example, a list of software releases (versions) for a particular application 
        /// could be obtained by providing the parent's asset id
        /// i.e. For all .Net Framework versions, pass in the id of the Microsoft .Net asset which is of
        /// asset type = "Software"
        /// Returns a list of child assets
        /// Example: If you passed in the id for "Microsoft .Net Framework", you would
        /// get the following:
        /// [someGuid][.Net Framework 1.0]
        /// [someGuid][.Net Framework 2.0]
        /// </summary>
        /// <param name="parentAssetId"></param>
        /// <returns></returns>
        //public Dictionary<Guid, string> Asset_GetChildren(Guid parentAssetId)
        //{
        //    return this.dal.Asset_GetChildren(parentAssetId);
        //}

        public Dictionary<Guid, string> Assets_Get(List<Guid> assetIds)
        {
            return this.dal.Assets_Get(assetIds);
        }

        //public Dictionary<Guid, string> Assets_SearchByName(Guid assetTypeId, EAssetRequestType requestType, string searchValue, bool includeChildAssetTypes, bool includeDescriptions)
        //{

        //    List<Guid> assetIds = new List<Guid>();

        //    if (includeChildAssetTypes)
        //    {
        //        assetIds = new AssetTypeLayer().AssetType_GetChildren(assetTypeId, true);
        //    }
        //    else
        //    {
        //        assetIds.Add(assetTypeId);
        //    }

        //    return this.dal.Assets_SearchByName(assetIds, requestType, searchValue, includeDescriptions);

        //}

        public bool Assets_ChangeAssetType(Guid instanceOfId, Guid newAssetTypeId)
        {
            return this.dal.Assets_ChangeAssetType(instanceOfId, newAssetTypeId);
        }

    }

}