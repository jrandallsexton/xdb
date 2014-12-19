
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Domains;
using XDB.Models;
using XDB.Common;
using XDB.Common.Enumerations;
using XDB.Common.Exceptions;
using XDB.Common.Interfaces;
using XDB.Interfaces;

using XDB.Common.Interfaces;

namespace XDB.API
{

    public class XListService : XBaseService, IXListService
    {

        public XListService() : base(ECommonObjectType.XList)
        {

        }

        public XListService(IXListDomain<XList> bll) : base(ECommonObjectType.XList)
        {
            this._bizLayer = bll;
        }

        private IXListDomain<XList> _bizLayer = null;

        public IXList Get(Guid id)
        {
            return this._bizLayer.Get(id);
        }

        public void Save(Guid userId, XList xList)
        {
            this.Validate(xList);
            this._bizLayer.Save(xList);
        }

        public void Delete(Guid userId, Guid id)
        {
            // TODO: Ensure user has permissions to delete a picklist
            this._bizLayer.Delete(id, userId);
        }

        private void Validate(XList list)
        {

            // Ensure a PK is defined
            if (list.Id.CompareTo(new Guid()) == 0) { throw new LogicalException("Id cannot be null", "Id"); }

            // Ensure the object has a name
            if (string.IsNullOrEmpty(list.Name)) { throw new LogicalException("Name must be defined", "Name"); }

            // Ensure the object has a description of some sort
            if (string.IsNullOrEmpty(list.Description)) { throw new LogicalException("Description must be defined", "Description"); }

            // Ensure that the member id of the asset's creator has been specified
            if (list.CreatedBy.CompareTo(new Guid()) == 0) { throw new LogicalException("'Created By' cannot be null", "CreatedBy"); }

            //// Ensure that the creator is valid
            //if (list.IsNew && (!XUserManager.Instance.ValidId(list.CreatedBy)))
            //{
            //    throw new LogicalException("Invalid user id", "CreatedBy");
            //}

            // If the asset has been modified and it is not new, ensure that the asset's modifier has been specified
            if ((list.IsDirty) && (!list.IsNew))
            {
                if (!list.LastModifiedBy.HasValue) { throw new LogicalException("'Last Modified By' cannot be null", "LastModifiedBy"); }
                //if (!this._userManger.ValidId(list.LastModifiedBy.Value)) { throw new LogicalException("Invalid user id", "LastModifiedBy"); }
            }

            // If the picklist doesn't contain any values ...
            //if ((picklist.PickListValues == null) || (picklist.PickListValues.Count == 0)) { throw new LogicalException("A picklist must have at least one value defined.", "PickListValues"); }

        }

    }

}