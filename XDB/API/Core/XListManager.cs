
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

    public class XListManager : XBaseApi
    {

        public XListManager() : base(ECommonObjectType.XList) { }

        private XListLayer bizLayer = new XListLayer();

        public XList Get(Guid id)
        {
            return this.bizLayer.Get(id);
        }

        public bool Save(Guid userId, XList picklist)
        {
            this.Validate(picklist);
            return this.bizLayer.Save(picklist, userId);
        }

        public bool Delete(Guid userId, Guid id)
        {
            // TODO: Ensure user has permissions to delete a picklist
            return this.bizLayer.Delete(id, userId);
        }

        private void Validate(XList picklist)
        {

            // Ensure a PK is defined
            if (picklist.Id.CompareTo(new Guid()) == 0) { throw new LogicalException("Id cannot be null", "Id"); }

            // Ensure the object has a name
            if (string.IsNullOrEmpty(picklist.Name)) { throw new LogicalException("Name must be defined", "Name"); }

            // Ensure the object has a description of some sort
            if (string.IsNullOrEmpty(picklist.Description)) { throw new LogicalException("Description must be defined", "Description"); }

            // Ensure that the member id of the asset's creator has been specified
            if (picklist.CreatedBy.CompareTo(new Guid()) == 0) { throw new LogicalException("'Created By' cannot be null", "CreatedBy"); }

            // Ensure that the creator is valid
            if (picklist.IsNew && (!XUserManager.Instance.ValidId(picklist.CreatedBy)))
            {
                throw new LogicalException("Invalid user id", "CreatedBy");
            }

            // If the asset has been modified and it is not new, ensure that the asset's modifier has been specified
            if ((picklist.IsDirty) && (!picklist.IsNew))
            {
                if (!picklist.LastModifiedBy.HasValue) { throw new LogicalException("'Last Modified By' cannot be null", "LastModifiedBy"); }
                if (!XUserManager.Instance.ValidId(picklist.LastModifiedBy.Value)) { throw new LogicalException("Invalid user id", "LastModifiedBy"); }
            }

            // If the picklist doesn't contain any values ...
            //if ((picklist.PickListValues == null) || (picklist.PickListValues.Count == 0)) { throw new LogicalException("A picklist must have at least one value defined.", "PickListValues"); }

        }

    }

}