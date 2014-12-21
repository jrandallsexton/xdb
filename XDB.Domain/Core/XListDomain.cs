
using System;
using System.Collections.Generic;

using XDB.Models;
using XDB.Common;
using XDB.Common.Enumerations;
using XDB.Common.Interfaces;
using XDB.Repositories;

namespace XDB.Domains
{

    /// <summary>
    /// Primary entry point for working with <see cref="XList"/> instances.
    /// </summary>
    public class XListDomain<T> : XBaseDomain, IXListDomain<T> where T : XBase, IXList
    {

        private IXListRepository<T> _repo = new XListRepo<T>();

        public XListDomain() : base(ECommonObjectType.XList) { }

        public XListDomain(IXListRepository<T> repo) : base(ECommonObjectType.XList)
        {
            this._repo = repo;
        }

        /// <summary>
        /// Gets an instance of a XList matching the specified id
        /// </summary>
        /// <param name="id">id of the XList to retrieve</param>
        /// <returns></returns>
        public IXList Get(Guid id)
        {
            return this._repo.Get(id);
        }

        public void Save(T list)
        {
            this._repo.Save(list);
            //bool plvsSaved = this._plvLayer.Save(picklist.Values, userId);
            //bool depsSaved = this.PickListDependencyList_Save(picklist.Dependencies);
        }

        /// <summary>
        /// Deletes a specified XList
        /// </summary>
        /// <param name="pickListId">Id of the PickList to delete</param>
        /// <param name="userId">Id of the user performing this action</param>
        /// <returns>true if successful; false otherwise</returns>
        public void Delete(Guid listId, Guid userId)
        {
            this._repo.Delete(listId, userId);
            //return new XListValueLayer().PickListValueList_DeleteByPickListId(listId, userId);
        }

        //public Guid GetIdByPropertyId(Guid propertyId)
        //{
        //    return this.dal.GetIdByPropertyId(propertyId);
        //}

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

        //public XList GetByPropertyId(Guid propertyId, bool includeDeleted, bool includeUnapproved)
        //{
        //    // TODO: Determine how to remove incoming params since I removed them from the dal.Get() method
        //    Guid pickListId = this.GetIdByPropertyId(propertyId);
        //    if (pickListId != new Guid())
        //    {
        //        return this.dal.Get(pickListId);
        //    }
        //    return null;
        //}

        public IDictionary<Guid, string> GetDictionary(bool includeDeleted)
        {
            return this._repo.GetDictionary(includeDeleted);
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