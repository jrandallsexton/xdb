
using System;
using System.Text;
using System.Collections.Generic;
using System.Data.SqlClient;

using XDB.Common;
using XDB.Common.Constants;
using XDB.Common.Interfaces;
using XDB.Common.Enumerations;

using XDB.Models;

namespace XDB.Repositories
{

    public class XValueRepository<T> : XBaseDal, IXValueRepository<T> where T : XBase, IXValue
    {

        public XValueRepository() : base(ECommonObjectType.XObject) { }

        //public XPropertyDomain XPropDomain { get; set; }

        //public PropertyValueDal(string connString) { this.ConnectionString = connString; }

        //#region private members

        //private const string spPropertyValue_Get_IgnoreDeletedFlag = "spr_PropertyValue_Get_IgnoreDeletedFlag";

        ////Collection SPs
        //private const string spPropertyValueList_Save = "spr_PropertyValueList_Save";
        //private const string spPropertyValueList_Delete = "spr_PropertyValueList_Delete";

        ////Collection by FK SPs
        //private const string spPropertyValueList_Get = "spr_PropertyValueList_Get";
        //private const string spPropertyValueList_GetByPropertyId = "spr_PropertyValueList_GetByPropertyId";
        //private const string spPropertyValueList_DeleteByPropertyId = "spr_PropertyValueList_DeleteByPropertyId";

        ////Collection by FK SPs
        //private const string spPropertyValueList_GetByAssetId = "spr_PropertyValueList_GetByAssetId";
        //private const string spPropertyValueList_GetBySubmittalGroupId = "spr_PropertyValueList_GetBySubmittalGroupId";
        //private const string spPropertyValueList_DeleteByAssetId = "spr_PropertyValueList_DeleteByAssetId";

        //private const string spPropertyValueList_DeleteByValue = "spr_PropertyValueList_DeleteByValue";

        //#endregion

        //internal PropertyValue PropertyValue_Get_IgnoreDeletedFlag(Guid propertyValueId)
        //{
        //    return this.PropertyValue_Get(propertyValueId, spPropertyValue_Get_IgnoreDeletedFlag);
        //}

        /// <summary>
        /// Gets an instance of a property value
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IXValue Get(Guid id)
        {
            return this.PropertyValue_Get(id, StoredProcs.PropertyValue_Get);
        }

        public Guid AssetId(Guid propertyValueId)
        {
            const string sql = "SELECT [AssetId] FROM [PropertyValues] WHERE [Id] = @Id";
            return base.ExecuteScalarGuidInLine(sql, new List<SqlParameter> { new SqlParameter("@Id", propertyValueId) });
        }

        public IXValue PropertyValue_Get(Guid xValueId, string spName)
        {

            XValue pv = null;

            using (SqlDataReader drdSql = base.OpenDataReader(spName, new List<SqlParameter> { new SqlParameter("@Id", xValueId) }))
            {

                if ((drdSql != null) && (drdSql.HasRows))
                {

                    pv = new XValue();

                    drdSql.Read();

                    int propertyId = drdSql.GetOrdinal("PropertyId");
                    int assetId = drdSql.GetOrdinal("AssetId");
                    int submittalGroupId = drdSql.GetOrdinal("SubmittalGroupId");
                    int value = drdSql.GetOrdinal("Value");
                    int idx = drdSql.GetOrdinal("Index");
                    int created = drdSql.GetOrdinal("Created");
                    int approved = drdSql.GetOrdinal("Approved");
                    int createdBy = drdSql.GetOrdinal("CreatedBy");
                    int approvedBy = drdSql.GetOrdinal("ApprovedBy");
                    int rejected = drdSql.GetOrdinal("Rejected");
                    int rejectedBy = drdSql.GetOrdinal("RejectedBy");
                    int deleted = drdSql.GetOrdinal("Deleted");
                    int deletedBy = drdSql.GetOrdinal("DeletedBy");

                    if (!drdSql.IsDBNull(propertyId)) { pv.PropertyId = drdSql.GetGuid(propertyId); }

                    if (!drdSql.IsDBNull(assetId)) { pv.AssetId = drdSql.GetGuid(assetId); }

                    if (!drdSql.IsDBNull(submittalGroupId)) { pv.SubmittalGroupId = drdSql.GetGuid(submittalGroupId); }

                    if (!drdSql.IsDBNull(value)) { pv.Value = drdSql.GetString(value).Trim(); }

                    if (!drdSql.IsDBNull(idx)) { pv.Index = drdSql.GetInt32(idx); }

                    if (!drdSql.IsDBNull(created)) { pv.Created = drdSql.GetDateTime(created); }

                    if (!drdSql.IsDBNull(approved)) { pv.Approved = drdSql.GetDateTime(approved); }

                    if (!drdSql.IsDBNull(rejected)) { pv.Rejected = drdSql.GetDateTime(rejected); }

                    if (!drdSql.IsDBNull(createdBy)) { pv.CreatedBy = drdSql.GetGuid(createdBy); }

                    if (!drdSql.IsDBNull(approvedBy)) { pv.ApprovedBy = drdSql.GetGuid(approvedBy); }

                    if (!drdSql.IsDBNull(rejectedBy)) { pv.RejectedBy = drdSql.GetGuid(rejectedBy); }

                    if (!drdSql.IsDBNull(deleted)) { pv.Deleted = drdSql.GetDateTime(deleted); }

                    if (!drdSql.IsDBNull(deletedBy)) { pv.DeletedBy = drdSql.GetGuid(deletedBy); }

                    pv.Id = xValueId;
                    pv.IsNew = false;
                    pv.IsDirty = false;

                }

            }

            return pv;
        }

        //public string PopulateDisplayValue(string propertyValue, XProperty prop)
        //{

        //    if (string.IsNullOrEmpty(propertyValue)) { return string.Empty; }

        //    Guid selectedId;
        //    Guid.TryParse(propertyValue, out selectedId);

        //    if (prop != null)
        //    {
        //        if (prop.IsSystem)
        //        {
        //            switch (prop.SystemType)
        //            {
        //                case ESystemType.AssetType: return new XObjectTypeDal().Name(selectedId);

        //                case ESystemType.CreatedBy:
        //                case ESystemType.DeletedBy:
        //                    // get the formatted name of the user
        //                    XUser m = new XUserDal().Get(selectedId);

        //                    if (m != null)
        //                    {
        //                        if (m.MiddleInitial.HasValue)
        //                        {
        //                            return string.Format("{0}, {1} {2} [{3}]", m.LastName, m.FirstName, m.MiddleInitial.Value.ToString(), m.UserId);
        //                        }
        //                        else
        //                        {
        //                            return string.Format("{0}, {1} [{2}]", m.LastName, m.FirstName, m.UserId);
        //                        }
        //                    }

        //                    break;
        //                default:
        //                    return propertyValue;
        //            }
        //        }
        //        else
        //        {
        //            switch (prop.DataType)
        //            {

        //                case EDataType.Relation_ChildParent:
        //                case EDataType.Relation_Other:
        //                case EDataType.Relation_ParentChild:
        //                case EDataType.Dependency: // get the display value or name of the asset
        //                case EDataType.Asset: // get the display value or name of the asset

        //                    #region different properties whose values are an asset

        //                    XObject asset = new XObjectDal().Get(selectedId);
        //                    if (asset != null)
        //                    {
        //                        return string.IsNullOrEmpty(asset.DisplayValue) ? asset.Name : asset.DisplayValue;
        //                    }

        //                    break;

        //                    #endregion

        //                case EDataType.User:

        //                    #region user

        //                    // get the formatted name of the user
        //                    XUser m = new XUserDal().Get(selectedId);

        //                    if (m != null)
        //                    {
        //                        if (m.MiddleInitial.HasValue)
        //                        {
        //                            return string.Format("{0}, {1} {2} [{3}]", m.LastName, m.FirstName, m.MiddleInitial.Value.ToString(), m.UserId);
        //                        }
        //                        else
        //                        {
        //                            return string.Format("{0}, {1} [{2}]", m.LastName, m.FirstName, m.UserId);
        //                        }
        //                    }

        //                    break;

        //                    #endregion

        //                case EDataType.Document:

        //                    #region document

        //                    XDocument doc = new XDocumentDal().Get(selectedId, true);

        //                    if (doc != null)
        //                    {
        //                        return doc.Title;
        //                    }
        //                    else
        //                    {
        //                        return string.Empty;
        //                    }

        //                    #endregion

        //                case EDataType.Image:

        //                    #region image

        //                    XImage img = new XImageDal().Get(selectedId, true);

        //                    if (img != null)
        //                    {
        //                        return img.Name;
        //                    }
        //                    else
        //                    {
        //                        return string.Empty;
        //                    }

        //                    #endregion

        //                case EDataType.PickList:

        //                    throw new Exception("NOT IMPLEMENTED DUDE!!!!");
        //                    //return new PicklistDal().DisplayValue(prop.PickListId.Value, propertyValue);

        //                case EDataType.Currency:

        //                    #region currency

        //                    XMoney cv = new XMoneyDal().Get(selectedId);

        //                    if (cv != null)
        //                    {
        //                        if (cv.SymbolText.Length > 1)
        //                        {
        //                            // put a space in between larger ones
        //                            return string.Format("{0} {1}", cv.SymbolAscii, cv.Amount);
        //                        }
        //                        // no space for simple Dollar, Euro, Yen signs
        //                        return string.Format("{0}{1}", cv.SymbolAscii, cv.Amount);
        //                    }
        //                    return propertyValue;

        //                    #endregion

        //                case EDataType.System_AssetType:

        //                    #region asset type

        //                    return new XObjectTypeDal().Name(selectedId);

        //                    #endregion

        //                case EDataType.URL:

        //                    #region url

        //                    XUrl url = new XUrlDal().Get(selectedId);
        //                    if (url != null)
        //                    {
        //                        if (string.IsNullOrEmpty(url.Name))
        //                        {
        //                            return url.Url;
        //                        }
        //                        else
        //                        {
        //                            return string.Format("{0}|{1}", url.Name, url.Url);
        //                        }
        //                    }
        //                    return string.Empty;

        //                    #endregion

        //                default:

        //                    return propertyValue;
        //            }
        //        }

        //    }

        //    return string.Empty;

        //}

        public string PopulateDisplayValueHtml(T xValue, IXProperty xProperty)
        {
            // load the display value for the property value if needed
            if ((xValue != null) && (!string.IsNullOrEmpty(xValue.Value)))
            {

                Guid selectedId;

                if (Guid.TryParse(xValue.Value, out selectedId))
                {

                    if (xProperty != null)
                    {
                        switch (xProperty.DataType)
                        {

                            case EDataType.Relation_ChildParent:
                            case EDataType.Relation_Other:
                            case EDataType.Relation_ParentChild:
                            case EDataType.Dependency: // get the display value or name of the asset
                            case EDataType.Asset: // get the display value or name of the asset
                                return string.Empty;
                            case EDataType.Currency:
                                string currVal = string.Empty;
                                XMoney cv = new XMoneyRepository().Get(new Guid(xValue.Value));
                                if (cv != null)
                                {
                                    currVal = string.Format("{0}^{1}", cv.SymbolAscii, cv.Amount);
                                }
                                return currVal;
                            case EDataType.User:
                                return string.Empty;
                            case EDataType.Document:
                                return string.Empty;
                            case EDataType.Image:
                                return string.Empty;
                            case EDataType.String:
                                return string.Empty;
                            case EDataType.System_AssetType:
                                return string.Empty;
                            case EDataType.URL:
                                XUrl url = new XUrlRepository().Get(new Guid(xValue.Value));
                                if (url != null)
                                {
                                    if (string.IsNullOrEmpty(url.Name))
                                    {
                                        return string.Format("<a href='{0}' target='_new'>{1}</a>", url.Url, url.Url);
                                    }
                                    else
                                    {
                                        return string.Format("<a href='{0}' target='_new'>{1}</a>", url.Url, url.Name);
                                    }
                                }
                                return xValue.Value;
                            default:
                                return xValue.Value;
                        }

                    }
                }

            }

            return string.Empty;
        }

        /// <summary>
        /// Deletes any occurences of the specified match that aren't already marked as deleted
        /// </summary>
        /// <param name="assetId"></param>
        /// <param name="propertyId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public bool PropertyValue_Delete(Guid assetId, Guid propertyId, Guid userId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>
                {
                    new SqlParameter("@PropertyId", propertyId),
                    new SqlParameter("@DeletedBy", userId),
                    new SqlParameter("@AssetId", assetId)
                };

            return base.ExecuteSql(StoredProcs.PropertyValue_DeleteForAsset, paramList);
        }

        public bool PropertyValue_Exists(Guid propertyId, Guid assetId, string value, bool isCaseSensitive)
        {

            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT COUNT(*)");
            sql.AppendLine("FROM [PropertyValues]");
            sql.AppendLine("WHERE [PropertyId] = @PropertyId");
            sql.AppendLine("AND [AssetId] = @AssetId");

            if (isCaseSensitive)
            {
                sql.AppendLine("AND [Value]COLLATE Latin1_General_CS_AS = @Value");
            }
            else
            {
                sql.AppendLine("AND [Value] = @Value");
            }

            sql.AppendLine("AND [Deleted] IS NULL");
            sql.AppendLine("AND [DeletedBy] IS NULL");

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@PropertyId", propertyId.ToString()));
            paramList.Add(new SqlParameter("@AssetId", assetId));
            paramList.Add(new SqlParameter("@Value", value));

            return (base.ExecuteScalarInLine(sql.ToString(), paramList) == 1);

        }

        public void Save(T xValue)
        {
            List<SqlParameter> paramList = new List<SqlParameter>
                {
                    new SqlParameter("@Id", xValue.Id),
                    new SqlParameter("@PropertyId", xValue.PropertyId),
                    new SqlParameter("@AssetId", xValue.AssetId),
                    new SqlParameter("@SubmittalGroupId", xValue.SubmittalGroupId),
                    new SqlParameter("@Value", xValue.Value),
                    new SqlParameter("@Order", xValue.Index),
                    new SqlParameter("@Created", xValue.Created),
                    new SqlParameter("@Approved", xValue.Approved),
                    new SqlParameter("@Deleted", xValue.Deleted),
                    new SqlParameter("@CreatedBy", xValue.CreatedBy),
                    new SqlParameter("@ApprovedBy", xValue.ApprovedBy),
                    new SqlParameter("@DeletedBy", xValue.DeletedBy),
                    new SqlParameter("@Rejected", xValue.Rejected),
                    new SqlParameter("@RejectedBy", xValue.RejectedBy)
                };

            base.ExecuteSql(StoredProcs.PropertyValue_Save, paramList);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propVal"></param>
        /// <param name="deleteExisting">Whether or not existing values should be marked as deleted; if saving values
        /// for a property that is multi-select, false should be provided</param>
        /// <param name="checkExistingIsCaseSensitive"></param>
        /// <returns></returns>
        public void Save(T xValue, bool deleteExisting, bool checkExistingIsCaseSensitive)
        {
            bool previousValuesDeleted = false;

            List<SqlParameter> paramList = new List<SqlParameter>();

            if (xValue.IsDirty)
            {

                //propVal.Value = base.QuoteReplace(propVal.Value);

                // if the value already exists, don't save another one ...
                bool valueCurrentlyExists = false;

                // ... but only check for an existing if the incoming property value is new.
                // ... If it's existing, something might have changed (i.e. Approved, Rejected)
                if (xValue.IsNew)
                {
                    valueCurrentlyExists = this.PropertyValue_Exists(xValue.PropertyId, xValue.AssetId, xValue.Value, checkExistingIsCaseSensitive);
                }

                if (valueCurrentlyExists) { return; }

                if (deleteExisting)
                {
                    // delete any existing property values
                    if (this.PropertyValue_Delete(xValue.AssetId, xValue.PropertyId, xValue.CreatedBy))
                    {
                        previousValuesDeleted = true;
                    }
                }

                if ((deleteExisting == false) || ((deleteExisting == true) && (previousValuesDeleted == true)))
                {
                    paramList.Add(new SqlParameter("@Id", xValue.Id));
                    paramList.Add(new SqlParameter("@PropertyId", xValue.PropertyId));
                    paramList.Add(new SqlParameter("@AssetId", xValue.AssetId));
                    paramList.Add(new SqlParameter("@SubmittalGroupId", xValue.SubmittalGroupId));

                    if (string.IsNullOrEmpty(xValue.Value))
                    {
                        paramList.Add(new SqlParameter("@Value", null));
                    }
                    else
                    {
                        paramList.Add(new SqlParameter("@Value", xValue.Value));
                    }

                    paramList.Add(new SqlParameter("@Order", xValue.Index));
                    paramList.Add(new SqlParameter("@Created", xValue.Created));
                    paramList.Add(new SqlParameter("@Approved", xValue.Approved));
                    paramList.Add(new SqlParameter("@Deleted", xValue.Deleted));
                    paramList.Add(new SqlParameter("@CreatedBy", xValue.CreatedBy));
                    paramList.Add(new SqlParameter("@ApprovedBy", xValue.ApprovedBy));
                    paramList.Add(new SqlParameter("@DeletedBy", xValue.DeletedBy));
                    paramList.Add(new SqlParameter("@Rejected", xValue.Rejected));
                    paramList.Add(new SqlParameter("@RejectedBy", xValue.RejectedBy));

                    if (base.ExecuteSql(StoredProcs.PropertyValue_Save, paramList))
                    {
                        xValue.IsNew = false;
                        xValue.IsDirty = false;
                    }
                }

            }

        }

        public void Delete(Guid xValueId, Guid userId)
        {

            List<SqlParameter> paramList = new List<SqlParameter>
                {
                    new SqlParameter("@Id", xValueId),
                    new SqlParameter("@DeletedBy", userId)
                };

            base.ExecuteSql(StoredProcs.PropertyValue_Delete, paramList);

        }

        ///// <summary>
        ///// Gets a complete list of all values that exist and have ever existed for
        ///// a specified asset property
        ///// </summary>
        ///// <param name="propertyId">id of the property</param>
        ///// <param name="assetId">id of the asset</param>
        ///// <param name="creationDateOrder"></param>
        ///// <returns>complete historical list of property values</returns>
        //internal PropertyValueList PropertyValues_Get(Guid propertyId, Guid assetId, ESortOrder creationDateOrder)
        //{
        //    PropertyValueList propertyValues = new PropertyValueList();

        //    StringBuilder sql = new StringBuilder();
        //    sql.AppendLine("SELECT PV.[Id] FROM [PropertyValues] PV WITH (NoLock)");
        //    sql.AppendLine("WHERE PV.[PropertyId] = @Id");
        //    sql.AppendLine("AND PV.[AssetId] = @AssetId");
        //    sql.AppendLine("AND PV.[Deleted] IS NULL");
        //    sql.AppendLine("ORDER BY [Created]");

        //    if (creationDateOrder == ESortOrder.Descending) { sql.AppendLine("DESC"); }

        //    List<SqlParameter> paramList = new List<SqlParameter>
        //        {
        //            new SqlParameter("@Id", propertyId),
        //            new SqlParameter("@AssetId", assetId)
        //        };

        //    using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), paramList))
        //    {

        //        if ((rdr == null) || (!rdr.HasRows)) return propertyValues;

        //        while (rdr.Read())
        //        {
        //            if (rdr.IsDBNull(0)) continue;

        //            Guid id = rdr.GetGuid(0);
        //            propertyValues.Add(this.PropertyValue_Get_IgnoreDeletedFlag(id));
        //        }
        //    }

        //    return propertyValues;

        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyId">a valid property id</param>
        /// <param name="assetId">a valid asset id</param>
        /// <returns></returns>
        public IXValue Get(Guid xPropertyId, Guid xObjectId)
        {
            // TODO: Test this code
            XPropertyRepository<XProperty> propDal = new XPropertyRepository<XProperty>();
            XProperty prop = propDal.Get(xPropertyId);

            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT [ID] FROM [PropertyValues] WITH (NoLock)");
            sql.AppendLine("WHERE [PropertyId] = @Id");
            sql.AppendLine("AND [AssetId] = @AssetId");
            sql.AppendLine("AND [Approved] IS NOT NULL");
            sql.AppendLine("AND [Deleted] IS NULL");

            List<SqlParameter> paramList = new List<SqlParameter>
                {
                    new SqlParameter("@Id", xPropertyId),
                    new SqlParameter("@AssetId", xObjectId)
                };

            Guid propertyValueId = base.ExecuteScalarGuidInLine(sql.ToString(), paramList);

            if (xPropertyId.CompareTo(new Guid()) == 0) return null;

            return this.Get(propertyValueId);
        }

        public IXValue GetPrevious(Guid xPropertyId, Guid xObjectId)
        {
            string sql = "SELECT TOP 1 [ID] from [PropertyValues] WHERE [AssetId] = @AssetId AND [PropertyId] = @PropId AND [Deleted] IS NOT NULL ORDER BY [Created] DESC";

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@AssetId", xObjectId));
            paramList.Add(new SqlParameter("@PropId", xPropertyId));

            Guid pvId = base.ExecuteScalarGuidInLine(sql, paramList);

            return this.Get(pvId);

        }

        public void Save(IList<T> xValues, IDictionary<Guid, IXProperty> xProperties)
        {

            XPropertyRepository<XProperty> propDal = new XPropertyRepository<XProperty>();

            foreach (T propertyvalue in xValues)
            {

                if (!propertyvalue.IsDirty) continue;

                XProperty prop = null;

                if (xProperties.ContainsKey(propertyvalue.PropertyId))
                {
                    prop = (XProperty)xProperties[propertyvalue.PropertyId];
                }
                else
                {
                    prop = propDal.Get(propertyvalue.PropertyId);
                    xProperties.Add(prop.Id, prop);
                }

                if (prop != null)
                {
                    bool matchIsCaseSensitive = prop.IsSystem && (prop.SystemType == ESystemType.AssetName);

                    // if the property is multi-select, DON'T delete previous values
                    this.Save(propertyvalue, !prop.AllowMultiValue, matchIsCaseSensitive);

                    propertyvalue.IsNew = false;
                    propertyvalue.IsDirty = false;

                }
            }

        }

        //internal bool PropertyValueList_Delete(PropertyValueList propertyvalueList, Guid userId)
        //{
        //    foreach (PropertyValue propertyvalue in propertyvalueList)
        //    {
        //        this.Delete(propertyvalue.Id, userId);
        //    }
        //    return true;
        //}

        //private PropertyValueList PropertyValues_LoadFromReader(SqlDataReader drdSql)
        //{

        //    PropertyValueList list = new PropertyValueList();

        //    List<Guid> propertyIds = new List<Guid>();

        //    if ((drdSql != null) && (drdSql.HasRows))
        //    {

        //        int Id = drdSql.GetOrdinal("Id");
        //        int PropertyId = drdSql.GetOrdinal("PropertyId");
        //        int AssetId = drdSql.GetOrdinal("AssetId");
        //        int SubmittalGroupId = drdSql.GetOrdinal("SubmittalGroupId");
        //        int Value = drdSql.GetOrdinal("Value");
        //        int Order = drdSql.GetOrdinal("Order");
        //        int Created = drdSql.GetOrdinal("Created");
        //        int Approved = drdSql.GetOrdinal("Approved");
        //        int Rejected = drdSql.GetOrdinal("Rejected");
        //        int Deleted = drdSql.GetOrdinal("Deleted");
        //        int CreatedBy = drdSql.GetOrdinal("CreatedBy");
        //        int ApprovedBy = drdSql.GetOrdinal("ApprovedBy");
        //        int RejectedBy = drdSql.GetOrdinal("RejectedBy");
        //        int DeletedBy = drdSql.GetOrdinal("DeletedBy");
        //        int DisplayValue = drdSql.GetOrdinal("DisplayValue");

        //        while (drdSql.Read())
        //        {

        //            PropertyValue propVal = new PropertyValue();

        //            if (!drdSql.IsDBNull(Id)) propVal.Id = drdSql.GetGuid(Id);

        //            if (!drdSql.IsDBNull(PropertyId)) propVal.PropertyId = drdSql.GetGuid(PropertyId);

        //            if (!drdSql.IsDBNull(AssetId)) propVal.AssetId = drdSql.GetGuid(AssetId);

        //            if (!drdSql.IsDBNull(SubmittalGroupId)) propVal.SubmittalGroupId = drdSql.GetGuid(SubmittalGroupId);

        //            if (!drdSql.IsDBNull(Value)) propVal.Value = drdSql.GetString(Value);

        //            if (!drdSql.IsDBNull(Order)) propVal.Order = (int)drdSql.GetValue(Order);

        //            propVal.Created = drdSql.GetDateTime(Created);

        //            if (!drdSql.IsDBNull(Approved)) propVal.Approved = drdSql.GetDateTime(Approved);

        //            if (!drdSql.IsDBNull(Rejected)) propVal.Rejected = drdSql.GetDateTime(Rejected);

        //            if (!drdSql.IsDBNull(Deleted)) propVal.Deleted = drdSql.GetDateTime(Deleted);

        //            propVal.CreatedBy = drdSql.GetGuid(CreatedBy);

        //            if (!drdSql.IsDBNull(ApprovedBy)) propVal.ApprovedBy = drdSql.GetGuid(ApprovedBy);

        //            if (!drdSql.IsDBNull(RejectedBy)) propVal.RejectedBy = drdSql.GetGuid(RejectedBy);

        //            if (!drdSql.IsDBNull(DeletedBy)) propVal.DeletedBy = drdSql.GetGuid(DeletedBy);

        //            if (!drdSql.IsDBNull(DisplayValue)) propVal.DisplayValue = drdSql.GetString(DisplayValue);

        //            propVal.IsNew = false;
        //            propVal.IsDirty = false;

        //            if (!propertyIds.Contains(propVal.PropertyId)) { propertyIds.Add(propVal.PropertyId); }

        //            list.Add(propVal);

        //        }

        //        Dictionary<Guid, Property> props = new PropertyDal().GetDictionary(propertyIds);

        //        foreach (PropertyValue pv in list)
        //        {
        //            Property prop = props[pv.PropertyId];

        //            pv.Value = this.FormatPropertyValue(prop, pv.Value);

        //            if ((pv != null) && (!string.IsNullOrEmpty(pv.Value)))
        //            {
        //                pv.DisplayValue = this.PopulateDisplayValue(pv.Value, prop);
        //                pv.DisplayValueHtml = this.PopulateDisplayValueHtml(pv, prop);
        //                pv.IsNew = false;
        //                pv.IsDirty = false;
        //            }

        //        }

        //        list.IsNew = false;
        //        list.IsDirty = false;

        //    }

        //    return list;

        //}

        //internal PropertyValueList PropertyValueList_GetByPropertyId(Guid propertyid)
        //{
        //    using (SqlDataReader drdSql = base.OpenDataReader(spPropertyValueList_GetByPropertyId, new List<SqlParameter> { new SqlParameter("@PropertyId", propertyid) }))
        //    {
        //        return this.PropertyValues_LoadFromReader(drdSql);
        //    }
        //}

        //internal PropertyValueList PropertyValueList_Get(List<Guid> ids)
        //{

        //    if ((ids == null) || (ids.Count == 0)) { return new PropertyValueList(); }

        //    StringBuilder propIds = new StringBuilder();

        //    foreach (Guid id in ids) { propIds.AppendFormat("{0},", id); }

        //    string propertyIds = propIds.ToString();
        //    propertyIds = propertyIds.Remove(propertyIds.Length - 1);

        //    using (SqlDataReader drdSql = base.OpenDataReader(spPropertyValueList_Get, new List<SqlParameter> { new SqlParameter("@Ids", propertyIds) }))
        //    {
        //        return this.PropertyValues_LoadFromReader(drdSql);
        //    }
        //}

        //internal PropertyValueList PropertyValueList_GetByAssetId(Guid assetid)
        //{
        //    using (SqlDataReader drdSql = base.OpenDataReader(spPropertyValueList_GetByAssetId, new List<SqlParameter> { new SqlParameter("@AssetId", assetid) }))
        //    {
        //        return this.PropertyValues_LoadFromReader(drdSql);
        //    }
        //}

        //internal bool PropertyValueList_DeleteByValue(string value, Guid memberId)
        //{
        //    List<SqlParameter> paramList = new List<SqlParameter>
        //            {
        //                new SqlParameter("@Value", value),
        //                new SqlParameter("@DeletedBy", memberId)
        //            };

        //    return base.ExecuteSql(spPropertyValueList_DeleteByValue, paramList);
        //}

        //internal StringBuilder PropertyValues_GetSql_ForDatabaseViews(EAssetRequestType requestType,
        //                       string assetDisplayName,
        //                       Guid assetTypeId,
        //                       List<Guid> assetTypeIds,
        //                       List<Guid> assetIds,
        //                       List<ReportProperty> reportProperties,
        //                       bool orderResults,
        //                       string definitionLabel,
        //                       string instanceLabel)
        //{

        //    AssetTypePropertyRelationDal relationDal = new AssetTypePropertyRelationDal();

        //    PropertyDal propDal = new PropertyDal();

        //    StringBuilder sql = new StringBuilder();
        //    StringBuilder selects = new StringBuilder();
        //    StringBuilder wheres = new StringBuilder();

        //    StringBuilder sqlRaw = new StringBuilder();
        //    StringBuilder selectsRaw = new StringBuilder();

        //    sql.Append("SELECT");
        //    sqlRaw.Append("SELECT");

        //    sql.AppendFormat("{0}\t[A].[Id] AS [AssetId]", Environment.NewLine);
        //    sqlRaw.AppendFormat("{0}\t[A].[Id] AS [AssetId]", Environment.NewLine);

        //    int propCount = 0;

        //    List<Guid> propIds = new List<Guid>();
        //    foreach (ReportProperty rp in reportProperties)
        //    {
        //        propIds.Add(rp.PropertyId);
        //    }

        //    Dictionary<Guid, AssetTypePropertyRelation> propRelations = new Dictionary<Guid, AssetTypePropertyRelation>();
        //    foreach (AssetTypePropertyRelation rel in relationDal.GetCollectionByAssetTypeIdAndPropertyIds(assetTypeId, propIds))
        //    {
        //        propRelations.Add(rel.PropertyId, rel);
        //    }

        //    List<Guid> propertyIds = new List<Guid>();
        //    foreach (ReportProperty rp in reportProperties)
        //    {
        //        if (!propertyIds.Contains(rp.PropertyId)) { propertyIds.Add(rp.PropertyId); }
        //    }

        //    Dictionary<Guid, Property> props = propDal.GetDictionary(propertyIds);

        //    foreach (ReportProperty rp in reportProperties)
        //    {

        //        Property property = props[rp.PropertyId];
        //        Property subProp = rp.SubPropertyId.HasValue ? propDal.Get(rp.SubPropertyId.Value) : null;

        //        if (property == null) { continue; }

        //        string assetId = "Id"; // dynamically change to "InstanceOfId" based on if the property is at the definition level or the instance level

        //        AssetTypePropertyRelation relation = null; // relationDal.AssetTypePropertyRelation_Get(assetTypeId, rp.PropertyId);
        //        if (propRelations.ContainsKey(rp.PropertyId)) { relation = propRelations[rp.PropertyId]; }

        //        if (!property.IsSystem)
        //        {
        //            if (relation == null) { continue; }
        //            if ((requestType == EAssetRequestType.Instance) && (!relation.IsInstance))
        //            {
        //                assetId = "InstanceOfId";
        //            }
        //        }

        //        if (rp.CustomReportFieldType == ECustomReportFieldType.NotApplicable)
        //        {
        //            #region standard fields

        //            string propDisplay = string.IsNullOrEmpty(property.DisplayValue) ? property.Name : property.DisplayValue;
        //            string propId = rp.PropertyId.ToString(); // just to enhance readability later in this method

        //            // system properties do not need a relationi.e. 
        //            if (!property.IsSystem)
        //            {
        //                if (relation.IsInheritedValue)
        //                {
        //                    selects.AppendFormat("{0}\t,dbo.GetPropertyValueLookup([A].[InstanceOfId], '{1}') AS [{2}]", Environment.NewLine, propId, propDisplay.Trim());
        //                    selectsRaw.AppendFormat("{0}\t,dbo.PropertyValue([A].[InstanceOfId], '{1}') AS [{2}]", Environment.NewLine, propId, propId);
        //                    continue;
        //                }
        //                else
        //                {
        //                    if ((requestType == EAssetRequestType.Instance) && (!relation.IsInstance))
        //                    {
        //                        assetId = "InstanceOfId";
        //                    }
        //                }
        //            }

        //            switch (property.DataType)
        //            {

        //                case EDataType.Asset:

        //                    if (property.IsSystem)
        //                    {
        //                        if (subProp == null)
        //                        {
        //                            switch (property.SystemType)
        //                            {
        //                                case ESystemType.InstanceOf:
        //                                    selects.AppendFormat("\r\n\t,dbo.GetAssetParentNameByInstanceId([A].[Id]) AS [{0}]", definitionLabel);
        //                                    selectsRaw.AppendFormat("\r\n\t,[A].[InstanceOfId] AS [{0}]", Constants.PropertyIds.InstanceOf);
        //                                    break;
        //                                default:
        //                                    selects.AppendFormat("\r\n\t,dbo.GetPropertyValueLookup([A].[{0}], '{1}') AS [{2}]", assetId, propId, propDisplay);
        //                                    selectsRaw.AppendFormat("\r\n\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propId);
        //                                    break;
        //                            }
        //                        }
        //                        else
        //                        {
        //                            propDisplay += ".";
        //                            string temp = string.IsNullOrEmpty(subProp.DisplayValue) ? subProp.Name : subProp.DisplayValue;
        //                            propDisplay += temp;
        //                            selects.AppendFormat("\r\r\n\t,dbo.GetPropertyValueLookup(dbo.GetSystemValueLookup([A].[{0}], '{1}'), '{2}') AS [{3}]", assetId, propId, subProp.Id, propDisplay);
        //                            selectsRaw.AppendFormat("\r\r\n\t,dbo.PropertyValue(dbo.GetSystemValueLookup([A].[{0}], '{1}'), '{2}') AS [{3}]", assetId, propId, subProp.Id, propDisplay);
        //                        }
        //                    }
        //                    else
        //                    {
        //                        if (subProp == null)
        //                        {
        //                            if (property.AllowMultiValue)
        //                            {
        //                                selects.AppendFormat("{0}\t,dbo.PropertyValueMulti_Asset([A].[{1}], '{2}') AS [{3}]", Environment.NewLine, assetId, propId, propDisplay);
        //                            }
        //                            else
        //                            {
        //                                selects.AppendFormat("{0}\t,dbo.PropertyValue_Asset([A].[{1}], '{2}') AS [{3}]", Environment.NewLine, assetId, propId, propDisplay);
        //                            }
        //                        }
        //                        else
        //                        {
        //                            propDisplay += ".";
        //                            string temp = string.IsNullOrEmpty(subProp.DisplayValue) ? subProp.Name : subProp.DisplayValue;
        //                            propDisplay += temp;
        //                            if (subProp.IsSystem)
        //                            {
        //                                selects.AppendFormat("\r\n\t,dbo.GetSystemValueLookup(dbo.GetPropertyValue([A].[{0}], '{1}'), '{2}') AS [{3}]", assetId, propId, subProp.Id, propDisplay);
        //                            }
        //                            else
        //                            {
        //                                selects.AppendFormat("\r\n\t,dbo.GetPropertyValueLookup(dbo.GetPropertyValue([A].[{0}], '{1}'), '{2}') AS [{3}]", assetId, propId, subProp.Id, propDisplay);
        //                            }
        //                        }
        //                    }

        //                    break;

        //                case EDataType.Currency:

        //                    selects.AppendFormat("{0}\t,dbo.PropertyValue_Currency([A].[{1}], '{2}') AS [{3}]", Environment.NewLine, assetId, propId, propDisplay);
        //                    selectsRaw.AppendFormat("{0}\t,dbo.PropertyValue([A].[{1}], '{2}') AS [{3}]", Environment.NewLine, assetId, propId, propId);

        //                    break;

        //                case EDataType.Date:

        //                    switch (property.SystemType)
        //                    {
        //                        case ESystemType.NotApplicable:
        //                            selects.AppendFormat("\r\n\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propDisplay);
        //                            selectsRaw.AppendFormat("\r\n\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propId);
        //                            break;
        //                        case ESystemType.Created:
        //                        case ESystemType.Deleted:
        //                        case ESystemType.LastModified:
        //                            selects.AppendFormat("\r\n\t,dbo.GetSystemValueLookup([A].[Id], '{0}') AS [{1}]", propId, propDisplay);
        //                            break;
        //                    }

        //                    break;

        //                case EDataType.DateTime:

        //                    selects.AppendFormat("\r\n\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propDisplay);
        //                    selectsRaw.AppendFormat("\r\n\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propId);

        //                    break;

        //                case EDataType.Dependency:

        //                    string columnName = string.IsNullOrEmpty(property.DisplayValue) ? property.Name : property.DisplayValue;

        //                    if (property.AllowMultiValue)
        //                    {
        //                        selects.AppendFormat("{0}\t,dbo.PropertyValueMulti_Asset([A].[{1}], '{2}') AS [{3}]", Environment.NewLine, assetId, propId, propDisplay);
        //                    }
        //                    else
        //                    {
        //                        selects.AppendFormat("\r\n\t,dbo.GetAssetValueById([pv{0}].[Value]) AS [{1}]", propCount, columnName);
        //                    }

        //                    break;

        //                case EDataType.Document:

        //                    // TODO: Implement SQL generation for this - provide a link to the doc?
        //                    selects.AppendFormat("\r\n\t,dbo.GetPropertyValueLookup([A].[{0}], '{1}') AS [{2}]", assetId, propId, propDisplay);

        //                    break;

        //                case EDataType.Float:

        //                    selects.AppendFormat("\r\n\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propDisplay);

        //                    break;

        //                case EDataType.Image:

        //                    //string url = "'" + SystemFrameworkHelper.ImageServiceUrl + "/getImageStream?imageId='";
        //                    string url = string.Format("'{0}'", SystemFrameworkHelper.ViewImageUrl);

        //                    selects.AppendFormat("\r\n\t,{0} + dbo.GetPropertyValueLookup([A].[Id], '{1}') AS [{2}]", url, property.Id.ToString(), propDisplay);

        //                    break;

        //                case EDataType.PickList:

        //                    selects.AppendFormat("{0}\t,dbo.PropertyValue_PickList([A].[{1}], '{2}') AS [{3}]", Environment.NewLine, assetId, propId, propDisplay);
        //                    selectsRaw.AppendFormat("{0}\t,dbo.PropertyValue([A].[{1}], '{2}') AS [{3}]", Environment.NewLine, assetId, propId, propId);

        //                    break;

        //                case EDataType.Int:
        //                case EDataType.IPv4:
        //                case EDataType.IPv6:
        //                case EDataType.Memo:
        //                case EDataType.Relation_Other:

        //                    selects.AppendFormat("\r\n\t,dbo.GetPropertyValueLookup([A].[{0}], '{1}') AS [{2}]", assetId, propId, propDisplay);
        //                    selectsRaw.AppendFormat("\r\n\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propId);

        //                    break;

        //                case EDataType.Relation_ChildParent:

        //                    selects.AppendFormat("\r\n\t,dbo.GetAssetParentNameByChildId([A].[{0}]) AS [{1}]", assetId, propDisplay);

        //                    break;

        //                case EDataType.Relation_ParentChild:

        //                    selects.AppendFormat("\r\n\t,dbo.GetAssetParentNameByChildId([A].[{0}]) AS [{1}]", assetId, propDisplay);

        //                    break;

        //                case EDataType.Geo:
        //                case EDataType.String:

        //                    #region EDataType.String

        //                    if (property.IsSystem)
        //                    {
        //                        switch (property.SystemType)
        //                        {

        //                            case ESystemType.AssetName:

        //                                if (string.IsNullOrEmpty(assetDisplayName))
        //                                {
        //                                    sql.AppendFormat("{0}\t,IsNull([A].[DisplayValue], [A].[Name]) AS [Asset Name]", Environment.NewLine);
        //                                }
        //                                else
        //                                {
        //                                    sql.AppendFormat("{0}\t,IsNull([A].[DisplayValue], [A].[Name]) AS [{1}]", Environment.NewLine, assetDisplayName);
        //                                }

        //                                break;

        //                            case ESystemType.AssetType:

        //                                selects.Append("\r\n\t,dbo.GetAssetType(A.[Id]) AS [Asset Type]");

        //                                break;

        //                            case ESystemType.Description:

        //                                selects.Append("\r\n\t,A.[Description] AS [Description]");

        //                                break;

        //                            case ESystemType.InstanceOfDescription:

        //                                selects.AppendFormat("\r\n\t,dbo.GetAssetParentDescByInstanceId([A].[Id]) AS [{0} Description]", definitionLabel);

        //                                break;

        //                            default:

        //                                selects.AppendFormat("\r\n\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propDisplay);

        //                                break;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        selects.AppendFormat("\r\n\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propDisplay);
        //                    }

        //                    #endregion

        //                    break;

        //                case EDataType.URL:

        //                    selects.AppendFormat("\r\n\t,dbo.GetPropertyValueLookup([A].[Id], '{0}') AS [{1}]", propId, propDisplay);

        //                    break;

        //                case EDataType.User:

        //                    if (property.IsSystem)
        //                    {
        //                        switch (property.SystemType)
        //                        {
        //                            case ESystemType.CreatedBy:
        //                            case ESystemType.LastModifiedBy:
        //                            case ESystemType.DeletedBy:
        //                                if (subProp == null)
        //                                {
        //                                    selects.AppendLine(string.Format("\t,dbo.GetMemberValueById(dbo.GetSystemValueLookup([A].[Id], '{0}')) AS [{1}]", propId, propDisplay));
        //                                }
        //                                else
        //                                {
        //                                    propDisplay += ".";
        //                                    string temp = string.IsNullOrEmpty(subProp.DisplayValue) ? subProp.Name : subProp.DisplayValue;
        //                                    propDisplay += temp;
        //                                    selects.AppendLine(string.Format("\t,dbo.GetPropertyValueLookup(dbo.GetSystemValueLookup([A].[{0}], '{1}'), '{2}') AS [{3}]", assetId, propId, subProp.Id, propDisplay));
        //                                }
        //                                break;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        if (subProp == null)
        //                        {
        //                            selects.AppendFormat("{0}\t,dbo.PropertyValue_Member([A].[Id], '{1}') AS [{2}]", Environment.NewLine, propId, propDisplay);
        //                            selectsRaw.AppendFormat("{0}\t,dbo.PropertyValue([A].[Id], '{1}') AS [{2}]", Environment.NewLine, propId, propId);
        //                        }
        //                        else
        //                        {
        //                            propDisplay += ".";
        //                            string temp = string.IsNullOrEmpty(subProp.DisplayValue) ? subProp.Name : subProp.DisplayValue;
        //                            propDisplay += temp;
        //                            selects.AppendLine(string.Format("\t,dbo.GetPropertyValueLookup(dbo.GetPropertyValue([A].[{0}], '{1}'), '{2}') AS [{3}]", assetId, propId, subProp.Id, propDisplay));
        //                        }
        //                    }
        //                    break;

        //                default:
        //                    break;
        //            }

        //            #endregion
        //        }
        //        else
        //        {
        //            #region custom fields

        //            switch (rp.CustomReportFieldType)
        //            {
        //                case ECustomReportFieldType.Calculated:
        //                    List<string> temp = new List<string>();
        //                    foreach (Guid propertyId in rp.PropertyIds)
        //                    {
        //                        temp.Add(string.Format("nullif(CAST (dbo.udf_GetNumeric(dbo.GetPropertyValueLookup([A].[{0}], '{1}')) AS float),0)", assetId, propertyId));
        //                    }
        //                    string final = string.Format(rp.Logic, temp.ToArray());
        //                    selects.AppendLine("\t,CAST ((" + final + ") AS decimal(18,2)) AS [" + rp.Label + "]");
        //                    break;
        //                case ECustomReportFieldType.Concatentation:
        //                    break;
        //                case ECustomReportFieldType.RangeLabel:
        //                    break;
        //                case ECustomReportFieldType.TimeElapsed:
        //                    if (property.IsSystem)
        //                    {
        //                        if (rp.TimeElapsedFormat == ETimeElapsedFormat.Absolute)
        //                        {
        //                            selects.AppendLine(string.Format("\t,dbo.F_AGE_YYYY_MM_DD(dbo.GetSystemValueLookup([A].[{0}], '{1}'), GetDate()) AS [{2}]", assetId, rp.PropertyId, rp.Label));
        //                        }
        //                        else
        //                        {
        //                            selects.AppendLine(string.Format("\t,cast(DATEDIFF ( D, dbo.GetSystemValueLookup([A].[{0}], '{1}'), GetDate() ) / cast(365 as float) as decimal(18, 2)) AS [{2}]", assetId, rp.PropertyId, rp.Label));
        //                        }
        //                    }
        //                    else
        //                    {
        //                        if (rp.TimeElapsedFormat == ETimeElapsedFormat.Absolute)
        //                        {
        //                            selects.AppendLine(string.Format("\t,dbo.F_AGE_YYYY_MM_DD(dbo.GetPropertyValueLookup([A].[{0}], '{1}'), GetDate()) AS [{2}]", assetId, rp.PropertyId, rp.Label));
        //                        }
        //                        else
        //                        {
        //                            selects.AppendLine(string.Format("\t,cast(DATEDIFF ( D, dbo.GetPropertyValueLookup([A].[{0}], '{1}'), GetDate() ) / cast(365 as float) as decimal(18, 2)) AS [{2}]", assetId, rp.PropertyId, rp.Label));
        //                        }
        //                    }
        //                    break;
        //                case ECustomReportFieldType.ValueLabel:
        //                    break;
        //            }

        //            #endregion
        //        }

        //        propCount++;

        //    }

        //    int currentAssetCount = 0;
        //    int lastAssetIdIndex = assetIds.Count - 1;

        //    if ((assetIds != null) && (assetIds.Count > 0))
        //    {
        //        foreach (Guid assetId in assetIds)
        //        {
        //            if (currentAssetCount == 0) { wheres.Append(" ([A].[Id] IN ("); }

        //            if (currentAssetCount == lastAssetIdIndex)
        //            {
        //                wheres.AppendFormat(" '{0}'))", assetId);
        //            }
        //            else
        //            {
        //                wheres.AppendFormat(" '{0}',", assetId);
        //            }

        //            currentAssetCount++;
        //        }
        //    }
        //    else
        //    {

        //        int atCount = assetTypeIds.Count - 1;
        //        int atIndex = 0;

        //        foreach (Guid atId in assetTypeIds)
        //        {
        //            if (atIndex == 0) { wheres.Append(" ([A].[AssetTypeId] IN ("); }

        //            if (atIndex == atCount)
        //            {
        //                wheres.AppendFormat(" '{0}'))", atId);
        //            }
        //            else
        //            {
        //                wheres.AppendFormat(" '{0}',", atId);
        //            }

        //            atIndex++;
        //        }

        //        if (requestType == EAssetRequestType.Definition)
        //        {
        //            wheres.AppendLine(" AND ([A].[IsInstance] = 0)");
        //        }
        //        else
        //        {
        //            wheres.AppendLine(" AND ([A].[IsInstance] = 1)");
        //        }

        //        wheres.AppendLine("AND (A.[Deleted] IS NULL)");

        //    }

        //    //wheres.AppendLine("AND A.[Deleted] IS NULL AND A.[DeletedBy] IS NULL");

        //    sql.AppendLine(selects.ToString());
        //    sql.AppendLine("FROM [Assets] A WITH (NoLock)");
        //    sql.AppendLine("WHERE");
        //    sql.Append(wheres.ToString());

        //    sqlRaw.AppendLine(selectsRaw.ToString());
        //    sqlRaw.AppendLine("FROM [Assets] A WITH (NoLock)");
        //    sqlRaw.AppendLine("WHERE");
        //    sqlRaw.Append(wheres.ToString());

        //    if (orderResults) { sql.Append("ORDER BY [Name]"); }

        //    //string fileName = "C:\\Projects\\TMCP.XDB\\TMCP.XDB.REST\\bin\\Report_IN.sql";

        //    //using (System.IO.StreamWriter sw = new System.IO.StreamWriter(fileName, false))
        //    //{
        //    //    sw.Write(sql.ToString());
        //    //}

        //    return sql;
        //}

        //internal DataSet PropertyValues_GetByReportId(Guid reportId, string assetDisplayName, int page, int recordsPerPage, string sortColumn, string sortDir)
        //{
        //    int startRow = ((page - 1) * recordsPerPage) + 1;
        //    int endRow = startRow + recordsPerPage - 1;

        //    StringBuilder sql = new StringBuilder();

        //    if (string.IsNullOrEmpty(sortColumn))
        //    {
        //        if (string.IsNullOrEmpty(assetDisplayName))
        //        {
        //            sortColumn = "Asset";
        //        }
        //        else
        //        {
        //            sortColumn = assetDisplayName;
        //        }
        //    }

        //    sql.AppendLine("select * from ");
        //    sql.AppendLine("( ");
        //    sql.AppendLine("select  row_number() over (ORDER BY [" + sortColumn + "] " + sortDir + ")  rn, * from ");
        //    sql.AppendLine("( ");

        //    sql.AppendFormat("SELECT * FROM [dbo].[vw{0}]", reportId.ToString().Replace("-", ""));

        //    sql.AppendLine(") as someTable) as anotherTable where rn between " + startRow + " and " + endRow); //math for the page

        //    DataSet dst = base.GetDataset(sql.ToString());
        //    dst.Tables[0].TableName = "pageOfData";
        //    dst.Tables[0].PrimaryKey = new DataColumn[] { dst.Tables[0].Columns[0] };

        //    ////o.k., let's try some stuff!
        //    //StringBuilder wheres = new StringBuilder();
        //    //int currentAssetCount = 0;
        //    //foreach (Guid assetId in assetIds)
        //    //{
        //    //    if (currentAssetCount == 0) { wheres.AppendLine(" \t\t([A].[Id] IN ("); }
        //    //    if (currentAssetCount == assetIds.Count - 1)
        //    //    {
        //    //        wheres.AppendLine(string.Format("\t\t\t\t'{0}'))", assetId.ToString()));
        //    //    }
        //    //    else
        //    //    {
        //    //        wheres.AppendLine(string.Format("\t\t\t\t'{0}', ", assetId.ToString()));
        //    //    }

        //    //    currentAssetCount++;
        //    //}

        //    StringBuilder sql2 = new StringBuilder();
        //    sql2.AppendFormat("SELECT COUNT(*) FROM [dbo].[vw{0}]", reportId.ToString().Replace("-", ""));
        //    //sql2.AppendLine("FROM [Assets] A\n");
        //    //if (includeAssetTypeNames) { sql2.AppendLine("INNER JOIN [AssetTypes] AT ON AT.[Id] = A.[AssetTypeId]\n"); }
        //    //sql2.Append("WHERE\n");
        //    //sql2.Append(wheres.ToString());

        //    DataSet dst2 = base.GetDataset(sql2.ToString());

        //    dst.Tables.Add(dst2.Tables[0].Clone());
        //    dst.Tables[1].ImportRow(dst2.Tables[0].Rows[0]);

        //    return dst;
        //}

        //internal DataSet PropertyValues_GetFromDatabaseViews(EAssetRequestType requestType, string assetDisplayName,
        //            Guid assetTypeId,
        //            List<Guid> assetTypeIds,
        //            List<Guid> assetIds,
        //            List<ReportProperty> reportProperties,
        //            int page,
        //            int recordsPerPage,
        //            string sortColumn,
        //            string sortDir,
        //            string definitionLabel,
        //            string instanceLabel, out string sqlText)
        //{

        //    int startRow = ((page - 1) * recordsPerPage) + 1;
        //    int endRow = startRow + recordsPerPage - 1;

        //    if (reportProperties.Count > 127)
        //    {
        //        throw new LogicalException("Cannot request values for more than 127 properties at a time due to database limitations.");
        //    }

        //    StringBuilder sql = new StringBuilder();

        //    if (string.IsNullOrEmpty(sortColumn))
        //    {
        //        if (string.IsNullOrEmpty(assetDisplayName))
        //        {
        //            sortColumn = "Asset Name";
        //        }
        //        else
        //        {
        //            sortColumn = assetDisplayName;
        //        }
        //    }

        //    sql.AppendLine("SELECT * FROM ( ");
        //    sql.AppendLine("SELECT row_number() over (ORDER BY [" + sortColumn + "] " + sortDir + ")  rn, * FROM ( ");

        //    StringBuilder innerSql = this.PropertyValues_GetSql_ForDatabaseViews(requestType, assetDisplayName, assetTypeId, assetTypeIds, assetIds, reportProperties, false, definitionLabel, instanceLabel);

        //    sqlText = innerSql.ToString(); // set the out param for the caller

        //    sql.AppendLine(innerSql.ToString());

        //    sql.AppendLine(") AS someTable) AS anotherTable WHERE rn between " + startRow + " AND " + endRow); //math for the page

        //    DataSet dst = base.GetDataset(sql.ToString());

        //    try
        //    {
        //        dst.Tables[0].TableName = "pageOfData";
        //        dst.Tables[0].PrimaryKey = new DataColumn[] { dst.Tables[0].Columns[0] };
        //    }
        //    catch (ArgumentException)
        //    {
        //        // TODO: Determine when this is actually ok
        //        // primary keys are duplicated for some cases
        //    }

        //    int currentAssetCount = 0;
        //    int assetIdCount = assetIds.Count - 1;

        //    StringBuilder wheres = new StringBuilder();

        //    if ((assetIds != null) && (assetIds.Count > 0))
        //    {
        //        foreach (Guid assetId in assetIds)
        //        {
        //            if (currentAssetCount == 0) { wheres.Append("([A].[Id] IN ("); }

        //            if (currentAssetCount == assetIdCount)
        //            {
        //                wheres.Append(string.Format("'{0}'))", assetId));
        //            }
        //            else
        //            {
        //                wheres.Append(string.Format("'{0}', ", assetId));
        //            }

        //            currentAssetCount++;
        //        }
        //    }
        //    else
        //    {
        //        int atCount = assetTypeIds.Count - 1;
        //        int atIndex = 0;
        //        foreach (Guid atId in assetTypeIds)
        //        {
        //            if (atIndex == 0) { wheres.Append("([A].[AssetTypeId] IN ("); }

        //            if (atIndex == atCount)
        //            {
        //                wheres.Append(string.Format("'{0}'))", atId));
        //            }
        //            else
        //            {
        //                wheres.Append(string.Format("'{0}', ", atId));
        //            }

        //            atIndex++;
        //        }
        //        if (requestType == EAssetRequestType.Definition)
        //        {
        //            wheres.AppendLine(" AND ([A].[IsInstance] = 0)");
        //        }
        //        else
        //        {
        //            wheres.AppendLine(" AND ([A].[IsInstance] = 1)");
        //        }

        //        wheres.AppendLine("AND (A.[Deleted] IS NULL)");
        //    }

        //    StringBuilder sql2 = new StringBuilder("select count(*) ");
        //    sql2.AppendLine("FROM [Assets] A WITH (NoLock)\n");
        //    sql2.AppendLine("INNER JOIN [AssetTypes] AT WITH (NoLock) ON AT.[Id] = A.[AssetTypeId]\n");
        //    sql2.Append("WHERE\n");
        //    sql2.Append(wheres.ToString());

        //    DataSet dst2 = base.GetDataset(sql2.ToString());

        //    dst.Tables.Add(dst2.Tables[0].Clone());
        //    dst.Tables[1].ImportRow(dst2.Tables[0].Rows[0]);

        //    return dst;


        //}

        //private string GetInheritedPropertyValue(Guid assetId, Guid propertyId)
        //{
        //    // need the InstanceOf value
        //    Asset a = new AssetDal().Get(assetId);
        //    if (a == null) { return string.Empty; }
        //    if (a.InstanceOfId != null)
        //    {
        //        PropertyValue pv = this.Get(propertyId, a.InstanceOfId.Value);
        //        return this.PopulateDisplayValue(pv.Value, new PropertyDal().Get(propertyId));
        //    }
        //    return string.Empty;
        //}

        //internal PropertyValueList PropertyValues_GetForAnAsset(Guid assetId, List<Guid> propertyIds, bool includeUnapproved, bool includeDeleted)
        //{

        //    if ((propertyIds == null) || (propertyIds.Count == 0)) { return new PropertyValueList(); }

        //    List<SqlParameter> paramList = new List<SqlParameter>();
        //    paramList.Add(new SqlParameter("@AssetId", assetId));
        //    paramList.Add(new SqlParameter("@PropertyIds", Helpers.ListOfGuidToCommaDelimString(propertyIds)));

        //    string spName = string.Empty;

        //    if (!includeUnapproved && !includeDeleted)
        //    {
        //        spName = "spr_PropertyValueList_GetForAnAsset";
        //    }
        //    else if (includeDeleted)
        //    {
        //        spName = "spr_PropertyValueList_GetForAnAssetIncludeDeleted";
        //    }
        //    else if (includeUnapproved)
        //    {
        //        return null;
        //    }
        //    else
        //    {
        //        return null;
        //    }

        //    using (SqlDataReader rdr = base.OpenDataReader(spName, paramList))
        //    {
        //        return this.PropertyValues_LoadFromReader(rdr);
        //    }

        //}

        //internal PropertyValueList PropertyValues_GetForASubmittalGroup(Guid submittalGroupId)
        //{
        //    using (SqlDataReader drdSql = base.OpenDataReader(spPropertyValueList_GetBySubmittalGroupId, new List<SqlParameter> { new SqlParameter("@SubmittalGroupId", submittalGroupId) }))
        //    {
        //        return this.PropertyValues_LoadFromReader(drdSql);
        //    }
        //}

        //internal Dictionary<Guid, string> PropertyValues_GetMatching(Guid assetTypeId, Guid propertyId, string propertyValue)
        //{

        //    Dictionary<Guid, string> values = new Dictionary<Guid, string>();

        //    StringBuilder sql = new StringBuilder();
        //    sql.AppendLine("SELECT DISTINCT [A].[Id], [A].[Name]");
        //    sql.AppendLine("FROM [Assets] [A] WITH (NoLock)");
        //    sql.AppendLine("INNER JOIN [PropertyValues] [PV] WITH (NoLock) ON [PV].[AssetId] = [A].[Id]");
        //    sql.AppendLine("AND [A].[AssetTypeId] = @AssetTypeId");
        //    sql.AppendLine("AND [PV].[Value] = @Value");
        //    sql.AppendLine("AND [PV].[PropertyId] = @PropertyId");
        //    //sql.AppendLine("AND [PV].[Approved] IS NOT NULL");
        //    //sql.AppendLine("AND [PV].[ApprovedBy] IS NOT NULL");
        //    //sql.AppendLine("AND [PV].[Deleted] IS NULL");
        //    //sql.AppendLine("AND [PV].[DeletedBy] IS NULL");
        //    //sql.AppendLine("AND [A].[Approved] IS NOT NULL");
        //    //sql.AppendLine("AND [A].[ApprovedBy] IS NOT NULL");
        //    sql.AppendLine("AND [A].[Deleted] IS NULL");
        //    sql.AppendLine("ORDER BY [A].[Name]");

        //    List<SqlParameter> paramList = new List<SqlParameter>();
        //    paramList.Add(new SqlParameter("@AssetTypeId", assetTypeId));
        //    paramList.Add(new SqlParameter("@Value", propertyValue));
        //    paramList.Add(new SqlParameter("@PropertyId", propertyId));

        //    using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), paramList))
        //    {
        //        if ((rdr == null) || (!rdr.HasRows)) return values;
        //        while (rdr.Read())
        //        {
        //            if ((!rdr.IsDBNull(0)) && (!rdr.IsDBNull(1)))
        //            {
        //                values.Add(rdr.GetGuid(0), rdr.GetString(1));
        //            }
        //        }
        //    }

        //    return values;

        //}

        public string FormatPropertyValue(IXProperty prop, string value)
        {

            if (prop.DataType == EDataType.Currency)
            {

                if (string.IsNullOrEmpty(value)) { return value; }

                // is it just the currency symbol?
                if (value.Length == 1)
                {
                    int outVal = 0;
                    bool parsed = int.TryParse(value, out outVal);
                    if (!parsed) { return string.Empty; }
                }

                string firstPos = value.Substring(0, 1);
                int result;
                bool isDigit = int.TryParse(firstPos, out result);

                if (isDigit)
                {
                    // default to USD
                    double curr;
                    bool parsed = double.TryParse(value, out curr);
                    if (parsed)
                    {
                        return String.Format("{0:C}", curr);
                    }
                    else
                    {
                        return value;
                    }
                }
                else
                {
                    if (firstPos == "$")
                    {
                        double curr;
                        bool parsed = double.TryParse(value.Substring(1, value.Length - 1), out curr);
                        if (parsed)
                        {
                            return String.Format("{0:C}", curr);
                        }
                        else
                        {
                            return value;
                        }
                    }
                    else
                    {
                        return value;
                    }
                }

                //if (string.IsNullOrEmpty(value))
                //{
                //    return "$0.00";
                //}
                //else
                //{
                //    double newValue;
                //    bool parsed = double.TryParse(value, out newValue);

                //    if (parsed)
                //    {
                //        return string.Format("{0:C}", newValue);
                //    }
                //    else
                //    {
                //        return value;
                //    }

                //}

            }

            return value;

        }

        //internal List<string> PropertyValues_GetPotentialByAssetTypeIds(List<Guid> assetTypeIds, Guid propertyId)
        //{
        //    List<string> values = new List<string>();

        //    List<SqlParameter> paramList = new List<SqlParameter>();
        //    paramList.Add(new SqlParameter("@AssetTypeIds", Helpers.ListOfGuidToCommaDelimString(assetTypeIds)));
        //    paramList.Add(new SqlParameter("@PropertyId", propertyId));
        //    //paramList.Add(new SqlParameter("@IsInstance", isInstance));

        //    using (SqlDataReader rdr = base.OpenDataReader("spr_PropertyValues_GetPotentialByAssetTypeIds", paramList))
        //    {
        //        if ((rdr == null) || (!rdr.HasRows)) return values;
        //        while (rdr.Read())
        //        {
        //            values.Add(rdr.GetString(0));
        //        }
        //    }

        //    return values;
        //}

        //internal List<string> PropertyValues_GetPotential(List<Guid> assetIds, Guid propertyId)
        //{
        //    StringBuilder sql = new StringBuilder();
        //    sql.AppendLine("SELECT DISTINCT PV.[Value]");
        //    sql.AppendLine("FROM [PropertyValues] PV WITH (NoLock)");
        //    sql.AppendLine("WHERE PV.[AssetId] IN (");

        //    int assetIdCount = assetIds.Count;
        //    for (int i = 0; i < assetIdCount; i++)
        //    {
        //        if (i == (assetIdCount - 1))
        //        {
        //            sql.AppendLine("'" + assetIds[i].ToString() + "')");
        //        }
        //        else
        //        {
        //            sql.AppendLine("'" + assetIds[i].ToString() + "',");
        //        }
        //    }

        //    sql.AppendLine("AND (PV.[PropertyId] = @PropertyId)");
        //    sql.AppendLine("AND (PV.[Approved] IS NOT NULL)");
        //    sql.AppendLine("AND (PV.[Deleted] IS NULL)");
        //    sql.AppendLine("AND (PV.[Value] IS NOT NULL)");
        //    sql.AppendLine("AND (PV.[Value] != '')");
        //    sql.AppendLine("AND (PV.[Value] != ' ')");

        //    List<string> values = new List<string>();

        //    using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), new List<SqlParameter> { new SqlParameter("@PropertyId", propertyId) }))
        //    {
        //        if ((rdr == null) || (!rdr.HasRows)) return values;
        //        while (rdr.Read())
        //        {
        //            values.Add(rdr.GetString(0));
        //        }
        //    }

        //    return values;
        //}

        //internal Dictionary<Guid, string> GetDictionaryByAssetTypeIds(List<Guid> assetTypeIds, ESystemType systemType)
        //{
        //    Dictionary<Guid, string> values = new Dictionary<Guid, string>();

        //    StringBuilder sql = new StringBuilder();
        //    StringBuilder wheres = new StringBuilder();

        //    switch (systemType)
        //    {
        //        case ESystemType.AssetType:
        //            sql.AppendLine("SELECT DISTINCT A.[AssetTypeId], dbo.GetAssetType(A.[Id]) AS [Value]");
        //            wheres.AppendLine("AND A.[InstanceOfId] IS NOT NULL");
        //            break;
        //        case ESystemType.CreatedBy:
        //            sql.AppendLine("SELECT DISTINCT A.[CreatedBy], dbo.GetMemberValueById(A.[CreatedBy]) AS [Value]");
        //            break;
        //        case ESystemType.DeletedBy:
        //            sql.AppendLine("SELECT DISTINCT A.[DeletedBy], dbo.GetMemberValueById(A.[DeletedBy]) AS [Value]");
        //            break;
        //        case ESystemType.InstanceOf:
        //            sql.AppendLine("SELECT DISTINCT A.[InstanceOfId], dbo.GetAssetParentNameByInstanceId(A.[Id]) AS [Value]");
        //            wheres.AppendLine("AND A.[InstanceOfId] IS NOT NULL");
        //            break;
        //        case ESystemType.LastModifiedBy:
        //            sql.AppendLine("SELECT DISTINCT A.[LastModifiedBy], dbo.GetMemberValueById(A.[LastModifiedBy]) AS [Value]");
        //            break;
        //    }

        //    sql.AppendLine("FROM [Assets] A WITH (NoLock)");
        //    sql.AppendLine("WHERE A.[AssetTypeId] IN (SELECT [Value] FROM dbo.Split(@AssetTypeIds, ','))");

        //    if ((systemType != ESystemType.DeletedBy) && (systemType != ESystemType.Deleted))
        //    {
        //        sql.AppendLine("AND A.[Deleted] IS NULL");
        //    }

        //    sql.AppendLine(wheres.ToString());

        //    sql.AppendLine("ORDER BY [Value]");

        //    using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), new List<SqlParameter> { new SqlParameter("@AssetTypeIds", Helpers.ListOfGuidToCommaDelimString(assetTypeIds)) }))
        //    {
        //        if ((rdr == null) || (!rdr.HasRows)) return values;
        //        while (rdr.Read())
        //        {
        //            if ((!rdr.IsDBNull(0)) && (!rdr.IsDBNull(1)))
        //            {
        //                values.Add(rdr.GetGuid(0), rdr.GetString(1));
        //            }
        //        }
        //    }

        //    return values;
        //}

        public IList<string> PropertyValues_GetPotentialByAssetTypeIds(List<Guid> xObjectTypeIds, ESystemType systemType)
        {

            List<string> values = new List<string>();

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@AssetTypeIds", Helpers.ListOfGuidToCommaDelimString(xObjectTypeIds)));

            StringBuilder sql = new StringBuilder();

            switch (systemType)
            {
                case ESystemType.AssetType:
                    sql.AppendLine("SELECT DISTINCT A.[AssetTypeId]");
                    break;
                case ESystemType.CreatedBy:
                    sql.AppendLine("SELECT DISTINCT A.[CreatedBy]");
                    break;
                case ESystemType.DeletedBy:
                    sql.AppendLine("SELECT DISTINCT A.[DeletedBy]");
                    break;
                case ESystemType.InstanceOf:
                    sql.AppendLine("SELECT DISTINCT A.[InstanceOfId]");
                    break;
                case ESystemType.LastModifiedBy:
                    sql.AppendLine("SELECT DISTINCT A.[LastModifiedBy]");
                    break;
            }

            sql.AppendLine("FROM [Assets] A WITH (NoLock)");
            sql.AppendLine("WHERE A.[AssetTypeId] IN (SELECT [Value] FROM dbo.Split(@AssetTypeIds, ','))");

            if ((systemType != ESystemType.DeletedBy) && (systemType != ESystemType.Deleted))
            {
                sql.AppendLine("AND A.[Deleted] IS NULL");
            }

            using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), paramList))
            {
                if ((rdr == null) || (!rdr.HasRows)) { return values; }
                while (rdr.Read())
                {
                    if (!rdr.IsDBNull(0)) { values.Add(rdr.GetGuid(0).ToString()); }
                }
            }

            return values;
        }

        //internal List<string> PropertyValues_GetPotential(List<Guid> assetIds, ESystemType systemType)
        //{
        //    StringBuilder sql = new StringBuilder();

        //    switch (systemType)
        //    {
        //        case ESystemType.AssetType:
        //            sql.AppendLine("SELECT DISTINCT A.[AssetTypeId]");
        //            break;
        //        case ESystemType.CreatedBy:
        //            sql.AppendLine("SELECT DISTINCT A.[CreatedBy]");
        //            break;
        //        case ESystemType.DeletedBy:
        //            sql.AppendLine("SELECT DISTINCT A.[DeletedBy]");
        //            break;
        //        case ESystemType.InstanceOf:
        //            sql.AppendLine("SELECT DISTINCT A.[InstanceOfId]");
        //            break;
        //        case ESystemType.LastModifiedBy:
        //            sql.AppendLine("SELECT DISTINCT A.[LastModifiedBy]");
        //            break;
        //    }

        //    sql.AppendLine("FROM [Assets] A WITH (NoLock)");
        //    sql.AppendLine("WHERE A.[Id] IN (");

        //    for (int i = 0; i < assetIds.Count; i++)
        //    {
        //        if (i == assetIds.Count - 1)
        //        {
        //            sql.AppendLine("'" + assetIds[i].ToString() + "')");
        //        }
        //        else
        //        {
        //            sql.AppendLine("'" + assetIds[i].ToString() + "',");
        //        }
        //    }

        //    if ((systemType != ESystemType.DeletedBy) && (systemType != ESystemType.Deleted))
        //    {
        //        sql.AppendLine("AND A.[Deleted] IS NULL");
        //    }

        //    List<string> values = new List<string>();

        //    using (SqlDataReader rdr = base.OpenDataReaderInLine(sql.ToString(), new List<SqlParameter>()))
        //    {
        //        if ((rdr != null) && (rdr.HasRows))
        //        {
        //            while (rdr.Read())
        //            {
        //                if (!rdr.IsDBNull(0)) { values.Add(rdr.GetGuid(0).ToString()); }
        //            }
        //        }
        //    }

        //    return values;
        //}

    }

}