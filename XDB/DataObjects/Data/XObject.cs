
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDB.DataObjects
{

    /// <summary>
    /// Generic object for all assets
    /// Corresponding class objects are found in the database table: [Assets]
    /// </summary>
    public class XObject : XBase
    {

        private Guid _assetTypeId = Guid.NewGuid();
        private Guid? _instanceOfId;
        //private List<XObjectRelation> _assetMembers = new List<AssetRelation>();

        public XObject() { }

        public XObject(Guid assetId, string assetName, Guid assetTypeId, Guid? instanceOfId, Guid userId)
        {
            this.Id = assetId;
            this.CreatedBy = userId;
            this.Approved = this.Created;
            this.ApprovedBy = this.CreatedBy;
            this.Name = assetName;
            this.DisplayValue = string.Empty;
            this.AssetTypeId = assetTypeId;
            //this.InstanceOfId = instanceOfId.HasValue ? instanceOfId.Value : null; // really odd that this doesn't work
            if (instanceOfId.HasValue)
            {
                this.InstanceOfId = instanceOfId.Value;
            }
            this.IsDirty = true;
        }

        /// <summary>
        /// Id of the asset's type; FK into [AssetTypes]
        /// </summary>
        public Guid AssetTypeId
        {
            get { return this._assetTypeId; }
            set
            {
                if (this._assetTypeId.CompareTo(value) != 0)
                {
                    this._assetTypeId = value;
                    this.IsDirty = true;
                }
            }
        }

        public bool IsInstance
        {
            get { return ((this._instanceOfId.HasValue) && (this._instanceOfId.Value != Guid.Empty)); }
        }

        /// <summary>
        /// If this asset is an intance, the id of the asset it is an instance of
        /// </summary>
        public Guid? InstanceOfId
        {
            get { return this._instanceOfId; }
            set
            {
                if ((this._instanceOfId.HasValue) && (this._instanceOfId.Value.CompareTo(value) == 0))
                {
                    return;
                }
                else
                {
                    this._instanceOfId = value;
                    this.IsDirty = true;
                }
            }
        }

        //public List<AssetRelation> AssetMembers
        //{
        //    get { return this._assetMembers; }
        //}

        ///// <summary>
        ///// Whether or not a user (within a user's context) is allowed to edit this asset
        ///// This value is set dynamically by the API when an asset is requested
        ///// </summary>
        //public bool AllowEdit { get; set; }

        ///// <summary>
        ///// Whether or not a user (within a user's context) is allowed to delete this asset
        ///// This value is set dynamically by the API when an asset is requested
        ///// </summary>
        //public bool AllowDelete { get; set; }

        //public void AddAssetMember(AssetAssetRelation relation)
        //{
        //    if (this._assetMembers == null) { this._assetMembers = new AssetAssetRelationList(); }
        //    this._assetMembers.Add(relation);
        //    this.IsDirty = true;
        //}

        //public void AddAssetMember(Guid assetId, Guid userId, EAssetRelationType relationType)
        //{
        //    if (this._assetMembers == null) { this._assetMembers = new AssetAssetRelationList(); }
        //    if (this._assetMembers.Contains(assetId)) { return; }
        //    AssetAssetRelation relation = new AssetAssetRelation();
        //    relation.FromAssetId = this.Id;
        //    relation.ToAssetId = assetId;
        //    relation.AssetRelationType = relationType;
        //    relation.Created = DateTime.Now;
        //    relation.CreatedBy = userId;
        //    this._assetMembers.Add(relation);
        //    this.IsDirty = true;
        //    this.LastModified = DateTime.Now;
        //    this.LastModifiedBy = userId;
        //}

        //public void RemoveAssetMember(Guid assetId, Guid userId)
        //{
        //    if (this._assetMembers == null) { this._assetMembers = new AssetAssetRelationList(); }
        //    foreach (AssetAssetRelation relation in this._assetMembers)
        //    {
        //        if (relation.ToAssetId.CompareTo(assetId) == 0)
        //        {
        //            relation.DeletedBy = userId;
        //            break;
        //        }
        //    }
        //}

    }

}