
using System;
using System.Collections.Generic;

using XDB.DataObjects;
using XDB.Enumerations;
using XDB.DAL;

namespace XDB.BLL
{

    /// <summary>
    /// Primary entry point for working with <see cref="Picklist"/> instances.
    /// </summary>
    internal class XListLayer : XBaseLayer
    {

        private XListDal dal = new XListDal();
        private XListValueLayer _plvLayer = new XListValueLayer();

        public XListLayer() : base(ECommonObjectType.XList) { }

        //public PicklistLayer(EApplicationInstance target)
        //{
        //    string connString = Core.Config.SystemFrameworkHelper.DbConnStringByInstance(target);
        //    this.dal = new PicklistDal(connString);
        //    this._plvLayer = new PickListValueLayer(target);
        //}

        /// <summary>
        /// Gets an instance of a picklist matching the specified id
        /// </summary>
        /// <param name="id">id of the pickList to retrieve</param>
        /// <returns></returns>
        public XList Get(Guid id)
        {

            XList pickList = this.dal.Get(id);

            if (pickList == null) { return null; }

            //pickList.Values = this._plvLayer.GetCollection(pickList.Id);
            //pickList.Dependencies = this.PickListDependencies_Get(pickList.Id);
            pickList.IsNew = false;
            pickList.IsDirty = false;

            return pickList;
        }

        public bool Save(XList picklist, Guid userId)
        {            

            if (!this.dal.Save(picklist, userId)) { return false; }

            bool plvsSaved = this._plvLayer.Save(picklist.Values, userId);
            //bool depsSaved = this.PickListDependencyList_Save(picklist.Dependencies);

            return plvsSaved; // && depsSaved;
        }

        /// <summary>
        /// Deletes a specified PickList
        /// </summary>
        /// <param name="pickListId">Id of the PickList to delete</param>
        /// <param name="userId">Id of the user performing this action</param>
        /// <returns>true if successful; false otherwise</returns>
        public bool Delete(Guid pickListId, Guid userId)
        {
            if (this.dal.Delete(pickListId, userId))
            {
                return new XListValueLayer().PickListValueList_DeleteByPickListId(pickListId, userId);
            }
            return false;
        }

        public Guid GetIdByPropertyId(Guid propertyId)
        {
            return this.dal.GetIdByPropertyId(propertyId);
        }

        //public bool Migrate(Guid pickListId, EApplicationInstance target)
        //{
        //    Picklist pl = new PicklistLayer().Get(pickListId);

        //    if (pl == null) { return false; }

        //    pl.IsDirty = true;
        //    pl.IsNew = true;

        //    for (int i = 0; i < pl.Values.Count; i++)
        //    {
        //        pl.Values[i].IsDirty = true;
        //        pl.Values[i].IsNew = true;
        //    }

        //    return new PicklistLayer(target).Save(pl);
        //}

        public XList GetByPropertyId(Guid propertyId, bool includeDeleted, bool includeUnapproved)
        {
            // TODO: Determine how to remove incoming params since I removed them from the dal.Get() method
            Guid pickListId = this.GetIdByPropertyId(propertyId);
            if (pickListId != new Guid())
            {
                return this.dal.Get(pickListId);
            }
            return null;
        }

        public Dictionary<Guid, string> GetDictionary(bool includeDeleted)
        {
            return this.dal.GetDictionary(includeDeleted);
        }

        //public bool PickListDependencyList_Save(PickListDependencyList values)
        //{
        //    return this._dal.PickListDependencyList_Save(values);
        //}

        //public bool PickListDependency_Save(PickListDependency value)
        //{
        //    return this._dal.PickListDependency_Save(value);
        //}

        //public PickListDependencyList PickListDependencies_Get(Guid childPickListId)
        //{
        //    return this._dal.PickListDependencies_Get(childPickListId);
        //}

    }

}