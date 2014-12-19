
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Common;
using XDB.Common.Interfaces;

namespace XDB.Models
{

    ///<summary>
    ///Class representation of asset types which define what every asset in the system is
    ///examples: Desktop, Laptop, Contract, Address, User, etc.
    ///Corresponding class objects are found in the database table: AssetTypes
    ///</summary>
    public class XObjectType : XBase, IXObjectType
    {

        private Guid? _parentId = null;
        //private AssetTypePropertyRelationList _propertyRelations = new AssetTypePropertyRelationList();
        private bool _allowAssets = true;
        private string _defLbl = string.Empty;
        private string _defLblP = string.Empty;
        private string _insLbl = string.Empty;
        private string _insLblP = string.Empty;

        public XObjectType() { }

        public XObjectType(string name, string pluralization, Guid userId)
        {
            this.Name = name;
            this.Plural = pluralization;
            this.CreatedBy = userId;
            this.IsDirty = true;
            this.IsNew = true;
        }

        /// <summary>
        /// Id, if applicable, to the parent asset type
        /// </summary>
        public Guid? ParentId
        {
            get { return this._parentId; }
            set
            {
                if ((this._parentId.HasValue) && (this._parentId.Value.CompareTo(value) == 0))
                {
                    return;
                }
                else
                {
                    this._parentId = value;
                    this.IsDirty = true;
                }
            }
        }

        public string DefinitionLabel
        {
            get { return this._defLbl; }
            set
            {
                if (this._defLbl.Equals(value, StringComparison.Ordinal)) { return; }
                this._defLbl = value;
                this.IsDirty = true;
            }
        }

        public string DefinitionLabelPlural
        {
            get { return this._defLblP; }
            set
            {
                if (this._defLblP.Equals(value, StringComparison.Ordinal)) { return; }
                this._defLblP = value;
                this.IsDirty = true;
            }
        }

        public string InstanceLabel
        {
            get { return this._insLbl; }
            set
            {
                if (this._insLbl.Equals(value, StringComparison.Ordinal)) { return; }
                this._insLbl = value;
                this.IsDirty = true;
            }
        }

        public string InstanceLabelPlural
        {
            get { return this._insLblP; }
            set
            {
                if (this._insLblP.Equals(value, StringComparison.Ordinal)) { return; }
                this._insLblP = value;
                this.IsDirty = true;
            }
        }

        //public AssetTypePropertyRelationList Properties
        //{
        //    get
        //    {
        //        if (this._propertyRelations == null)
        //        {
        //            this._propertyRelations = new AssetTypePropertyRelationList();
        //        }
        //        return this._propertyRelations;
        //    }
        //}

        public bool AllowAssets
        {
            get { return this._allowAssets; }
            set
            {
                if (this._allowAssets == value) { return; }
                this._allowAssets = value;
                this.IsDirty = true;
            }
        }

        //public void AddPropertyRelation(Guid propertyId, EAssetTypeClass typeClass, Guid userId)
        //{

        //    if (this._propertyRelations.Contains(propertyId, typeClass)) { return; }

        //    AssetTypePropertyRelation relation = new AssetTypePropertyRelation();
        //    relation.PropertyId = propertyId;
        //    relation.IsInstance = (typeClass == EAssetTypeClass.Instance);
        //    relation.AssetTypeId = this._id;
        //    relation.CreatedBy = userId;
        //    relation.Created = DateTime.Now;
        //    relation.IsDirty = true;
        //    relation.IsNew = true;
        //    this._propertyRelations.Add(relation);

        //    this.IsDirty = true;
        //}

        //public void RemovePropertyMember(Guid propertyId, EAssetTypeClass typeClass, Guid userId)
        //{
        //    foreach (AssetTypePropertyRelation relation in this._propertyRelations)
        //    {
        //        if ((relation.PropertyId.CompareTo(propertyId) == 0) && (relation.IsInstance == (typeClass == EAssetTypeClass.Instance)))
        //        {
        //            relation.Deleted = DateTime.Now;
        //            relation.DeletedBy = userId;
        //            relation.IsDirty = true;
        //            this.IsDirty = true;
        //            break;
        //        }
        //    }
        //}

    }

}