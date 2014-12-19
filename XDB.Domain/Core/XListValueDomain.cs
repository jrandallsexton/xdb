
using System;
using System.Collections.Generic;

using XDB.Models;
using XDB.Common;
using XDB.Common.Enumerations;
using XDB.Common.Exceptions;
using XDB.Common.Interfaces;
using XDB.Repositories;

namespace XDB.Domains
{

    public class XListValueDomain : XBaseDomain
    {

        //public class XListValueDomain : XBaseDomain
        //public class XListValueRepository<T> : IXListValueRepository<T> where T : XBase, IXListValue

        private IXListValueRepository<XListValue> dal = new XListValueRepository<XListValue>();

        public XListValueDomain() : base(ECommonObjectType.XListValue) { }

        /// <summary>
        /// Gets an instance of a picklist value matching the specified id
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public XListValue Get(Guid Id)
        {
            return this.dal.Get(Id);
        }

        public void Save(List<XListValue> values, Guid userId)
        {
            this.Validate(values);
            this.dal.Save(values, userId);
        }

        //public bool PickListValueList_Delete(PickListValueList picklistvalue, Guid userId)
        //{
        //    return this._dal.PickListValueList_Delete(picklistvalue, userId);
        //}

        public IList<XListValue> GetCollection(Guid pickListId)
        {
            return this.dal.GetCollection(pickListId);
        }

        //public Dictionary<Guid, string> GetDictionary(Guid pickListId)
        //{
        //    return this.dal.GetDictionary(pickListId);
        //}

        //public Dictionary<Guid, string> PickListValues_Get(Guid pickListId, Guid parentPickListId, Guid parentPickListValueId)
        //{
        //    return this._dal.PickListValues_Get(pickListId, parentPickListId, parentPickListValueId);
        //}

        //internal void PickListValueList_DeleteByPickListId(Guid pickListId, Guid userId)
        //{
        //    this.dal.PickListValueList_DeleteByPickListId(pickListId, userId);
        //}

        private void Validate(XListValue value)
        {
            // Ensure a PK is defined
            if (value.Id.CompareTo(new Guid()) == 0) { throw new LogicalException("Id cannot be null", "Id"); }

            // Ensure the value belongs to a picklist
            if (value.PickListId.CompareTo(new Guid()) == 0) { throw new LogicalException("PickListId must be defined", "PickListId"); }

            //// Ensure the selected picklist is valid
            //if (new PickListLayer().IsValidId(value.PickListId) == false) { throw new LogicalException("Invalid picklist", "PickListId"); }

            // Ensure the value has a value
            if (string.IsNullOrEmpty(value.Value)) { throw new LogicalException("Value must be defined", "Value"); }

            // Ensure that the member id of the asset's creator has been specified
            if (value.CreatedBy.CompareTo(new Guid()) == 0) { throw new LogicalException("'Created By' cannot be null", "CreatedBy"); }

            //// Ensure that the creator is valid
            //if (new MemberLayer().IsValidId(value.CreatedBy) == false) { throw new LogicalException("Invalid user id", "CreatedBy"); }

            // If the asset has been modified and it is not new, ensure that the asset's modifier has been specified
            if (((value.IsDirty) && (!value.IsNew)) && (!value.LastModifiedBy.HasValue)) { throw new LogicalException("'Last Modified By' cannot be null", "LastModifiedBy"); }

        }

        private void Validate(List<XListValue> values)
        {
            foreach (XListValue plv in values)
            {
                this.Validate(plv);
            }
        }

        public Guid GetIdByValue(Guid pickListId, string value)
        {
            return this.dal.GetIdByValue(pickListId, value);
        }

        public Guid GetIdByDisplayValue(Guid pickListId, string displayValue)
        {
            return this.dal.GetIdByDisplayValue(pickListId, displayValue);
        }

        public IDictionary<Guid, string> GetMatching(List<Guid> pickListValueIds, Guid pickListId)
        {
            return this.dal.GetMatching(pickListValueIds, pickListId);
        }

    }

}