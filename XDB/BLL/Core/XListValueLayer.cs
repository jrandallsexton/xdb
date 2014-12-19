
using System;
using System.Collections.Generic;

using XDB.DataObjects;
using XDB.Enumerations;
using XDB.Exceptions;
using XDB.DAL;

namespace XDB.BLL
{

    internal class XListValueLayer : XBaseLayer
    {

        private XListValueDal dal = new XListValueDal();

        public XListValueLayer() : base(ECommonObjectType.XListValue) { }

        //public PickListValueLayer(EApplicationInstance target)
        //{
        //    string connString = Core.Config.SystemFrameworkHelper.DbConnStringByInstance(target);
        //    this.dal = new PickListValueDal(connString);
        //}

        /// <summary>
        /// Gets an instance of a picklist value matching the specified id
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public XListValue Get(Guid Id)
        {
            return this.dal.Get(Id);
        }

        internal bool Save(List<XListValue> values, Guid userId)
        {
            this.Validate(values);
            return this.dal.Save(values, userId);
        }

        //public bool PickListValueList_Delete(PickListValueList picklistvalue, Guid userId)
        //{
        //    return this._dal.PickListValueList_Delete(picklistvalue, userId);
        //}

        public List<XListValue> GetCollection(Guid pickListId)
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

        internal bool PickListValueList_DeleteByPickListId(Guid pickListId, Guid userId)
        {
            return this.dal.PickListValueList_DeleteByPickListId(pickListId, userId);
        }

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

        public Dictionary<Guid, string> GetMatching(List<Guid> pickListValueIds, Guid pickListId)
        {
            return this.dal.GetMatching(pickListValueIds, pickListId);
        }

    }

}