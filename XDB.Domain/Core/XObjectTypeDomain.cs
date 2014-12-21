
using System;
using System.Collections.Generic;

using XDB.Models;
using XDB.Common;
using XDB.Common.Enumerations;
using XDB.Common.Exceptions;
using XDB.Common.Extensions;
using XDB.Common.Interfaces;
using XDB.Repositories;

namespace XDB.Domains
{

    /// <summary>
    /// Primary entry point for manipulating <see cref="XObjectType"/> objects
    /// </summary>
    public class XObjectTypeDomain<T> : XBaseDomain, IXObjectTypeDomain<T> where T : XBase, IXObjectType
    {

        private IXObjectTypeRepository<T> dal = new XObjectTypeRepository<T>();

        public XObjectTypeDomain() : base(ECommonObjectType.XObjectType) { }

        public IXObjectType Get(Guid id)
        {
            return this.dal.Get(id);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assetTypeName"></param>
        /// <returns></returns>
        public IXObjectType GetByName(string assetTypeName)
        {
            throw new Exception("NOT IMPLEMENTED");
            //SqlBaseDal dal = new SqlBaseDal();
            //List<SqlParameter> paramList = new List<SqlParameter>();
            //paramList.Add(new SqlParameter("@Name", assetTypeName));
            //Guid assetTypeId = dal.ExecuteScalarGuidInLine("SELECT [Id] FROM [AssetTypes] WITH (NoLock) WHERE [Name] = @Name", paramList);
            //if (assetTypeId != new Guid())
            //{
            //    return this.Get(assetTypeId);
            //}
            //return null;
        }

        public bool AllowAssets(Guid assetTypeId)
        {
            throw new Exception("NOT IMPLEMENTED");
            //return this.dal.ExecuteScalarBool(string.Format("SELECT [AllowAssets] FROM [AssetTypes] WHERE [Id] = '{0}'", assetTypeId));
        }

        public void Save(T xObjectType, Guid userId)
        {
            this.dal.Save(xObjectType);
            //if (!new AssetTypePropertyRelationDal(this.ConnectionString).Save(assettype.Properties))
            //{
            //    throw new LogicalException("Error while saving AssetType-Property relations.");
            //}
        }

        //public bool Migrate(Guid assetTypeId, Core.Enumerations.EApplicationInstance target)
        //{
        //    AssetType assetType = this.Get(assetTypeId);

        //    if (assetType == null) { return false; }

        //    List<string> missingProperties = new List<string>();

        //    PropertyLayer propLayerSource = new PropertyLayer();
        //    PropertyLayer propLayer = new PropertyLayer(target);

        //    foreach (AssetTypePropertyRelation relation in assetType.Properties)
        //    {
        //        if (!propLayer.IsValidId(relation.PropertyId))
        //        {
        //            missingProperties.Add(propLayerSource.DisplayValue(relation.PropertyId));
        //        }
        //    }

        //    if (missingProperties.Count > 0)
        //    {
        //        if (missingProperties.Count == 1)
        //        {
        //            throw new LogicalException(string.Format("Target instance is missing a property: {0}", missingProperties[0]));
        //        }
        //        else
        //        {
        //            StringBuilder err = new StringBuilder();
        //            err.AppendLine(string.Format("Target instance is missing {0} properties:<br/>", missingProperties.Count));
        //            for (int i = 0; i < missingProperties.Count; i++)
        //            {
        //                err.AppendLine(string.Format("{0}) {1}<br/>", i + 1, missingProperties[i]));
        //            }
        //            throw new LogicalException(err.ToString());
        //        }
        //    }

        //    for (int i = 0; i < assetType.Properties.Count; i++)
        //    {
        //        assetType.Properties[i].IsDirty = true;
        //        assetType.Properties[i].IsNew = true;
        //    }

        //    assetType.IsDirty = true;
        //    assetType.IsNew = true;

        //    return new AssetTypeLayer(target).Save(assetType, Core.Constants.MemberIds.Admin);

        //}

        public bool Delete(Guid assetTypeId, Guid userId)
        {
            throw new Exception("NOT IMPLEMENTED");
            //if (assetTypeId.CompareTo(Constants.AssetTypeIds.User) == 0)
            //{
            //    throw new LogicalException("Cannot delete AssetType=User");
            //}
            //else
            //{
            //    return this.dal.AssetType_Delete(assetTypeId, userId);
            //}
        }

        public Guid? ParentId(Guid assetTypeId)
        {
            return this.dal.ParentId(assetTypeId);
        }

        public IList<Guid> AssetType_GetChildren(Guid assetTypeId, bool includeAllDescendants)
        {
            return this.dal.Children(assetTypeId);
        }

        //public List<Guid> GetChildren(Guid assetTypeId)
        //{
        //    return this.dal.GetChildren(assetTypeId);
        //}

        public Guid GetIdByAssetId(Guid assetId)
        {
            return this.dal.GetIdByAssetId(assetId);
        }

        public IList<Guid> GetStack(Guid assetTypeId)
        {
            throw new Exception("NOT IMPLEMENTED");

            //List<Guid> values = new List<Guid>();

            //Guid? parentId = this.ParentId(assetTypeId);
            //Guid? temp;

            //// get a direct line up the chain of parents
            //while (parentId.HasValue)
            //{
            //    values.Add(parentId.Value);
            //    parentId = this.ParentId(parentId.Value);
            //}

            //// then get all children
            //foreach (Guid id in this.AssetType_GetChildren(assetTypeId, true))
            //{
            //    if (!values.Contains(id)) { values.Add(id); }
            //}

            //return values;

        }

        public IDictionary<Guid, Guid> AssetTypes_GetDefaultViews(Guid userId, EXObjectRequestType requestType)
        {
            throw new Exception("NOT IMPLEMENTED");
            //if (new MemberLayer().IsAdmin(userId))
            //{
            //    return this.dal.AssetTypes_GetDefaultViews(null, requestType);
            //}
            //else
            //{
            //    return this.dal.AssetTypes_GetDefaultViews(userId, requestType);
            //}
        }

        /// <summary>
        /// Gets a dictionary of AssetType Ids and Names
        /// </summary>
        /// <param name="includePlaceholders">whether or not "placeholder" AssetTypes should be included (i.e. AllowAssets field)</param>
        /// <returns>Dictionary of Guid, string where Guid is the AssetType's Id and string is the AssetType's Name</returns>
        public IDictionary<Guid, string> GetDictionary(bool includePlaceholders)
        {
            return this.dal.GetDictionary(includePlaceholders);
        }

        public IDictionary<Guid, string> GetDictionaryByParentId(Guid parentAssetTypeId)
        {
            return this.dal.AssetTypes_GetDictionary(parentAssetTypeId);
        }

        //public Dictionary<Guid, EAssetRequestType> AssetTypes_GetForMember(Guid memberId)
        //{
        //    return this.dal.AssetTypes_GetForMember(memberId);           
        //}

        public IDictionary<Guid, string> AssetTypes_GetAvailableForCreation(Guid assetTypeId)
        {
            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            foreach (Guid id in this.dal.Children(assetTypeId))
            {
                if (this.dal.AllowAssets(id))
                {
                    values.Add(id, this.Name(id));
                }
            }

            return values;
        }

        public IDictionary<Guid, string> AssetType_GetLowestLevelParents(Guid assetTypeId)
        {

            Dictionary<Guid, string> values = new Dictionary<Guid, string>();

            IList<Guid> childIds = this.dal.Children(assetTypeId);

            foreach (Guid id in childIds)
            {
                IList<Guid> subChildIds = this.dal.Children(id);
                if ((subChildIds != null) || (subChildIds.Count == 0))
                {
                    if (!values.ContainsKey(id)) { values.Add(id, this.Name(id)); }
                }
            }

            return values;
        }

        public IDictionary<Guid, string> AssetTypes_GetParents()
        {
            return this.dal.AssetTypes_GetParents();
        }

        public bool HasProperty(Guid assetTypeId, Guid propertyId, bool isInstance)
        {
            throw new Exception("NOT IMPLEMENTED");
            //return new AssetTypePropertyRelationDal().Exists(assetTypeId, propertyId, isInstance);
        }

        //public List<AssetTypePropertyRelation> AssetTypePropertyRelations_Get(List<Guid> assetTypeIds, Guid propertyId)
        //{
        //    return new AssetTypePropertyRelationDal().GetCollectionByAssetTypeIdsAndPropertyId(assetTypeIds, propertyId);
        //}

        public XObjectTypeProperty AssetTypePropertyRelation_Get(Guid assetTypeId, Guid propertyId)
        {
            return new XObjectTypePropertyDomain().AssetTypePropertyRelation_Get(assetTypeId, propertyId);
        }

        //public System.Data.DataSet AssetTypes_GetHierarchy()
        //{
        //    return this.dal.AssetTypes_GetHierarchy();
        //}

        //public Dictionary<Guid, string> AssetType_GetPotentialDefaultViews(Guid assetTypeId)
        //{
        //    return this.dal.AssetType_GetPotentialDefaultViews(assetTypeId);
        //}

        public string Pluralization(Guid assetTypeId)
        {
            return this.dal.Pluralization(assetTypeId);
        }

        public bool HasAssets(Guid assetTypeId, EXObjectRequestType requestType, bool searchChildAssetTypes)
        {

            bool hasAssets = this.dal.HasAssets(assetTypeId, requestType);

            if (!hasAssets && searchChildAssetTypes)
            {
                IList<Guid> childAssetTypeIds = this.AssetType_GetChildren(assetTypeId, true);
                foreach (Guid childId in childAssetTypeIds)
                {
                    hasAssets = this.dal.HasAssets(childId, requestType);
                    if (hasAssets) { break; }
                }
            }

            return hasAssets;
        }

        public bool HasChildAssetTypes(Guid assetTypeId)
        {
            IList<Guid> childIds = this.AssetType_GetChildren(assetTypeId, true);
            if ((childIds == null) || (childIds.Count == 1)) { return false; }
            return true;
        }

        public string ReportingLabel(Guid assetTypeId, bool forInstances, bool usePlural)
        {
            return this.dal.ReportingLabel(assetTypeId, forInstances, usePlural);
        }

    }

}