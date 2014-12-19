
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.BLL;
using XDB.DataObjects;
using XDB.Enumerations;
using XDB.Exceptions;
using XDB.Extensions;
using XDB.Interfaces;

namespace XDB.API
{

    public class XObjectTypeManager : XBaseApi
    {

        public XObjectTypeManager() : base(ECommonObjectType.XObjectType) { }

        private static XObjectTypeManager instance;

        public static XObjectTypeManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new XObjectTypeManager();
                }
                return instance;
            }
        }

        XObjectTypeLayer bizLayer = new XObjectTypeLayer();

        public bool Save(XObjectType objectType, Guid userId)
        {
            this.Validate(objectType);
            return this.bizLayer.Save(objectType, userId);
            // && (this.CreateGeneratedObjects(assetType)));
        }

        private void Validate(XObjectType assetType)
        {

            if (string.IsNullOrEmpty(assetType.Name)) { throw new LogicalException("Name must be defined", "Name"); }

            //if (string.IsNullOrEmpty(assetType.Pluralization)) { throw new LogicalException("Pluralization must be defined.", "Pluralization"); }

            if ((assetType.ParentId.HasValue) && (assetType.ParentId.Value.CompareTo(assetType.Id) == 0))
            {
                throw new LogicalException("ParentId cannot be itself.");
            }

            if (assetType.CreatedBy == Guid.Empty) { throw new LogicalException("CreatedBy must be defined", "CreatedBy"); }
            if (!XUserManager.Instance.ValidId(assetType.CreatedBy))
            {
                throw new LogicalException("Invalid user id", "CreatedBy");
            }

            //if ((assetType.Id.CompareTo(Constants.AssetTypeIds.User) == 0) && (assetType.Deleted.HasValue || assetType.DeletedBy.HasValue))
            //{
            //    throw new LogicalException("Cannot delete AssetType=User");
            //}

        }

    }

}