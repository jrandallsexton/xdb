
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    public class XPropertyManager : XBaseApi
    {

        public XPropertyManager() : base(ECommonObjectType.XProperty) { }

        private XPropertyLayer bizLayer = new XPropertyLayer();

        public XProperty Get(Guid id)
        {
            return this.bizLayer.Get(id);
        }

        public bool Save(XProperty property, Guid userId)
        {
            //ESystemActionType actionType = (property.IsNew) ? ESystemActionType.Create : ESystemActionType.Update;
            //RulesEngine.Validate(property, actionType, userId);
            this.Validate(property);
            return this.bizLayer.Save(property, userId);
        }

        public bool Delete(Guid propertyId, Guid userId)
        {
            if (!new XUserManager().HasPermission(userId, ECommonObjectType.XProperty, ESystemActionType.Delete))
            {
                throw new LogicalException(ExceptionMessages.PERM_PROP_DEL);
            }
            return this.bizLayer.Delete(propertyId, userId);
        }

        private void Validate(XProperty property)
        {

            if (property.Id == Guid.Empty) { throw new LogicalException("Id cannot be null", "Id"); }

            if (property.DataType == EDataType.Undefined) { throw new LogicalException("DataType must be defined", "DataType"); }

            if ((property.DataType == EDataType.Asset) && (!property.AssetTypeId.HasValue))
            {
                throw new LogicalException("DataType is Asset, but the Asset Type was not specified", "AssetTypeId");
            }

            if ((property.AssetTypeId.HasValue) && (!property.AssetTypeIsInstance.HasValue))
            {
                string msg = "Must know whether the property is dealing with Asset Instances or Asset Definitions.  AssetTypeIsInstance not specified.";
                throw new LogicalException(msg, "AssetTypeIsInstance");
            }

            if (string.IsNullOrEmpty(property.Name)) { throw new LogicalException("Property name must be defined", "Name"); }

            if (string.IsNullOrEmpty(property.Description)) { throw new LogicalException("Description must be defined", "Description"); }

            if (property.CreatedBy == Guid.Empty) { throw new LogicalException("'Created By' cannot be null", "CreatedBy"); }

            if (XUserManager.Instance.ValidId(property.CreatedBy) == false)
            {
                throw new LogicalException("Invalid user id", "CreatedBy");
            }

            //if (property.IsSystem && property.Deleted.HasValue)
            //{
            //    throw new LogicalException("System properties cannot be deleted");
            //}

            //if ((property.IsDirty) && (property.LastModifiedBy == new Guid())) { throw new LogicalException("'Last Modified By' cannot be null", "LastModifiedBy"); }

        }

        public Dictionary<Guid, XProperty> GetObjectDictionary(List<Guid> propertyIds)
        {
            return this.bizLayer.GetObjectDictionary(propertyIds);
        }

        //public Dictionary<Guid, T> GetObjectDictionary<T>(List<Guid> ids) 
        //{
        //    return this.bizLayer.GetObjectDictionary(ids);
        //}

    }

}