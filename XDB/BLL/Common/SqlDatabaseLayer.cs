
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Constants;
using XDB.DAL;
using XDB.DataObjects;
using XDB.Enumerations;

namespace XDB.BLL
{
    /// <summary>
    /// FYI, for anyone coming in behind me ...
    /// THIS is where EVERYTHING happens in terms of:
    /// performance
    /// generating sql for:
    /// reports
    /// views
    /// which roles have edit access
    /// everything
    /// </summary>
    public class SqlDatabaseLayer
    {

        private XSqlDal _dal = new XSqlDal();

        //public bool ExecuteSql(string sqlStatement)
        //{
        //    return this._dal.ExecuteSql(sqlStatement);
        //}

        //public bool ExecuteSql(string sqlStatement, List<SqlParameter> paramList)
        //{
        //    return this._dal.ExecuteInLineSql(sqlStatement, paramList);
        //}

        //internal Dictionary<string, int> GetDictionaryStringInt(string sql)
        //{
        //    return this._dal.GetDictionaryStringInt(sql);
        //}

        //internal Dictionary<Guid, string> GetDictionary(string sql)
        //{
        //    return this._dal.GetDictionary(sql);
        //}

        //public int Count(string tableName)
        //{
        //    string sql = string.Format("SELECT COUNT(*) FROM [{0}]", tableName);
        //    return this._dal.ExecuteScalar(sql);
        //}

        public List<string> GetTableColumnNames(string tableName)
        {
            return this._dal.GetTableColumnNames(tableName);
        }

        public List<string> GetStoredProcColumnNames(string spName, List<SqlParameter> paramList)
        {
            return this._dal.GetStoredProcColumnNames(spName, paramList);
        }

        //public DataSet GetDataSet(string spName, List<SqlParameter> paramList)
        //{
        //    return this._dal.GetDataset(spName, paramList);
        //}

        //public DataSet GetDataSet(string sql)
        //{
        //    return this._dal.GetDataset(sql);
        //}

        internal List<string> GetGeneratedStoredProcNames(Dictionary<Guid, EAssetRequestType> assetTypes)
        {
            List<string> values = new List<string>();

            foreach (KeyValuePair<Guid, EAssetRequestType> kvp in assetTypes)
            {
                values.Add(this.FormatGeneratedStoredProcName(kvp.Key, (kvp.Value == EAssetRequestType.Instance), true));
                values.Add(this.FormatGeneratedStoredProcName(kvp.Key, (kvp.Value == EAssetRequestType.Instance), false));
            }

            return values;
        }

        //internal bool ViewExists(string viewName)
        //{
        //    return false;
        //}

        public string FilterString(string filterLogic, List<XFilter> filters)
        {
            string returnValue = string.Empty;

            if ((filters == null) || (filters.Count == 0)) { return string.Empty; }

            ArrayList tempValue = new ArrayList();
            XPropertyLayer propLayer = new XPropertyLayer();
            XListValueLayer plvLayer = new XListValueLayer();

            List<Guid> propIds = new List<Guid>();

            foreach (XFilter f in filters)
            {
                if (!propIds.Contains(f.PropertyId)) { propIds.Add(f.PropertyId); }
            }

            Dictionary<Guid, XProperty> props = new XPropertyLayer().GetObjectDictionary(propIds);

            foreach (XFilter filter in filters)
            {

                XProperty prop = props[filter.PropertyId];

                string propName = propLayer.DisplayValue(filter.PropertyId);
                string propVal = filter.Value;

                switch (prop.DataType)
                {
                    case EDataType.Date:
                        #region EDataType.Date

                        if (filter.Value != "NULL")
                        {
                            if (!prop.IsSystem)
                            {
                                Guid datePartId;
                                if (Guid.TryParse(filter.Value, out datePartId))
                                {
                                    //DatePart dPart = new DatePartLayer().DatePart_Get(datePartId);
                                    //if (dPart != null)
                                    //{
                                    //    propVal = dPart.DisplayValue;
                                    //}
                                }
                                else
                                {
                                    propVal = filter.Value;
                                }
                            }
                            else
                            {
                                propVal = filter.Value;
                            }
                        }

                        break;

                        #endregion

                    case EDataType.Asset:
                        #region EDataType.Asset

                        if (prop.IsSystem)
                        {
                            switch (prop.SystemType)
                            {
                                case ESystemType.AssetName:
                                case ESystemType.InstanceOf:
                                    //propName = r.ReportingLabel;
                                    break;
                                default:
                                    break;
                            }
                        }

                        propVal = new XObjectLayer().Name(new Guid(filter.Value));

                        break;

                        #endregion

                    case EDataType.PickList:
                        #region EDataType.PickList

                        XListValue plv = plvLayer.Get(new Guid(filter.Value));
                        propVal = string.IsNullOrEmpty(plv.DisplayValue) ? plv.Value : plv.DisplayValue;

                        break;

                        #endregion

                    case EDataType.User:
                        #region EDataType.User

                        propVal = new XUserLayer().DisplayValue(new Guid(filter.Value));

                        break;

                        #endregion

                    default:
                        #region Default

                        propVal = filter.Value;

                        break;
                        #endregion
                }

                if (filter.Value == new Guid().ToString()) { propVal = "null"; }

                tempValue.Add(string.Format("{0} {1} {2}", propName, filter.Operator, propVal));

            }

            return string.Format(filterLogic, tempValue.ToArray());

        }

        public string FormatGeneratedRoleViewName(Guid roleId, Guid assetTypeId, EPermissionType permissionType)
        {
            return string.Format("vwRole_{0}_{1}_{2}", roleId, assetTypeId, permissionType.GetHashCode());
        }

        private string FormatGeneratedStoredProcName(Guid assetTypeId, bool isInstance, bool isForRaw)
        {
            string val = string.Empty;

            if (isInstance)
            {
                if (isForRaw)
                {
                    val = string.Format("spr_gen{0}_I_Raw_Update", assetTypeId);
                }
                else
                {
                    val = string.Format("spr_gen{0}_I_Update", assetTypeId);
                }
            }
            else
            {
                if (isForRaw)
                {
                    val = string.Format("spr_gen{0}_D_Raw_Update", assetTypeId);
                }
                else
                {
                    val = string.Format("spr_gen{0}_D_Update", assetTypeId);
                }
            }

            return val.Replace("-", "");
        }

        public string FormatGeneratedTableName(Guid assetTypeId, bool isInstance, bool isForRaw)
        {
            string val = string.Empty;

            if (isInstance)
            {
                if (isForRaw)
                {
                    val = string.Format("gen{0}_I_Raw", assetTypeId);
                }
                else
                {
                    val = string.Format("gen{0}_I", assetTypeId);
                }
            }
            else
            {
                if (isForRaw)
                {
                    val = string.Format("gen{0}_D_Raw", assetTypeId);
                }
                else
                {
                    val = string.Format("gen{0}_D", assetTypeId);
                }
            }

            return val.Replace("-", "");

        }

        public StringBuilder GetSqlString(StringBuilder baseQuery, Guid assetTypeId, EAssetRequestType requestType, string tempTableName, Dictionary<Guid, string> propertyIds)
        {

            var isInstance = (requestType == EAssetRequestType.Instance);

            // determine base table names
            var tableName_D = this.FormatGeneratedTableName(assetTypeId, false, false);
            var tableName_D_Raw = this.FormatGeneratedTableName(assetTypeId, false, true);
            var tableName_I = this.FormatGeneratedTableName(assetTypeId, true, false);
            var tableName_I_Raw = this.FormatGeneratedTableName(assetTypeId, true, true);

            Dictionary<Guid, XObjectTypeProperty> relations = new Dictionary<Guid, XObjectTypeProperty>();

            #region Prefetch all AssetType-Property relations

            List<Guid> propIds = new List<Guid>();
            foreach (var kvp in propertyIds)
            {
                propIds.Add(kvp.Key);
            }

            XObjectTypePropertyDal atprdal = new XObjectTypePropertyDal();
            foreach (XObjectTypeProperty relation in atprdal.GetCollectionByAssetTypeIdAndPropertyIds(assetTypeId, propIds))
            {
                relations.Add(relation.PropertyId, relation);
            }

            #endregion

            StringBuilder final = new StringBuilder();

            var atLayer = new XObjectTypeLayer();

            var tempDefTblUsed = false;
            var tempInsTblUsed = false;

            foreach (KeyValuePair<Guid, string> kvp in propertyIds)
            {

                // get the AssetType-Property relation
                if (!relations.ContainsKey(kvp.Key))
                {
                    continue;
                }

                var relation = relations[kvp.Key];

                var xTblNameTemp = string.Empty;
                var xTblName = string.Empty;
                var xTblNameRaw = string.Empty;

                if (requestType == EAssetRequestType.Definition)
                {
                    xTblNameTemp = string.Format("{0}_D", tempTableName);
                    xTblName = tableName_D;
                    xTblNameRaw = tableName_D_Raw;
                    tempDefTblUsed = true;
                }
                else
                {
                    if (relation.IsInstance)
                    {
                        xTblNameTemp = string.Format("{0}_I", tempTableName);
                        xTblName = tableName_I;
                        xTblNameRaw = tableName_I_Raw;
                        tempInsTblUsed = true;
                    }
                    else
                    {
                        xTblNameTemp = string.Format("{0}_D", tempTableName);
                        xTblName = tableName_D;
                        xTblNameRaw = tableName_D_Raw;
                        tempDefTblUsed = true;
                    }
                }

                final.AppendFormat("SELECT COMPLETE.[Key], COMPLETE.[{0}] AS [Value], COMPLETE.[Enabled] FROM (", kvp.Value).AppendLine();
                final.AppendFormat("\tSELECT DISTINCT B.[{0}] AS [Key], A.[{1}], 0 AS [Enabled]", kvp.Key, kvp.Value).AppendLine();
                final.AppendFormat("\t\tFROM [{0}] AS A", xTblName).AppendLine();
                final.AppendFormat("\tINNER JOIN [{0}] AS B ON B.[AssetId] = A.[AssetId]", xTblNameRaw).AppendLine();
                final.AppendFormat("\tWHERE [{0}] NOT IN (", kvp.Value).AppendLine();
                final.AppendFormat("\t\tSELECT DISTINCT [{0}] FROM [#{1}]", kvp.Value, xTblNameTemp).AppendLine();
                final.AppendLine("\t)");
                final.AppendLine("\t\tUNION");
                final.AppendFormat("\tSELECT DISTINCT B.[{0}] AS [Key], [{1}], 1 AS [Enabled]", kvp.Key, kvp.Value).AppendLine();
                final.AppendFormat("\tFROM [#{0}] AS C", xTblNameTemp).AppendLine();
                final.AppendFormat("\t\tINNER JOIN [{0}] AS B ON B.[AssetId] = C.[AssetId]", xTblNameRaw).AppendLine();
                final.AppendLine("\t) COMPLETE");
                final.AppendLine("ORDER BY [Value]").AppendLine(string.Empty);

                //if (((relation.IsInstance && requestType == EAssetRequestType.Instance)) || ((!relation.IsInstance && requestType == EAssetRequestType.Definition)))
                //{

                //}
                //else if (!relation.IsInstance && requestType == EAssetRequestType.Instance)
                //{

                //    if (!defTablesAdded)
                //    {
                //        baseQuery.AppendFormat("INNER JOIN [dbo].[{0}] C ON [C].[AssetId] = [B].[{1}]", tblDefNameRaw, Constants.PropertyIds.InstanceOf).AppendLine();
                //        baseQuery.AppendFormat("INNER JOIN [dbo].[{0}] D ON [D].[AssetId] = [C].[AssetId]", tblDefName).AppendLine();
                //        defTablesAdded = true;
                //    }

                //    var colName = kvp.Value;
                //    if (kvp.Key.CompareTo(Constants.PropertyIds.InstanceOf) == 0)
                //    {
                //        colName = atLayer.ReportingLabel(assetTypeId, false, false);
                //        final.AppendFormat("SELECT COMPLETE.[Key], COMPLETE.[{0}] AS [Value], COMPLETE.[Enabled] FROM (", colName).AppendLine();
                //        final.AppendFormat("\tSELECT DISTINCT B.[{0}] AS [Key], A.[{1}], 0 AS [Enabled]", kvp.Key, colName).AppendLine();
                //        final.AppendFormat("\t\tFROM [{0}] AS A", tblName).AppendLine();
                //        final.AppendFormat("\tINNER JOIN [{0}] AS B ON B.[AssetId] = A.[AssetId]", tblNameRaw).AppendLine();
                //        final.AppendFormat("\tWHERE [{0}] NOT IN (", colName).AppendLine();
                //        final.AppendFormat("\t\tSELECT DISTINCT [{0}] FROM [#{1}]", kvp.Value, tempTableName).AppendLine();
                //        final.AppendLine("\t)");
                //        final.AppendLine("\t\tUNION");
                //        final.AppendFormat("\tSELECT DISTINCT B.[{0}] AS [Key], [{1}], 1 AS [Enabled]", kvp.Key, colName).AppendLine();
                //        final.AppendFormat("\tFROM [#{0}] AS C", tempTableName).AppendLine();
                //        final.AppendFormat("\t\tINNER JOIN [{0}] AS B ON B.[AssetId] = C.[AssetId]", tblNameRaw).AppendLine();
                //        final.AppendLine("\t) COMPLETE");
                //        final.AppendLine("ORDER BY [Value]").AppendLine(string.Empty);
                //    } 
                //    else
                //    {
                //        final.AppendFormat("SELECT COMPLETE.[Key], COMPLETE.[{0}] AS [Value], COMPLETE.[Enabled] FROM (", colName).AppendLine();
                //        final.AppendFormat("\tSELECT DISTINCT B.[{0}] AS [Key], A.[{1}], 0 AS [Enabled]", kvp.Key, colName).AppendLine();
                //        final.AppendFormat("\t\tFROM [{0}] AS A", tblName).AppendLine();
                //        final.AppendFormat("\tINNER JOIN [{0}] AS B ON B.[AssetId] = A.[AssetId]", tblNameRaw).AppendLine();
                //        final.AppendFormat("\tWHERE [{0}] NOT IN (", colName).AppendLine();
                //        final.AppendFormat("\t\tSELECT DISTINCT [{0}] FROM [#{1}]", kvp.Value, tblDefName).AppendLine(); // yes
                //        final.AppendLine("\t)");
                //        final.AppendLine("\t\tUNION");
                //        final.AppendFormat("\tSELECT DISTINCT B.[{0}] AS [Key], [{1}], 1 AS [Enabled]", kvp.Key, colName).AppendLine();
                //        final.AppendFormat("\tFROM [#{0}] AS C", tempTableName).AppendLine();
                //        final.AppendFormat("\t\tINNER JOIN [{0}] AS B ON B.[AssetId] = C.[AssetId]", tblNameRaw).AppendLine();
                //        final.AppendLine("\t) COMPLETE");
                //        final.AppendLine("ORDER BY [Value]").AppendLine(string.Empty);
                //    }

                //} 
                //else
                //{
                //    final.AppendFormat("SELECT COMPLETE.[Key], COMPLETE.[{0}] AS [Value], COMPLETE.[Enabled] FROM (", kvp.Value).AppendLine();
                //    final.AppendFormat("\tSELECT DISTINCT B.[{0}] AS [Key], A.[{1}], 0 AS [Enabled]", kvp.Key, kvp.Value).AppendLine();
                //    final.AppendFormat("\t\tFROM [{0}] AS A", tblName).AppendLine();
                //    final.AppendFormat("\tINNER JOIN [{0}] AS B ON B.[AssetId] = A.[AssetId]", tblNameRaw).AppendLine();
                //    final.AppendFormat("\tWHERE [{0}] NOT IN (", kvp.Value).AppendLine();
                //    final.AppendFormat("\t\tSELECT DISTINCT [{0}] FROM [#{1}]", kvp.Value, tempTableName).AppendLine();
                //    final.AppendLine("\t)");
                //    final.AppendLine("\t\tUNION");
                //    final.AppendFormat("\tSELECT DISTINCT B.[{0}] AS [Key], [{1}], 1 AS [Enabled]", kvp.Key, kvp.Value).AppendLine();
                //    final.AppendFormat("\tFROM [#{0}] AS C", tempTableName).AppendLine();
                //    final.AppendFormat("\t\tINNER JOIN [{0}] AS B ON B.[AssetId] = C.[AssetId]", tblNameRaw).AppendLine();
                //    final.AppendLine("\t) COMPLETE");
                //    final.AppendLine("ORDER BY [Value]").AppendLine(string.Empty);
                //}

            }

            if (tempDefTblUsed) { final.AppendFormat("DROP TABLE [#{0}_D]", tempTableName).AppendLine(); }
            if (tempInsTblUsed) { final.AppendFormat("DROP TABLE [#{0}_I]", tempTableName).AppendLine(); }

            StringBuilder complete = new StringBuilder(baseQuery.ToString());
            complete.AppendLine();
            complete.Append(final.ToString());

            return complete;

        }

        public StringBuilder GetSqlString(Guid assetTypeId,
                                          EAssetRequestType requestType,
                                          List<PropertySelect> reportProps,
                                          List<XFilter> filters,
                                          XFilter dynamicFilter,
                                          string filterLogic,
                                          bool includeOrderBy,
                                          int orderByIndex,
                                          bool isGroupingCount,
                                          string intoTblName,
                                          bool allFields)
        {
            XObjectLayer aLayer = new XObjectLayer();
            XObjectTypeLayer atLayer = new XObjectTypeLayer();
            XObjectTypePropertyDal atprDal = new XObjectTypePropertyDal();

            StringBuilder finalSql = new StringBuilder();

            StringBuilder selects = new StringBuilder();
            StringBuilder froms = new StringBuilder();
            StringBuilder groupBys = new StringBuilder();
            StringBuilder havings = new StringBuilder();
            StringBuilder _wheres = new StringBuilder();

            List<string> joinedTables = new List<string>(); // for keeping track of which tables we've added to the _froms

            var isInstance = (requestType == EAssetRequestType.Instance);

            string atName = atLayer.Name(assetTypeId);
            string _rptLabel = atLayer.ReportingLabel(assetTypeId, isInstance, false);

            string lblDef = string.Empty;
            string lblIns = string.Empty;

            if (isInstance)
            {
                lblDef = atLayer.ReportingLabel(assetTypeId, false, false);
                lblIns = _rptLabel;
            }
            else
            {
                lblDef = _rptLabel;
                lblIns = atLayer.ReportingLabel(assetTypeId, true, false);
            }

            #region determine base table names

            string tblName = this.FormatGeneratedTableName(assetTypeId, isInstance, false);
            string tblNameRaw = this.FormatGeneratedTableName(assetTypeId, isInstance, true);

            string defTblNameRaw = this.FormatGeneratedTableName(assetTypeId, !isInstance, true);
            string defTblName = this.FormatGeneratedTableName(assetTypeId, !isInstance, false);

            #endregion

            selects.AppendLine("SELECT");

            froms.AppendFormat("FROM [dbo].[{0}] AS [A]", tblName).AppendLine();
            froms.AppendFormat("INNER JOIN [dbo].[{0}] AS [B] ON [B].[AssetId] = [A].[AssetId]", tblNameRaw).AppendLine();

            List<Guid> propIds = new List<Guid>();
            #region Prefetch all properties

            foreach (XFilter f in filters)
            {
                if (!propIds.Contains(f.PropertyId)) { propIds.Add(f.PropertyId); }
            }

            foreach (PropertySelect prop in reportProps)
            {
                if (prop.CustomReportFieldType != ECustomReportFieldType.NotApplicable) continue;
                if (!propIds.Contains(prop.PropertyId)) { propIds.Add(prop.PropertyId); }
                if ((prop.SubPropertyId.HasValue) && (!propIds.Contains(prop.SubPropertyId.Value))) { propIds.Add(prop.SubPropertyId.Value); }
            }

            Dictionary<Guid, XProperty> props = new XPropertyLayer().GetObjectDictionary(propIds);

            #endregion

            Dictionary<Guid, XObjectTypeProperty> relations = new Dictionary<Guid, XObjectTypeProperty>();
            #region Prefetch all AssetType-Property relations

            foreach (XObjectTypeProperty relation in atprDal.GetCollectionByAssetTypeIdAndPropertyIds(assetTypeId, propIds))
            {
                relations.Add(relation.PropertyId, relation);
            }

            #endregion

            if (reportProps.Count == 0)
            {
                if (!string.IsNullOrEmpty(intoTblName))
                {
                    selects.AppendFormat("A.* INTO [#{0}]", intoTblName).AppendLine();
                }
                else
                {
                    if (allFields)
                    {
                        selects.AppendLine("A.*");
                    }
                    else
                    {
                        selects.AppendLine("\t[A].[AssetId] AS [AssetId]").AppendLine();
                        selects.AppendLine(",\t[A].[Name]");
                    }
                }
            }
            else
            {

                if (!isGroupingCount) { selects.AppendLine("\t[A].[AssetId] AS [AssetId]"); }

                int i = 0;
                foreach (PropertySelect reportProp in reportProps)
                {

                    if ((i == 0) && (!isGroupingCount))
                    {
                        selects.AppendFormat(",\t[A].[Name] AS [{0}]", _rptLabel).AppendLine();
                    }
                    else
                    {

                        XProperty p = props[reportProp.PropertyId];
                        XObjectTypeProperty relation = null;

                        if (relations.ContainsKey(reportProp.PropertyId)) { relation = relations[reportProp.PropertyId]; }

                        if (relation != null)
                        {
                            if ((reportProp.SubPropertyId.HasValue) && (reportProp.SubPropertyId.Value.CompareTo(new Guid()) != 0))
                            {
                                #region subProperty processing

                                // if we are here, we know that the PropertyId is an Asset (or a User - which technically is still an Asset)
                                // we need to join into that table so we can retrieve the subProperty value

                                XProperty subProp = props[reportProp.SubPropertyId.Value];

                                string tempTableName = string.Empty;

                                if (subProp.IsSystem)
                                {

                                    tempTableName = this.FormatGeneratedTableName(p.AssetTypeId.Value, p.AssetTypeIsInstance.Value, false);
                                    string tempTableNameAlias = reportProp.PropertyName.Replace(" ", "_") + "_Data";
                                    froms.AppendFormat("LEFT JOIN [dbo].[{0}] AS [{1}] ON [{2}].[AssetId] = [B].[{3}]", tempTableName, tempTableNameAlias, tempTableNameAlias, reportProp.PropertyId).AppendLine();

                                    switch (subProp.SystemType)
                                    {
                                        case ESystemType.AssetName:
                                            if (p.AssetTypeId.Value.CompareTo(XObjectTypeIds.User) == 0)
                                            {
                                                selects.AppendFormat("\t,[{0}].[Name] AS [{1}.BUN]", tempTableNameAlias, reportProp.PropertyName).AppendLine();
                                            }
                                            else
                                            {
                                                selects.AppendFormat("\t,[{0}].[{1}] AS [{2}]", tempTableNameAlias, reportProp.SubPropertyName, reportProp.SubPropertyName).AppendLine();
                                            }
                                            break;
                                        case ESystemType.AssetType:
                                            break;
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
                                            break;
                                        case ESystemType.LastModified:
                                            break;
                                        case ESystemType.LastModifiedBy:
                                            break;
                                    }
                                }
                                else
                                {
                                    XObjectTypeProperty tempRel = atprDal.AssetTypePropertyRelation_Get(p.AssetTypeId.Value, reportProp.SubPropertyId.Value);

                                    if (tempRel.IsInstance)
                                    {
                                        tempTableName = this.FormatGeneratedTableName(p.AssetTypeId.Value, true, false);
                                    }
                                    else
                                    {
                                        tempTableName = this.FormatGeneratedTableName(p.AssetTypeId.Value, false, false);
                                    }

                                    if (!joinedTables.Contains(tempTableName))
                                    {
                                        joinedTables.Add(tempTableName);
                                        froms.AppendFormat("LEFT JOIN [dbo].[{0}] ON [{1}].[AssetId] = [B].[{2}]", tempTableName, tempTableName, reportProp.PropertyId).AppendLine();
                                    }
                                    selects.AppendFormat("\t,[{0}].[{1}] AS [{2}]", tempTableName, reportProp.SubPropertyName, reportProp.SubPropertyName).AppendLine();
                                }

                                #endregion
                            }
                            else
                            {
                                #region reportProperty without a subProperty

                                if ((isInstance) && (relation.IsInstance))
                                {
                                    if (isGroupingCount)
                                    {
                                        selects.AppendFormat("\t[B].[{0}] AS [Id]", reportProp.PropertyId).AppendLine();
                                        selects.AppendFormat("\t,[A].[{0}] AS [Key]", reportProp.PropertyName).AppendLine();
                                        selects.AppendFormat("\t,COUNT([A].[{0}]) AS [Value]", reportProp.PropertyName).AppendLine();
                                        groupBys.AppendFormat("GROUP BY [B].[{0}], [A].[{1}]", reportProp.PropertyId, reportProp.PropertyName).AppendLine();
                                        havings.AppendFormat("HAVING (COUNT([A].[{0}]) > 0)", reportProp.PropertyName).AppendLine();

                                        //selects.AppendFormat("\t[A].[{0}] AS [Key]", reportProp.PropertyName).AppendLine();
                                        //selects.AppendFormat("\t,COUNT([A].[{0}]) AS [Value]", reportProp.PropertyName).AppendLine();
                                        //groupBys.AppendFormat("GROUP BY [A].[{0}]", reportProp.PropertyName).AppendLine();
                                        //havings.AppendFormat("HAVING (COUNT([A].[{0}]) > 0)", reportProp.PropertyName).AppendLine();
                                    }
                                    else
                                    {
                                        selects.AppendFormat("\t,[A].[{0}] AS [{1}]", reportProp.PropertyName, reportProp.PropertyName).AppendLine();
                                    }
                                    //if (relation.IsInheritedValue)
                                    //{
                                    //    _selects.AppendFormat("\t,[dbo].[GetPropertyValueLookup]([dbo].[GetAssetParentIdByChildId]([A].[AssetId]), '{0}') AS [{1}]", reportProp.PropertyId, reportProp.PropertyName).AppendLine();
                                    //}
                                    //else
                                    //{

                                    //}                                
                                }
                                else if ((isInstance) && (!relation.IsInstance))
                                {

                                    if (!joinedTables.Contains(defTblNameRaw))
                                    {
                                        joinedTables.Add(defTblNameRaw);
                                        froms.AppendFormat("INNER JOIN [dbo].[{0}] ON [{1}].[AssetId] = [B].[{2}]", defTblNameRaw, defTblNameRaw, Constants.XPropertyIds.InstanceOf).AppendLine();
                                    }

                                    if (!joinedTables.Contains(defTblName))
                                    {
                                        joinedTables.Add(defTblName);
                                        froms.AppendFormat("INNER JOIN [dbo].[{0}] ON [{1}].[AssetId] = [{2}].[AssetId]", defTblName, defTblName, defTblNameRaw).AppendLine();
                                    }

                                    selects.AppendFormat("\t,[{0}].[{1}] AS [{2}]", defTblName, reportProp.PropertyName, reportProp.PropertyName).AppendLine();

                                }
                                else
                                {
                                    if (isGroupingCount)
                                    {
                                        selects.AppendFormat("\t[B].[{0}] AS [Id]", reportProp.PropertyId).AppendLine();
                                        selects.AppendFormat("\t,[A].[{0}] AS [Key]", reportProp.PropertyName).AppendLine();
                                        selects.AppendFormat("\t,COUNT([A].[{0}]) AS [Value]", reportProp.PropertyName).AppendLine();
                                        groupBys.AppendFormat("GROUP BY [B].[{0}], [A].[{1}]", reportProp.PropertyId, reportProp.PropertyName).AppendLine();
                                        havings.AppendFormat("HAVING (COUNT([A].[{0}]) > 0)", reportProp.PropertyName).AppendLine();

                                        //selects.AppendFormat("\t[A].[{0}] AS [Key]", reportProp.PropertyName).AppendLine();
                                        //selects.AppendFormat("\t,COUNT([A].[{0}]) AS [Value]", reportProp.PropertyName).AppendLine();
                                        //groupBys.AppendFormat("GROUP BY [A].[{0}]", reportProp.PropertyName).AppendLine();
                                        //havings.AppendFormat("HAVING (COUNT([A].[{0}]) > 0)", reportProp.PropertyName).AppendLine();
                                    }
                                    else
                                    {
                                        selects.AppendFormat("\t,[A].[{0}] AS [{1}]", reportProp.PropertyName, reportProp.PropertyName).AppendLine();
                                    }
                                }

                                #endregion
                            }
                        }
                        else
                        {
                            #region property without a relation to the assetType - indicates a system property of some sort (assetType, instanceOf, etc)

                            if (reportProp.PropertyId.CompareTo(Constants.XPropertyIds.InstanceOf) == 0)
                            {
                                selects.AppendFormat("\t,[A].[{0}] AS [{1}]", lblDef, lblDef).AppendLine();
                            }
                            else if (reportProp.PropertyId.CompareTo(Constants.XPropertyIds.InstanceOfDesc) == 0)
                            {

                                string tempTableName = this.FormatGeneratedTableName(assetTypeId, false, false);

                                if (!joinedTables.Contains(tempTableName))
                                {
                                    joinedTables.Add(tempTableName);
                                    froms.AppendFormat("INNER JOIN [dbo].[{0}] ON [{1}].[AssetId] = [{2}].[AssetId]", tempTableName, tempTableName, tempTableName).AppendLine();
                                }
                                selects.AppendFormat("\t,[{0}].[Description] AS [{1} Description]", tempTableName, lblDef).AppendLine();

                            }
                            else if (reportProp.PropertyId.CompareTo(Constants.XPropertyIds.Description) == 0)
                            {
                                selects.AppendFormat("\t,[A].[{0}] AS [{1}]", reportProp.PropertyName, reportProp.PropertyName).AppendLine();
                            }
                            else if (reportProp.PropertyId.CompareTo(Constants.XPropertyIds.AssetType) == 0)
                            {
                                string tempDefTableNameRaw = this.FormatGeneratedTableName(assetTypeId, false, true);
                                string tempDefTableName = this.FormatGeneratedTableName(assetTypeId, false, false);

                                if (!joinedTables.Contains(tempDefTableNameRaw))
                                {
                                    joinedTables.Add(tempDefTableNameRaw);
                                    froms.AppendFormat("INNER JOIN [dbo].[{0}] ON [{1}].[AssetId] = [B].[{2}]", tempDefTableNameRaw, tempDefTableNameRaw, Constants.XPropertyIds.InstanceOfDesc).AppendLine();
                                }

                                if (!joinedTables.Contains(tempDefTableName))
                                {
                                    joinedTables.Add(tempDefTableName);
                                    froms.AppendFormat("INNER JOIN [dbo].[{0}] ON [{1}].[AssetId] = [{2}].[AssetId]", tempDefTableName, tempDefTableName, tempDefTableNameRaw).AppendLine();
                                }

                                selects.AppendFormat("\t,[{0}].[Asset Type] AS [Asset Type]", tempDefTableName).AppendLine();

                            }
                            else if (reportProp.PropertyId.CompareTo(Constants.XPropertyIds.AssetName) == 0)
                            {
                                selects.AppendFormat("\t,[A].[Name] AS [{0}]", reportProp.PropertyName).AppendLine();
                            }
                            else
                            {
                                selects.AppendFormat("\t,[A].[{0}] AS [{1}]", reportProp.PropertyName, reportProp.PropertyName).AppendLine();
                            }

                            #endregion
                        }

                    }
                    i++;
                }
            }


            if (filters.Count > 0)
            {
                #region process filter condition(s)

                ArrayList wheres = new ArrayList();

                _wheres.AppendLine("WHERE");

                foreach (XFilter f in filters)
                {

                    XProperty prop = props[f.PropertyId];
                    XObjectTypeProperty relation = atLayer.AssetTypePropertyRelation_Get(assetTypeId, f.PropertyId);

                    if (prop.IsSystem)
                    {
                        switch (prop.SystemType)
                        {
                            case ESystemType.AssetName:
                                if (f.OperatorId == EFilterOperator.Like)
                                {
                                    wheres.Add(string.Format("[A].[{0}] LIKE '%{1}%'", atName, f.Value));
                                }
                                else
                                {
                                    wheres.Add(string.Format("[A].[{0}] {1} '{2}'", atName, f.Operator, f.Value));
                                }
                                break;
                            case ESystemType.AssetType:
                                wheres.Add(string.Format("[B].[{0}] {1} '{2}'", f.PropertyId, f.Operator, f.Value));
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        switch (prop.DataType)
                        {

                            case EDataType.Asset:

                                if ((isInstance) && (relation.IsInstance))
                                {
                                    Guid tempVal;
                                    if (Guid.TryParse(f.Value, out tempVal))
                                    {
                                        if (tempVal.CompareTo(new Guid()) == 0)
                                        {
                                            if (f.OperatorId == EFilterOperator.EqualTo)
                                            {
                                                wheres.Add(string.Format("([B].[{0}] = '') OR ([B].[{1}] IS NULL) ", f.PropertyId, f.PropertyId));
                                            }
                                            else if (f.OperatorId == EFilterOperator.NotEqual)
                                            {
                                                wheres.Add(string.Format("([B].[{0}] != '') AND ([B].[{1}] IS NOT NULL) ", f.PropertyId, f.PropertyId));
                                            }
                                            else
                                            {
                                                // wtf?
                                            }
                                        }
                                        else
                                        {
                                            wheres.Add(string.Format("[B].[{0}] {1} '{2}'", f.PropertyId, f.Operator, f.Value));
                                        }
                                    }
                                }
                                else if ((isInstance) && (!relation.IsInstance))
                                {
                                    if (!joinedTables.Contains(defTblNameRaw))
                                    {
                                        joinedTables.Add(defTblNameRaw);
                                        froms.AppendFormat("INNER JOIN [dbo].[{0}] ON [{1}].[AssetId] = [B].[{2}]", defTblNameRaw, defTblNameRaw, Constants.XPropertyIds.InstanceOf).AppendLine();
                                    }
                                    wheres.Add(string.Format("[{0}].[{1}] {2} '{3}'", defTblNameRaw, f.PropertyId, f.Operator, f.Value));
                                }
                                else
                                {
                                    wheres.Add(string.Format("[B].[{0}] {1} '{2}'", f.PropertyId, f.Operator, f.Value));
                                }

                                break;

                            case EDataType.PickList:

                                if ((isInstance) && (relation.IsInstance))
                                {
                                    wheres.Add(string.Format("[B].[{0}] {1} '{2}'", f.PropertyId, f.Operator, f.Value));
                                }
                                else if ((isInstance) && (!relation.IsInstance))
                                {
                                    if (!joinedTables.Contains(defTblNameRaw))
                                    {
                                        joinedTables.Add(defTblNameRaw);
                                        froms.AppendFormat("INNER JOIN [dbo].[{0}] ON [{1}].[AssetId] = [B].[{2}]", defTblNameRaw, defTblNameRaw, Constants.XPropertyIds.InstanceOf).AppendLine();
                                    }
                                    wheres.Add(string.Format("[{0}].[{1}] {2} '{3}'", defTblNameRaw, f.PropertyId, f.Operator, f.Value));
                                }
                                else
                                {
                                    wheres.Add(string.Format("[B].[{0}] {1} '{2}'", f.PropertyId, f.Operator, f.Value));
                                }

                                break;

                            default:

                                bool isNotNull = ((f.Value == new Guid().ToString()) || (string.IsNullOrEmpty(f.Value)));

                                if ((isInstance) && (relation.IsInstance))
                                {
                                    if (isNotNull)
                                    {
                                        wheres.Add(string.Format("[B].[{0}] IS NOT NULL", f.PropertyId));
                                    }
                                    else
                                    {
                                        wheres.Add(string.Format("[B].[{0}] {1} '{2}'", f.PropertyId, f.Operator, f.Value));
                                    }
                                }
                                else if ((isInstance) && (!relation.IsInstance))
                                {
                                    if (isNotNull)
                                    {
                                        wheres.Add(string.Format("[{0}].[{1}] IS NOT NULL", defTblNameRaw, f.PropertyId));
                                    }
                                    else
                                    {
                                        wheres.Add(string.Format("[{0}].[{1}] {2} '{3}'", defTblNameRaw, f.PropertyId, f.Operator, f.Value));
                                    }
                                }
                                else
                                {
                                    if (isNotNull)
                                    {
                                        wheres.Add(string.Format("[B].[{0}] IS NOT NULL", f.PropertyId));
                                    }
                                    else
                                    {
                                        wheres.Add(string.Format("[B].[{0}] {1} '{2}'", f.PropertyId, f.Operator, f.Value));
                                    }
                                }

                                break;

                        }
                    }
                }

                if (wheres.Count > 0) { _wheres.AppendLine(string.Format(filterLogic, wheres.ToArray())); }

                #endregion
            }

            if (dynamicFilter != null)
            {
                if (_wheres.Length > 0)
                {
                    _wheres.AppendFormat("AND ([B].[{0}] = @Val)", dynamicFilter.PropertyId).AppendLine();
                }
                else
                {
                    _wheres.AppendFormat("WHERE ([B].[{0}] = @Val)", dynamicFilter.PropertyId).AppendLine();
                }
            }

            #region build final sql string from every portion we have constructed

            finalSql.Append(selects);
            finalSql.Append(froms);
            if (_wheres.Length > 0) { finalSql.Append(_wheres); }
            if (groupBys.Length > 0) { finalSql.Append(groupBys); }
            if (havings.Length > 0) { finalSql.Append(havings); }
            if (includeOrderBy) { finalSql.AppendFormat("ORDER BY [{0}]", _rptLabel).AppendLine(); }

            #endregion

            return finalSql;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assetTypeId"></param>
        /// <param name="requestType"></param>
        /// <param name="reportProps"></param>
        /// <param name="filters"></param>
        /// <param name="filterLogic"></param>
        /// <param name="includeOrderBy"></param>
        /// <param name="forSingleProp">For returning just a distinct list of single properties (e.x. All locations for hardware instances)</param>
        /// <returns></returns>
        public StringBuilder GetSqlString(Guid assetTypeId,
                                          EAssetRequestType requestType,
                                          List<PropertySelect> reportProps,
                                          List<XFilter> filters,
                                          string filterLogic,
                                          bool includeOrderBy,
                                          bool forSingleProp)
        {

            XObjectLayer aLayer = new XObjectLayer();
            XObjectTypeLayer atLayer = new XObjectTypeLayer();
            XObjectTypePropertyDal atprDal = new XObjectTypePropertyDal();

            StringBuilder finalSql = new StringBuilder();

            StringBuilder selects = new StringBuilder();
            StringBuilder froms = new StringBuilder();
            StringBuilder _wheres = new StringBuilder();

            List<string> joinedTables = new List<string>(); // for keeping track of which tables we've added to the _froms

            var isInstance = (requestType == EAssetRequestType.Instance);

            //var atName = atLayer.Name(assetTypeId);
            var _rptLabel = atLayer.ReportingLabel(assetTypeId, isInstance, false);

            var lblDef = string.Empty;
            var lblIns = string.Empty;

            if (isInstance)
            {
                lblDef = atLayer.ReportingLabel(assetTypeId, false, false);
                lblIns = _rptLabel;
            }
            else
            {
                lblDef = _rptLabel;
                lblIns = atLayer.ReportingLabel(assetTypeId, true, false);
            }

            #region determine base table names

            string tblName = this.FormatGeneratedTableName(assetTypeId, isInstance, false);
            string tblNameRaw = this.FormatGeneratedTableName(assetTypeId, isInstance, true);

            string defTblNameRaw = this.FormatGeneratedTableName(assetTypeId, !isInstance, true);
            string defTblName = this.FormatGeneratedTableName(assetTypeId, !isInstance, false);

            #endregion

            if (forSingleProp)
            {
                selects.AppendLine("SELECT DISTINCT");
            }
            else
            {
                selects.AppendLine("SELECT");
            }

            froms.AppendFormat("FROM [dbo].[{0}] AS [A]", tblName).AppendLine();
            froms.AppendFormat("INNER JOIN [dbo].[{0}] AS [B] ON [B].[AssetId] = [A].[AssetId]", tblNameRaw).AppendLine();

            List<Guid> propIds = new List<Guid>();
            #region Prefetch all properties

            foreach (XFilter f in filters)
            {
                if (!propIds.Contains(f.PropertyId)) { propIds.Add(f.PropertyId); }
            }

            foreach (PropertySelect prop in reportProps)
            {
                if (prop.CustomReportFieldType == ECustomReportFieldType.NotApplicable)
                {
                    if (!propIds.Contains(prop.PropertyId)) { propIds.Add(prop.PropertyId); }
                    if ((prop.SubPropertyId.HasValue) && (!propIds.Contains(prop.SubPropertyId.Value))) { propIds.Add(prop.SubPropertyId.Value); }
                }
            }

            Dictionary<Guid, XProperty> props = new XPropertyLayer().GetObjectDictionary(propIds);

            #endregion

            Dictionary<Guid, XObjectTypeProperty> relations = new Dictionary<Guid, XObjectTypeProperty>();
            #region Prefetch all AssetType-Property relations

            foreach (XObjectTypeProperty relation in atprDal.GetCollectionByAssetTypeIdAndPropertyIds(assetTypeId, propIds))
            {
                relations.Add(relation.PropertyId, relation);
            }

            #endregion

            if (!forSingleProp) { selects.AppendLine("\t[A].[AssetId] AS [AssetId]"); }

            var i = 0;
            foreach (PropertySelect reportProp in reportProps)
            {
                #region process report properties

                if ((!forSingleProp) && (i == 0))
                {
                    selects.AppendFormat(",\t[A].[Name] AS [{0}]", _rptLabel).AppendLine();
                }
                else
                {

                    XProperty p = props[reportProp.PropertyId];
                    XObjectTypeProperty relation = null;

                    if (relations.ContainsKey(reportProp.PropertyId)) { relation = relations[reportProp.PropertyId]; }

                    if (relation != null)
                    {
                        if ((reportProp.SubPropertyId.HasValue) && (reportProp.SubPropertyId.Value.CompareTo(new Guid()) != 0))
                        {
                            #region subProperty processing

                            // if we are here, we know that the PropertyId is an Asset (or a User - which technically is still an Asset)
                            // we need to join into that table so we can retrieve the subProperty value

                            XProperty subProp = props[reportProp.SubPropertyId.Value];

                            string tempTableName = string.Empty;

                            if (subProp.IsSystem)
                            {

                                var isIns = p.AssetTypeIsInstance.HasValue ? p.AssetTypeIsInstance.Value : false;
                                tempTableName = this.FormatGeneratedTableName(p.AssetTypeId.Value, isIns, false);
                                string tempTableNameAlias = reportProp.PropertyName.Replace(" ", "_") + "_Data";
                                froms.AppendFormat("LEFT JOIN [dbo].[{0}] AS [{1}] ON [{2}].[AssetId] = [B].[{3}]", tempTableName, tempTableNameAlias, tempTableNameAlias, reportProp.PropertyId).AppendLine();

                                switch (subProp.SystemType)
                                {
                                    case ESystemType.AssetName:
                                        if (p.AssetTypeId.Value.CompareTo(XObjectTypeIds.User) == 0)
                                        {
                                            selects.AppendFormat("\t,[{0}].[Name] AS [{1}.BUN]", tempTableNameAlias, reportProp.PropertyName).AppendLine();
                                        }
                                        else
                                        {
                                            selects.AppendFormat("\t,[{0}].[{1}] AS [{2}]", tempTableNameAlias, reportProp.SubPropertyName, reportProp.SubPropertyName).AppendLine();
                                        }
                                        break;
                                    case ESystemType.AssetType:
                                        break;
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
                                        break;
                                    case ESystemType.LastModified:
                                        break;
                                    case ESystemType.LastModifiedBy:
                                        break;
                                }
                            }
                            else
                            {
                                XObjectTypeProperty tempRel = atprDal.AssetTypePropertyRelation_Get(p.AssetTypeId.Value, reportProp.SubPropertyId.Value);

                                if (tempRel.IsInstance)
                                {
                                    tempTableName = this.FormatGeneratedTableName(p.AssetTypeId.Value, true, false);
                                }
                                else
                                {
                                    tempTableName = this.FormatGeneratedTableName(p.AssetTypeId.Value, false, false);
                                }

                                if (!joinedTables.Contains(tempTableName))
                                {
                                    joinedTables.Add(tempTableName);
                                    froms.AppendFormat("LEFT JOIN [dbo].[{0}] ON [{1}].[AssetId] = [B].[{2}]", tempTableName, tempTableName, reportProp.PropertyId).AppendLine();
                                }
                                selects.AppendFormat("\t,[{0}].[{1}] AS [{2}]", tempTableName, reportProp.SubPropertyName, reportProp.SubPropertyName).AppendLine();
                            }

                            #endregion
                        }
                        else
                        {
                            #region reportProperty without a subProperty

                            if (reportProp.CustomReportFieldType == ECustomReportFieldType.NotApplicable)
                            {

                                if ((isInstance) && (relation.IsInstance))
                                {
                                    if (forSingleProp)
                                    {
                                        selects.AppendFormat("\t[B].[{0}] AS [Key]", reportProp.PropertyId).AppendLine();
                                        selects.AppendFormat("\t,[A].[{0}] AS [Value]", reportProp.PropertyName).AppendLine();
                                    }
                                    else
                                    {
                                        selects.AppendFormat("\t,[A].[{0}] AS [{1}]", reportProp.PropertyName, reportProp.PropertyName).AppendLine();
                                    }
                                }
                                else if ((isInstance) && (!relation.IsInstance))
                                {

                                    if (!joinedTables.Contains(defTblNameRaw))
                                    {
                                        joinedTables.Add(defTblNameRaw);
                                        froms.AppendFormat("INNER JOIN [dbo].[{0}] ON [{1}].[AssetId] = [B].[{2}]", defTblNameRaw, defTblNameRaw, Constants.XPropertyIds.InstanceOf).AppendLine();
                                    }

                                    if (!joinedTables.Contains(defTblName))
                                    {
                                        joinedTables.Add(defTblName);
                                        froms.AppendFormat("INNER JOIN [dbo].[{0}] ON [{1}].[AssetId] = [{2}].[AssetId]", defTblName, defTblName, defTblNameRaw).AppendLine();
                                    }

                                    if (forSingleProp)
                                    {
                                        selects.AppendFormat("\t[{0}].[{1}] AS [Key]", defTblNameRaw, reportProp.PropertyId).AppendLine();
                                        selects.AppendFormat("\t,[{0}].[{1}] AS [Value]", defTblName, reportProp.PropertyName).AppendLine();
                                    }
                                    else
                                    {
                                        selects.AppendFormat("\t,[{0}].[{1}] AS [{2}]", defTblName, reportProp.PropertyName, reportProp.PropertyName).AppendLine();
                                    }

                                }
                                else
                                {
                                    if (forSingleProp)
                                    {
                                        selects.AppendFormat("\t[B].[{0}] AS [Key]", reportProp.PropertyId).AppendLine();
                                        selects.AppendFormat("\t,[A].[{0}] AS [Value]", reportProp.PropertyName).AppendLine();
                                    }
                                    else
                                    {
                                        selects.AppendFormat("\t,[A].[{0}] AS [{1}]", reportProp.PropertyName, reportProp.PropertyName).AppendLine();
                                    }

                                }

                            }
                            else
                            {
                                switch (reportProp.CustomReportFieldType)
                                {
                                    case ECustomReportFieldType.Calculated:
                                        break;

                                    case ECustomReportFieldType.Concatentation:
                                        break;

                                    case ECustomReportFieldType.RangeLabel:
                                        break;

                                    case ECustomReportFieldType.TimeElapsed:

                                        switch (reportProp.TimeElapsedFormat)
                                        {
                                            case ETimeElapsedFormat.Absolute:
                                                break;
                                            case ETimeElapsedFormat.Decimal:
                                                break;
                                        }

                                        switch (reportProp.TimeElapsedType)
                                        {
                                            case ETimeElapsedType.AlwaysCurrentDate:
                                                break;
                                            case ETimeElapsedType.SpecificDate:
                                                break;
                                        }

                                        break;

                                    case ECustomReportFieldType.ValueLabel:
                                        break;

                                }
                            }

                            #endregion
                        }
                    }
                    else
                    {
                        #region property without a relation to the assetType - indicates a system property of some sort (assetType, instanceOf, etc)

                        if (reportProp.CustomReportFieldType == ECustomReportFieldType.NotApplicable)
                        {
                            if (reportProp.PropertyId.CompareTo(Constants.XPropertyIds.InstanceOf) == 0)
                            {
                                selects.AppendFormat("\t,[A].[{0}] AS [{1}]", lblDef, lblDef).AppendLine();
                            }
                            else if (reportProp.PropertyId.CompareTo(Constants.XPropertyIds.InstanceOfDesc) == 0)
                            {

                                var tempTblName = this.FormatGeneratedTableName(assetTypeId, false, false);

                                if (!joinedTables.Contains(tempTblName))
                                {
                                    joinedTables.Add(tempTblName);
                                    froms.AppendFormat("INNER JOIN [dbo].[{0}] ON [{1}].[AssetId] = [B].[{2}]", tempTblName, tempTblName, Constants.XPropertyIds.InstanceOf).AppendLine();
                                }

                                selects.AppendFormat("\t,[{0}].[Description] AS [{1} Description]", tempTblName, lblDef).AppendLine();

                            }
                            else if (reportProp.PropertyId.CompareTo(Constants.XPropertyIds.Description) == 0)
                            {
                                selects.AppendFormat("\t,[A].[{0}] AS [{1}]", reportProp.PropertyName, reportProp.PropertyName).AppendLine();
                            }
                            else if (reportProp.PropertyId.CompareTo(Constants.XPropertyIds.AssetType) == 0)
                            {
                                string tempDefTableNameRaw = this.FormatGeneratedTableName(assetTypeId, false, true);
                                string tempDefTableName = this.FormatGeneratedTableName(assetTypeId, false, false);

                                if (!joinedTables.Contains(tempDefTableNameRaw))
                                {
                                    joinedTables.Add(tempDefTableNameRaw);
                                    froms.AppendFormat("INNER JOIN [dbo].[{0}] ON [{1}].[AssetId] = [B].[{2}]", tempDefTableNameRaw, tempDefTableNameRaw, Constants.XPropertyIds.InstanceOfDesc).AppendLine();
                                }

                                if (!joinedTables.Contains(tempDefTableName))
                                {
                                    joinedTables.Add(tempDefTableName);
                                    froms.AppendFormat("INNER JOIN [dbo].[{0}] ON [{1}].[AssetId] = [{2}].[AssetId]", tempDefTableName, tempDefTableName, tempDefTableNameRaw).AppendLine();
                                }

                                selects.AppendFormat("\t,[{0}].[Asset Type] AS [Asset Type]", tempDefTableName).AppendLine();

                            }
                            else if (reportProp.PropertyId.CompareTo(Constants.XPropertyIds.AssetName) == 0)
                            {
                                selects.AppendFormat("\t,[A].[Name] AS [{0}]", reportProp.PropertyName).AppendLine();
                            }
                            else
                            {
                                selects.AppendFormat("\t,[A].[{0}] AS [{1}]", reportProp.PropertyName, reportProp.PropertyName).AppendLine();
                            }
                        }
                        else
                        {
                            switch (reportProp.CustomReportFieldType)
                            {
                                case ECustomReportFieldType.Calculated:
                                case ECustomReportFieldType.Concatentation:
                                case ECustomReportFieldType.RangeLabel:
                                case ECustomReportFieldType.TimeElapsed:
                                case ECustomReportFieldType.ValueLabel:
                                    break;
                            }
                        }


                        #endregion
                    }

                }
                i++;

                #endregion
            }

            if (filters.Count > 0)
            {
                #region process filter condition(s)

                ArrayList wheres = new ArrayList();

                _wheres.AppendLine("WHERE");

                foreach (XFilter f in filters)
                {

                    XProperty prop = props[f.PropertyId];
                    XObjectTypeProperty relation = atLayer.AssetTypePropertyRelation_Get(assetTypeId, f.PropertyId);

                    if (prop.IsSystem)
                    {
                        switch (prop.SystemType)
                        {
                            case ESystemType.AssetName:

                                //if (requestType == EAssetRequestType.Instance)
                                //{
                                //    if (!joinedTables.Contains(defTblNameRaw))
                                //    {
                                //        joinedTables.Add(defTblNameRaw);
                                //        froms.AppendFormat("INNER JOIN [dbo].[{0}] ON [{1}].[AssetId] = [B].[{2}]", defTblNameRaw, defTblNameRaw, Constants.PropertyIds.InstanceOf).AppendLine();
                                //    }
                                //    if (f.OperatorId == EFilterOperator.Like)
                                //    {
                                //        wheres.Add(string.Format("[{0}].[{1}] LIKE '%{2}%'", defTblNameRaw, Constants.PropertyIds.InstanceOf, f.Value));
                                //    }
                                //    else
                                //    {
                                //        wheres.Add(string.Format("[{0}].[{1}] {2} '{3}'", defTblNameRaw, Constants.PropertyIds.InstanceOf, f.Operator, f.Value));
                                //    }                                    
                                //}
                                //else
                                //{
                                if (f.OperatorId == EFilterOperator.Like)
                                {
                                    wheres.Add(string.Format("[A].[Name] LIKE '%{0}%'", f.Value));
                                }
                                else
                                {
                                    wheres.Add(string.Format("[A].[Name] {0} '{1}'", f.Operator, f.Value));
                                }
                                //}
                                break;
                            case ESystemType.AssetType:
                                wheres.Add(string.Format("[B].[{0}] {1} '{2}'", f.PropertyId, f.Operator, f.Value));
                                break;
                            case ESystemType.InstanceOf:
                                //if (!joinedTables.Contains(defTblNameRaw))
                                //{
                                //        joinedTables.Add(defTblNameRaw);
                                //        froms.AppendFormat("INNER JOIN [dbo].[{0}] ON [{1}].[AssetId] = [B].[{2}]", defTblNameRaw, defTblNameRaw, Constants.PropertyIds.InstanceOf).AppendLine();
                                //}
                                //wheres.Add(string.Format("[{0}].[{1}] {2} '{3}'", defTblNameRaw, Constants.PropertyIds.InstanceOf, f.Operator, f.Value));
                                wheres.Add(string.Format("[B].[{0}] {1} '{2}'", Constants.XPropertyIds.InstanceOf, f.Operator, f.Value));
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        switch (prop.DataType)
                        {

                            case EDataType.Asset:

                                if ((isInstance) && (relation.IsInstance))
                                {
                                    Guid tempVal;
                                    if (Guid.TryParse(f.Value, out tempVal))
                                    {
                                        if (tempVal.CompareTo(new Guid()) == 0)
                                        {
                                            if (f.OperatorId == EFilterOperator.EqualTo)
                                            {
                                                wheres.Add(string.Format("([B].[{0}] = '') OR ([B].[{1}] IS NULL) ", f.PropertyId, f.PropertyId));
                                            }
                                            else if (f.OperatorId == EFilterOperator.NotEqual)
                                            {
                                                wheres.Add(string.Format("([B].[{0}] != '') AND ([B].[{1}] IS NOT NULL) ", f.PropertyId, f.PropertyId));
                                            }
                                            else
                                            {
                                                // wtf?
                                            }
                                        }
                                        else
                                        {
                                            wheres.Add(string.Format("[B].[{0}] {1} '{2}'", f.PropertyId, f.Operator, f.Value));
                                        }
                                    }
                                }
                                else if ((isInstance) && (!relation.IsInstance))
                                {
                                    if (!joinedTables.Contains(defTblNameRaw))
                                    {
                                        joinedTables.Add(defTblNameRaw);
                                        froms.AppendFormat("INNER JOIN [dbo].[{0}] ON [{1}].[AssetId] = [B].[{2}]", defTblNameRaw, defTblNameRaw, Constants.XPropertyIds.InstanceOf).AppendLine();
                                    }
                                    wheres.Add(string.Format("[{0}].[{1}] {2} '{3}'", defTblNameRaw, f.PropertyId, f.Operator, f.Value));
                                }
                                else
                                {
                                    wheres.Add(string.Format("[B].[{0}] {1} '{2}'", f.PropertyId, f.Operator, f.Value));
                                }

                                break;

                            case EDataType.PickList:

                                if ((isInstance) && (relation.IsInstance))
                                {
                                    wheres.Add(string.Format("[B].[{0}] {1} '{2}'", f.PropertyId, f.Operator, f.Value));
                                }
                                else if ((isInstance) && (!relation.IsInstance))
                                {
                                    if (!joinedTables.Contains(defTblNameRaw))
                                    {
                                        joinedTables.Add(defTblNameRaw);
                                        froms.AppendFormat("INNER JOIN [dbo].[{0}] ON [{1}].[AssetId] = [B].[{2}]", defTblNameRaw, defTblNameRaw, Constants.XPropertyIds.InstanceOf).AppendLine();
                                    }
                                    wheres.Add(string.Format("[{0}].[{1}] {2} '{3}'", defTblNameRaw, f.PropertyId, f.Operator, f.Value));
                                }
                                else
                                {
                                    wheres.Add(string.Format("[B].[{0}] {1} '{2}'", f.PropertyId, f.Operator, f.Value));
                                }

                                break;

                            default:

                                bool isNotNull = ((f.Value == new Guid().ToString()) || (string.IsNullOrEmpty(f.Value)));

                                if ((isInstance) && (relation.IsInstance))
                                {
                                    if (isNotNull)
                                    {
                                        wheres.Add(string.Format("[B].[{0}] IS NOT NULL", f.PropertyId));
                                    }
                                    else
                                    {
                                        wheres.Add(string.Format("[B].[{0}] {1} '{2}'", f.PropertyId, f.Operator, f.Value));
                                    }
                                }
                                else if ((isInstance) && (!relation.IsInstance))
                                {
                                    if (isNotNull)
                                    {
                                        wheres.Add(string.Format("[{0}].[{1}] IS NOT NULL", defTblNameRaw, f.PropertyId));
                                    }
                                    else
                                    {
                                        wheres.Add(string.Format("[{0}].[{1}] {2} '{3}'", defTblNameRaw, f.PropertyId, f.Operator, f.Value));
                                    }
                                }
                                else
                                {
                                    if (isNotNull)
                                    {
                                        wheres.Add(string.Format("[B].[{0}] IS NOT NULL", f.PropertyId));
                                    }
                                    else
                                    {
                                        wheres.Add(string.Format("[B].[{0}] {1} '{2}'", f.PropertyId, f.Operator, f.Value));
                                    }
                                }

                                break;

                        }
                    }
                }

                if (wheres.Count > 0) { _wheres.AppendLine(string.Format(filterLogic, wheres.ToArray())); }

                #endregion
            }

            #region build final sql string from every portion we have constructed

            finalSql.Append(selects.ToString());
            finalSql.Append(froms.ToString());
            if (filters.Count > 0) { finalSql.Append(_wheres.ToString()); }

            if (forSingleProp)
            {
                finalSql.AppendLine("ORDER BY [Value]");
            }
            else
            {
                if (includeOrderBy) { finalSql.AppendFormat("ORDER BY [{0}]", _rptLabel).AppendLine(); }
            }

            #endregion

            return finalSql;

        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="r"></param>
        ///// <param name="includeOrderBy">If this SQL is used to create a view, the ORDER BY cannot be included.  Therefore, this allows you to opt-out of its inclusion within the generated SQL</param>
        ///// <returns></returns>
        //public StringBuilder GetSqlString(Report r, bool includeOrderBy)
        //{
        //    var requestType = r.IsInstance ? EAssetRequestType.Instance : EAssetRequestType.Definition;

        //    return this.GetSqlString(r.AssetTypeId, requestType, r.ReportProperties, r.Filters, r.FilterLogic, includeOrderBy, false);
        //}

        public StringBuilder GetSqlString(List<XRoleHelper> roleHelpers)
        {
            StringBuilder sql = new StringBuilder();

            int count = roleHelpers.Count;
            for (int i = 0; i < count; i++)
            {
                XRoleHelper helper = roleHelpers[i];
                string roleViewName = this.FormatGeneratedRoleViewName(helper.RoleId, helper.AssetTypeId, EPermissionType.Edit);
                sql.AppendFormat("SELECT [AssetId], [Asset Code], '{0}' AS [Role] FROM [{1}]", helper.RoleName, roleViewName).AppendLine();
                if (i < (count - 1))
                {
                    sql.AppendLine("UNION");
                }
            }

            return sql;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestType"></param>
        /// <param name="assetDisplayName"></param>
        /// <param name="assetTypeId"></param>
        /// <param name="assetTypeIds"></param>
        /// <param name="assetIds"></param>
        /// <param name="reportProperties"></param>
        /// <param name="orderResults"></param>
        /// <param name="definitionLabel"></param>
        /// <param name="instanceLabel"></param>
        /// <returns></returns>
        public StringBuilder GetSqlForGenTable(EAssetRequestType requestType,
                                       string assetDisplayName,
                                       Guid assetTypeId,
                                       List<Guid> assetTypeIds,
                                       List<Guid> assetIds,
                                       List<PropertySelect> reportProperties,
                                       bool orderResults,
                                       string definitionLabel,
                                       string instanceLabel,
                                       string intoTableName,
                                       ESqlType sqlType)
        {

            XObjectTypePropertyDal relationDal = new XObjectTypePropertyDal();

            XPropertyDal propDal = new XPropertyDal();

            // Note: The biggest task here is to correctly construct the SQL statement
            // which (due to the high level of normalization) is difficult
            StringBuilder sql = new StringBuilder();
            StringBuilder selects = new StringBuilder();
            StringBuilder wheres = new StringBuilder();

            StringBuilder sqlRaw = new StringBuilder();
            StringBuilder selectsRaw = new StringBuilder();

            sql.AppendLine("SELECT DISTINCT"); // TODO:  I removed the DISTINCT; determine if it was necessary
            sqlRaw.AppendLine("SELECT DISTINCT");

            // [Id], [Name], [DisplayValue], [AssetType] are AWLAYS the first columns.  Always
            sql.AppendLine("\t[A].[Id] AS [AssetId]");
            sql.AppendLine("\t,[A].[Name] AS [Name]");
            sql.AppendLine("\t,[A].[DisplayValue] AS [DisplayValue]");
            sql.AppendLine("\t,dbo.GetAssetType(A.[Id]) AS [Asset Type]");

            sqlRaw.AppendLine("\t[A].[Id] AS [AssetId]");
            sqlRaw.AppendLine("\t,[A].[Name] AS [8D145EE0-54D2-47DB-8785-5AE086C20DDE]");
            sqlRaw.AppendLine("\t,[A].[DisplayValue] AS [DisplayValue]");
            sqlRaw.AppendLine("\t,[A].[AssetTypeId] AS [48014265-A355-45EC-9878-94C041ABD927]");

            int propCount = 0;

            List<Guid> propIds = new List<Guid>();
            foreach (PropertySelect rp in reportProperties)
            {
                propIds.Add(rp.PropertyId);
            }

            Dictionary<Guid, XObjectTypeProperty> propRelations = new Dictionary<Guid, XObjectTypeProperty>();
            foreach (XObjectTypeProperty rel in relationDal.GetCollectionByAssetTypeIdAndPropertyIds(assetTypeId, propIds))
            {
                propRelations.Add(rel.PropertyId, rel);
            }

            List<Guid> propertyIds = new List<Guid>();
            foreach (PropertySelect rp in reportProperties)
            {
                if (!propertyIds.Contains(rp.PropertyId)) { propertyIds.Add(rp.PropertyId); }
            }

            Dictionary<Guid, XProperty> props = propDal.GetObjectDictionary(propertyIds);

            foreach (PropertySelect rp in reportProperties)
            {

                XProperty property = props[rp.PropertyId];
                XProperty subProp = rp.SubPropertyId.HasValue ? propDal.Get(rp.SubPropertyId.Value) : null;

                if (property == null) { continue; }

                string assetId = "Id"; // dynamically change to "InstanceOfId" based on if the property is at the definition level or the instance level

                XObjectTypeProperty relation = null; // relationDal.AssetTypePropertyRelation_Get(assetTypeId, rp.PropertyId);
                if (propRelations.ContainsKey(rp.PropertyId)) { relation = propRelations[rp.PropertyId]; }

                if (!property.IsSystem)
                {
                    if (relation == null) { continue; }
                    if ((requestType == EAssetRequestType.Instance) && (!relation.IsInstance))
                    {
                        assetId = "InstanceOfId";
                    }
                }

                if (rp.CustomReportFieldType == ECustomReportFieldType.NotApplicable)
                {
                    #region standard fields

                    var propDisplay = string.IsNullOrEmpty(property.DisplayValue) ? property.Name : property.DisplayValue;
                    var propId = rp.PropertyId.ToString(); // just to enhance readability later in this method

                    // system properties do not need a relationi.e. 
                    if (!property.IsSystem)
                    {
                        if (relation.IsInheritedValue)
                        {
                            selects.AppendFormat("\t,[dbo].[GetPropertyValueLookup]([dbo].[GetAssetParentIdByChildId]([A].[Id]), '{0}') AS [{1}]", propId, propDisplay.Trim()).AppendLine();
                            selectsRaw.AppendFormat("\t,[dbo].[GetPropertyValueLookup]([dbo].[GetAssetParentIdByChildId]([A].[Id]), '{0}') AS [{1}]", propId, propId).AppendLine();
                            continue;
                        }
                        else
                        {
                            if ((requestType == EAssetRequestType.Instance) && (!relation.IsInstance))
                            {
                                assetId = "InstanceOfId";
                            }
                        }
                    }

                    switch (property.DataType)
                    {

                        case EDataType.Asset:

                            #region EDataType.Asset

                            if (property.IsSystem)
                            {
                                if (subProp == null)
                                {
                                    switch (property.SystemType)
                                    {
                                        case ESystemType.InstanceOf:
                                            selects.AppendFormat("\t,dbo.GetAssetParentNameByInstanceId([A].[Id]) AS [{0}]", definitionLabel).AppendLine();
                                            selectsRaw.AppendFormat("\t,[A].[InstanceOfId] AS [{0}]", Constants.XPropertyIds.InstanceOf).AppendLine();
                                            break;
                                        default:
                                            selects.AppendFormat("\t,dbo.GetPropertyValueLookup([A].[{0}], '{1}') AS [{2}]", assetId, propId, propDisplay).AppendLine();
                                            selectsRaw.AppendFormat("\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propId).AppendLine();
                                            break;
                                    }
                                }
                                else
                                {
                                    propDisplay += ".";
                                    string temp = string.IsNullOrEmpty(subProp.DisplayValue) ? subProp.Name : subProp.DisplayValue;
                                    propDisplay += temp;
                                    selects.AppendFormat("\t,dbo.GetPropertyValueLookup(dbo.GetSystemValueLookup([A].[{0}], '{1}'), '{2}') AS [{3}]", assetId, propId, subProp.Id, propDisplay).AppendLine();
                                    selectsRaw.AppendFormat("\t,dbo.PropertyValue(dbo.GetSystemValueLookup([A].[{0}], '{1}'), '{2}') AS [{3}]", assetId, propId, subProp.Id, propDisplay).AppendLine();
                                }
                            }
                            else
                            {
                                if (subProp == null)
                                {
                                    if (property.AllowMultiValue)
                                    {
                                        selects.AppendFormat("\t,dbo.PropertyValueMulti_Asset([A].[{0}], '{1}') AS [{2}]", assetId, propId, propDisplay).AppendLine();
                                        selectsRaw.AppendFormat("\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propId).AppendLine(); // TODO: Not sure about this
                                    }
                                    else
                                    {
                                        selects.AppendFormat("\t,dbo.PropertyValue_Asset([A].[{0}], '{1}') AS [{2}]", assetId, propId, propDisplay).AppendLine();
                                        selectsRaw.AppendFormat("\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propId).AppendLine();
                                    }
                                }
                                else
                                {
                                    propDisplay += ".";
                                    string temp = string.IsNullOrEmpty(subProp.DisplayValue) ? subProp.Name : subProp.DisplayValue;
                                    propDisplay += temp;
                                    if (subProp.IsSystem)
                                    {
                                        selects.AppendFormat("\t,dbo.GetSystemValueLookup(dbo.GetPropertyValue([A].[{0}], '{1}'), '{2}') AS [{3}]", assetId, propId, subProp.Id, propDisplay).AppendLine();
                                    }
                                    else
                                    {
                                        selects.AppendFormat("\t,dbo.GetPropertyValueLookup(dbo.GetPropertyValue([A].[{0}], '{1}'), '{2}') AS [{3}]", assetId, propId, subProp.Id, propDisplay).AppendLine();
                                    }
                                }
                            }

                            #endregion

                            break;

                        case EDataType.Currency:

                            #region EDataType.Currency

                            selects.AppendFormat("\t,dbo.PropertyValue_Currency([A].[{0}], '{1}') AS [{2}]", assetId, propId, propDisplay).AppendLine();
                            selectsRaw.AppendFormat("\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propId).AppendLine();

                            #endregion

                            break;

                        case EDataType.Date:
                        case EDataType.DateTime:

                            #region EDataType.Date/DateTime

                            if (property.IsSystem)
                            {
                                switch (property.SystemType)
                                {
                                    case ESystemType.NotApplicable:
                                        selects.AppendFormat("\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propDisplay).AppendLine();
                                        selectsRaw.AppendFormat("\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propId).AppendLine();
                                        break;
                                    case ESystemType.Created:
                                        selects.AppendLine("\t,[A].[Created] AS [Created]");
                                        selectsRaw.AppendFormat("\t,[A].[Created] AS [{0}]", propId).AppendLine();
                                        break;
                                    case ESystemType.Deleted:
                                        selects.AppendLine("\t,[A].[Deleted] AS [Deleted]");
                                        selectsRaw.AppendFormat("\t,[A].[Deleted] AS [{0}]", propId).AppendLine();
                                        break;
                                    case ESystemType.LastModified:
                                        selects.AppendLine("\t,[A].[LastModified] AS [Last Modified]");
                                        selectsRaw.AppendFormat("\t,[A].[LastModified] AS [{0}]", propId).AppendLine();
                                        break;
                                }
                            }
                            else
                            {
                                selects.AppendFormat("\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propDisplay).AppendLine();
                                selectsRaw.AppendFormat("\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propId).AppendLine();
                            }

                            #endregion

                            break;

                        case EDataType.Dependency:

                            #region EDataType.Dependency

                            string columnName = string.IsNullOrEmpty(property.DisplayValue) ? property.Name : property.DisplayValue;

                            if (property.AllowMultiValue)
                            {
                                selects.AppendFormat("\t,dbo.PropertyValueMulti_Asset([A].[{0}], '{1}') AS [{2}]", assetId, propId, propDisplay).AppendLine();
                                selectsRaw.AppendFormat("\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{1}]", assetId, propId, propId).AppendLine(); // TODO: Not sure about this
                            }
                            else
                            {
                                selects.AppendFormat("\t,dbo.GetAssetValueById([pv{0}].[Value]) AS [{1}]", propCount, columnName).AppendLine();
                                selectsRaw.AppendFormat("\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{1}]", assetId, propId, propId).AppendLine();
                            }

                            #endregion

                            break;

                        case EDataType.Document:

                            #region EDataType.Document

                            // TODO: Implement SQL generation for this - provide a link to the doc?
                            selects.AppendFormat("\t,dbo.GetPropertyValueLookup([A].[{0}], '{1}') AS [{2}]", assetId, propId, propDisplay).AppendLine();

                            #endregion

                            break;

                        case EDataType.Image:

                            #region EDataType.Image

                            //string url = "'" + SystemFrameworkHelper.ImageServiceUrl + "/getImageStream?imageId='";
                            string url = string.Format("'{0}'", Config.ViewImageUrl);

                            selects.AppendFormat("\t,{0} + dbo.GetPropertyValueLookup([A].[Id], '{1}') AS [{2}]", url, property.Id.ToString(), propDisplay).AppendLine();

                            #endregion

                            break;

                        case EDataType.PickList:

                            #region EDataType.PickList

                            selects.AppendFormat("\t,dbo.PropertyValue_PickList([A].[{0}], '{1}') AS [{2}]", assetId, propId, propDisplay).AppendLine();
                            selectsRaw.AppendFormat("\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propId).AppendLine();

                            #endregion

                            break;

                        case EDataType.Float:
                        case EDataType.Int:
                        case EDataType.IPv4:
                        case EDataType.IPv6:
                        case EDataType.Memo:
                        case EDataType.Relation_Other:

                            selects.AppendFormat("\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propDisplay).AppendLine();
                            selectsRaw.AppendFormat("\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propId).AppendLine();

                            break;

                        case EDataType.Relation_ChildParent:

                            selects.AppendFormat("\t,dbo.GetAssetParentNameByChildId([A].[{0}]) AS [{1}]", assetId, propDisplay).AppendLine();
                            selectsRaw.AppendFormat("\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propId).AppendLine();

                            break;

                        case EDataType.Relation_ParentChild:

                            selects.AppendFormat("\t,dbo.GetAssetParentNameByChildId([A].[{0}]) AS [{1}]", assetId, propDisplay).AppendLine();
                            selectsRaw.AppendFormat("\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propId).AppendLine();

                            break;

                        case EDataType.Geo:
                        case EDataType.String:

                            #region EDataType.String

                            if (property.IsSystem)
                            {
                                switch (property.SystemType)
                                {

                                    case ESystemType.AssetName:

                                        //if (string.IsNullOrEmpty(assetDisplayName))
                                        //{
                                        //    sql.AppendLine("\t,IsNull([A].[DisplayValue], [A].[Name]) AS [Asset Name]");
                                        //}
                                        //else
                                        //{
                                        //    sql.AppendFormat("\t,IsNull([A].[DisplayValue], [A].[Name]) AS [{0}]", assetDisplayName).AppendLine();
                                        //    sql.AppendFormat("\t,[A].[Name] AS [Name]", assetDisplayName).AppendLine();
                                        //    sql.AppendFormat("\t,[A].[DisplayValue] AS [DisplayValue]", assetDisplayName).AppendLine();
                                        //}

                                        break;

                                    case ESystemType.AssetType:

                                        //selects.AppendLine("\t,dbo.GetAssetType(A.[Id]) AS [Asset Type]");
                                        //selectsRaw.AppendFormat("\t,(A.[AssetTypeId]) AS [{0}]", propId).AppendLine();

                                        break;

                                    case ESystemType.Description:

                                        selects.AppendLine("\t,A.[Description] AS [Description]");
                                        selectsRaw.AppendFormat("\t,(A.[Description]) AS [{0}]", propId).AppendLine();

                                        break;

                                    case ESystemType.InstanceOfDescription:

                                        selects.AppendFormat("\t,dbo.GetAssetParentDescByInstanceId([A].[Id]) AS [{0} Description]", definitionLabel).AppendLine();

                                        break;

                                    default:

                                        selects.AppendFormat("\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propDisplay).AppendLine();
                                        selectsRaw.AppendFormat("\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propId).AppendLine();

                                        break;
                                }
                            }
                            else
                            {
                                selects.AppendFormat("\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propDisplay).AppendLine();
                                selectsRaw.AppendFormat("\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propId).AppendLine();
                            }

                            #endregion

                            break;

                        case EDataType.URL:

                            selects.AppendFormat("\t,dbo.GetPropertyValueLookup([A].[Id], '{0}') AS [{1}]", propId, propDisplay).AppendLine();

                            break;

                        case EDataType.User:

                            if (property.IsSystem)
                            {
                                switch (property.SystemType)
                                {
                                    case ESystemType.CreatedBy:
                                        selects.AppendLine("\t,dbo.GetMemberValueById([A].[CreatedBy]) AS [Created By]");
                                        selectsRaw.AppendFormat("\t,[A].[CreatedBy] AS [{0}]", propId).AppendLine();
                                        break;

                                    case ESystemType.LastModifiedBy:
                                        selects.AppendLine("\t,dbo.GetMemberValueById([A].[LastModifiedBy]) AS [Last Modified By]");
                                        selectsRaw.AppendFormat("\t,[A].[LastModifiedBy] AS [{0}]", propId).AppendLine();
                                        break;

                                    case ESystemType.DeletedBy:
                                        if (subProp == null)
                                        {
                                            selects.AppendLine(string.Format("\t,dbo.GetMemberValueById(dbo.GetSystemValueLookup([A].[Id], '{0}')) AS [{1}]", propId, propDisplay));
                                        }
                                        else
                                        {
                                            propDisplay += ".";
                                            string temp = string.IsNullOrEmpty(subProp.DisplayValue) ? subProp.Name : subProp.DisplayValue;
                                            propDisplay += temp;
                                            selects.AppendLine(string.Format("\t,dbo.GetPropertyValueLookup(dbo.GetSystemValueLookup([A].[{0}], '{1}'), '{2}') AS [{3}]", assetId, propId, subProp.Id, propDisplay));
                                        }
                                        break;
                                }
                            }
                            else
                            {
                                if (subProp == null)
                                {
                                    selects.AppendFormat("\t,dbo.PropertyValue_Member([A].[Id], '{0}') AS [{1}]", propId, propDisplay).AppendLine();
                                    selectsRaw.AppendFormat("\t,dbo.PropertyValue([A].[Id], '{0}') AS [{1}]", propId, propId).AppendLine();
                                }
                                else
                                {
                                    propDisplay += ".";
                                    string temp = string.IsNullOrEmpty(subProp.DisplayValue) ? subProp.Name : subProp.DisplayValue;
                                    propDisplay += temp;
                                    selects.AppendLine(string.Format("\t,dbo.GetPropertyValueLookup(dbo.GetPropertyValue([A].[{0}], '{1}'), '{2}') AS [{3}]", assetId, propId, subProp.Id, propDisplay));
                                }
                            }
                            break;

                        default:
                            selects.AppendFormat("\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propDisplay).AppendLine();
                            selectsRaw.AppendFormat("\t,dbo.PropertyValue([A].[{0}], '{1}') AS [{2}]", assetId, propId, propId).AppendLine();
                            break;
                    }

                    #endregion
                }
                else
                {
                    #region custom fields

                    switch (rp.CustomReportFieldType)
                    {
                        case ECustomReportFieldType.Calculated:
                            List<string> temp = new List<string>();
                            foreach (Guid propertyId in rp.PropertyIds)
                            {
                                temp.Add(string.Format("nullif(CAST (dbo.udf_GetNumeric(dbo.GetPropertyValueLookup([A].[{0}], '{1}')) AS float),0)", assetId, propertyId));
                            }
                            string final = string.Format(rp.Logic, temp.ToArray());
                            selects.AppendLine("\t,CAST ((" + final + ") AS decimal(18,2)) AS [" + rp.Label + "]");
                            break;
                        case ECustomReportFieldType.Concatentation:
                            break;
                        case ECustomReportFieldType.RangeLabel:
                            break;
                        case ECustomReportFieldType.TimeElapsed:
                            if (property.IsSystem)
                            {
                                if (rp.TimeElapsedFormat == ETimeElapsedFormat.Absolute)
                                {
                                    selects.AppendLine(string.Format("\t,dbo.F_AGE_YYYY_MM_DD(dbo.GetSystemValueLookup([A].[{0}], '{1}'), GetDate()) AS [{2}]", assetId, rp.PropertyId, rp.Label));
                                }
                                else
                                {
                                    selects.AppendLine(string.Format("\t,cast(DATEDIFF ( D, dbo.GetSystemValueLookup([A].[{0}], '{1}'), GetDate() ) / cast(365 as float) as decimal(18, 2)) AS [{2}]", assetId, rp.PropertyId, rp.Label));
                                }
                            }
                            else
                            {
                                if (rp.TimeElapsedFormat == ETimeElapsedFormat.Absolute)
                                {
                                    selects.AppendLine(string.Format("\t,dbo.F_AGE_YYYY_MM_DD(dbo.GetPropertyValueLookup([A].[{0}], '{1}'), GetDate()) AS [{2}]", assetId, rp.PropertyId, rp.Label));
                                }
                                else
                                {
                                    selects.AppendLine(string.Format("\t,cast(DATEDIFF ( D, dbo.GetPropertyValueLookup([A].[{0}], '{1}'), GetDate() ) / cast(365 as float) as decimal(18, 2)) AS [{2}]", assetId, rp.PropertyId, rp.Label));
                                }
                            }
                            break;
                        case ECustomReportFieldType.ValueLabel:
                            break;
                    }

                    #endregion
                }

                propCount++;

            }

            int currentAssetCount = 0;
            int lastAssetIdIndex = assetIds.Count - 1;

            if ((assetIds != null) && (assetIds.Count > 0))
            {
                foreach (Guid assetId in assetIds)
                {
                    if (currentAssetCount == 0) { wheres.Append(" ([A].[Id] IN ("); }

                    if (currentAssetCount == lastAssetIdIndex)
                    {
                        wheres.AppendFormat(" '{0}'))", assetId);
                    }
                    else
                    {
                        wheres.AppendFormat(" '{0}',", assetId);
                    }

                    currentAssetCount++;
                }
            }
            else
            {
                if (assetTypeIds.Count == 1)
                {
                    wheres.AppendFormat(" ([A].[AssetTypeId] = '{0}')", assetTypeIds[0]);
                }
                else
                {
                    int atCount = assetTypeIds.Count - 1;
                    int atIndex = 0;
                    foreach (Guid atId in assetTypeIds)
                    {
                        if (atIndex == 0) { wheres.Append(" ([A].[AssetTypeId] IN ("); }

                        if (atIndex == atCount)
                        {
                            wheres.AppendFormat(" '{0}'))", atId);
                        }
                        else
                        {
                            wheres.AppendFormat(" '{0}',", atId);
                        }

                        atIndex++;
                    }
                }

                if (requestType == EAssetRequestType.Definition)
                {
                    wheres.AppendLine(" AND ([A].[IsInstance] = 0)");
                }
                else
                {
                    wheres.AppendLine(" AND ([A].[IsInstance] = 1)");
                }

                wheres.AppendLine("AND (A.[Deleted] IS NULL)");

            }

            //wheres.AppendLine("AND A.[Deleted] IS NULL AND A.[DeletedBy] IS NULL");

            sql.Append(selects.ToString());
            sqlRaw.Append(selectsRaw.ToString());

            if (!string.IsNullOrEmpty(intoTableName))
            {
                if (sqlType == ESqlType.Decoded)
                {
                    sql.AppendFormat("INTO [{0}]", intoTableName).AppendLine();
                }
                else
                {
                    sqlRaw.AppendFormat("INTO [{0}]", intoTableName).AppendLine();
                }
            }

            sql.AppendLine("FROM [Assets] A WITH (NoLock)");
            sql.AppendLine("WHERE");
            sql.Append(wheres.ToString());

            sqlRaw.AppendLine("FROM [Assets] A WITH (NoLock)");
            sqlRaw.AppendLine("WHERE");
            sqlRaw.Append(wheres.ToString());

            if (orderResults)
            {
                sql.Append("ORDER BY [Name]");
                sqlRaw.Append("ORDER BY [Name]");
            }

            if (sqlType == ESqlType.Decoded)
            {
                return sql;
            }
            else
            {
                return sqlRaw;
            }

        }

        public bool InsertIntoGenTables(Guid assetId, bool isInstance, Guid assetTypeId)
        {
            Dictionary<Guid, EAssetRequestType> assetTypes = new Dictionary<Guid, EAssetRequestType>();

            EAssetRequestType requestType = isInstance ? EAssetRequestType.Instance : EAssetRequestType.Definition;

            foreach (Guid id in new XObjectTypeLayer().GetStack(assetTypeId))
            {
                assetTypes.Add(id, requestType);
            }

            List<string> genStoredProcs = new SqlDatabaseLayer().GetGeneratedStoredProcNames(assetTypes);

            Dictionary<string, Guid> spNamesAssetId = new Dictionary<string, Guid>();

            foreach (string spName in genStoredProcs) { spNamesAssetId.Add(spName, assetId); }

            return this.InsertIntoGenTables(spNamesAssetId);
        }

        internal bool InsertIntoGenTables(Dictionary<string, Guid> spNamesAssetId)
        {
            foreach (KeyValuePair<string, Guid> kvp in spNamesAssetId)
            {
                this._dal.ExecuteSql(kvp.Key, new List<SqlParameter>() { new SqlParameter("@AssetId", kvp.Value) });
            }
            return true;
        }

    }

}