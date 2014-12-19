
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using XDB.BLL;
using XDB.Constants;
using XDB.DataObjects;
using XDB.Enumerations;
using XDB.Exceptions;
using XDB.Extensions;
using XDB.Interfaces;

namespace XDB.API
{

    public class XObjectService : XBaseService
    {

        public XObjectService() : base(ECommonObjectType.XObject) { }

        XObjectLayer bizLayer = new XObjectLayer();

        private static XObjectService instance;

        public static XObjectService Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new XObjectService();
                }
                return instance;
            }
        }

        //private AssetLa
        /// <summary>
        /// Determines whether or not the specified asset is valid; used prior to saving an asset to the database
        /// </summary>
        /// <param name="asset">an instance of an Asset</param>
        /// <returns>true if asset instance is valid; otherwise LogicalException thrown</returns>
        /// <exception cref="LogicalException">LogicalException</exception>
        private void Validate(XObject asset)
        {

            // Ensure a PK is defined
            if (asset.Id.CompareTo(new Guid()) == 0) { throw new LogicalException("Id cannot be null", "Id"); }

            // Ensure that asset instances are linked to the type of asset they are
            if (asset.InstanceOfId.HasValue)
            {
                if (!this.bizLayer.ValidId(asset.InstanceOfId.Value))
                {
                    throw new LogicalException("InstanceOfId is invalid", "InstanceOfId");
                }
            }

            // Ensure the asset has a name
            if (string.IsNullOrEmpty(asset.Name)) { throw new LogicalException("Name must be defined", "Name"); }

            //// Ensure the asset has a description of some sort
            //if (string.IsNullOrEmpty(asset.Description)) { throw new LogicalException("Description must be defined", "Description"); }

            // Ensure that the asset is associated with an asset type
            if (asset.AssetTypeId.CompareTo(new Guid()) == 0) { throw new LogicalException("Asset Type must be defined", "AssetTypeId"); }

            // Ensure that the member id of the asset's creator has been specified
            if (asset.CreatedBy.CompareTo(new Guid()) == 0) { throw new LogicalException("'Created By' cannot be null", "CreatedBy"); }

            // If the asset has been modified and it is not new, ensure that the asset's modifier has been specified
            if (((asset.IsDirty) && (!asset.IsNew)) && (!asset.LastModifiedBy.HasValue)) { throw new LogicalException("'Last Modified By' cannot be null", "LastModifiedBy"); }

            //// Ensure edit is allowed
            //if (!asset.AllowEdit) { throw new LogicalException("User permissions state you are not allowed to edit this asset."); }

            // Ensure deletion is allowed if applicable
            //if ((!asset.AllowDelete) && (asset.Deleted.HasValue)) { throw new LogicalException("User permissions state you are not allowed to delete this asset."); }

            // Ensure the specified AssetTypeId is valid
            if (!XObjectTypeService.Instance.ValidId(asset.AssetTypeId))
            {
                throw new LogicalException("Specified asset type id does not exist", "AssetTypeId");
            }

            //if (!XUserManager.Instance.ValidId(asset.CreatedBy))
            //{
            //    throw new LogicalException("Invalid user id", "CreatedBy");
            //}

        }

        public bool Save(Guid userId, XObject asset)
        {
            this.Validate(asset);
            return this.bizLayer.Save(asset);
        }

        public bool Create(Guid userId, Guid assetId, string assetName, string assetDisplayValue, Guid assetTypeId, Guid? parentId, string description)
        {

            if (this.bizLayer.Create(userId, assetId, assetName, assetDisplayValue, assetTypeId, parentId, description)) { return false; }

            XSubmittal submittal = new XSubmittal(assetId, assetName, userId);

            if (parentId.HasValue)
            {

                XValue pv = new XValue(Constants.XPropertyIds.AssetParent, assetId, parentId.Value.ToString(), userId);

                //if (isAutoApprove)
                //{
                pv.Approved = pv.Created;
                pv.ApprovedBy = userId;
                //}

                submittal.PropertyValues.Add(pv);

            }

            XValue pv2 = new XValue(XPropertyIds.AssetName, assetId, assetName, userId);
            //if (isAutoApprove)
            //{
            pv2.Approved = pv2.Created;
            pv2.ApprovedBy = userId;
            //}

            submittal.PropertyValues.Add(pv2);

            if (new XSubmittalLayer().Save(submittal, true, userId))
            {
                ThreadPool.QueueUserWorkItem(o => new SqlDatabaseLayer().InsertIntoGenTables(assetId, false, assetTypeId));
                return true;
            }

            return false;
        }

        public bool CreateInstance(Guid userId, Guid assetId, string assetName, Guid assetTypeId, Guid instanceOfId)
        {

            if (this.bizLayer.CreateInstance(userId, assetId, assetName, assetTypeId, instanceOfId))
            {

                XValue pv = new XValue(Constants.XPropertyIds.AssetName, assetId, assetName, userId);

                //if (isAutoApprove)
                //{
                pv.Approved = pv.Created;
                pv.ApprovedBy = pv.CreatedBy;
                //}

                XSubmittal submittal = new XSubmittal(assetId, assetName, userId);

                submittal.PropertyValues.Add(pv);

                if (new XSubmittalLayer().Save(submittal, true, userId))
                {
                    ThreadPool.QueueUserWorkItem(o => new SqlDatabaseLayer().InsertIntoGenTables(assetId, true, assetTypeId));
                    return true;
                }
            }

            return false;
        }

        public bool Delete(Guid objectId, Guid userId)
        {
            if (!this.bizLayer.Delete(objectId, userId)) { return false; }
            // now we need to remove the asset from any generated tables
            ThreadPool.QueueUserWorkItem(o => new XGenEngine().XObjectDeleteFromGenTables(objectId));
            return true;
        }

    }

}