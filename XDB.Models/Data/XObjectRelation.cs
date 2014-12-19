
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Common;
using XDB.Common.Enumerations;
using XDB.Common.Interfaces;

namespace XDB.Models
{

    //<summary>
    //</summary>
    public class XObjectRelation : XBase, IXObjectRelation
    {

        private Guid _fromAssetId = Guid.NewGuid();
        private Guid _toAssetId = Guid.NewGuid();
        private EObjectRelationType _assetRelationType = EObjectRelationType.Undefined;

        public XObjectRelation() : base() { }

        public XObjectRelation(Guid fromAssetId, Guid toAssetId, EObjectRelationType relationType, Guid userId)
        {
            this.FromAssetId = fromAssetId;
            this.ToAssetId = toAssetId;
            this.AssetRelationType = relationType;
            this.CreatedBy = userId;
            this.IsNew = true;
            this.IsDirty = true;
        }

        /// <summary>
        /// Id of the first (left-hand) asset within the logical statement (is-a statement); FK into Assets
        /// <summary>
        public Guid FromAssetId
        {
            get { return this._fromAssetId; }
            set
            {
                if (this._fromAssetId.CompareTo(value) != 0)
                {
                    this._fromAssetId = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Id of the second (right-hand) asset within the logical statement (is-a statement); FK into Assets
        /// <summary>
        public Guid ToAssetId
        {
            get { return this._toAssetId; }
            set
            {
                if (this._toAssetId.CompareTo(value) != 0)
                {
                    this._toAssetId = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Id of the asset's relation type; FK into AssetRelationTypes
        /// <summary>
        public EObjectRelationType AssetRelationType
        {
            get { return this._assetRelationType; }
            set
            {
                if (this._assetRelationType != value)
                {
                    this._assetRelationType = value;
                    this.IsDirty = true;
                }
            }
        }

    }

}