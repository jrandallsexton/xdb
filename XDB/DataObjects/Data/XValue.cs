
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDB.DataObjects
{

    //<summary>
    //</summary>
    public class XValue : XBase
    {

        private Guid _propertyId;
        private Guid _assetId;
        private Guid? _submittalGroupId;
        private string _value = string.Empty;
        private int? _index;
        private DateTime? _rejected;
        private Guid? _rejectedBy;

        /// <summary>
        /// Id of the property to which this value belongs; FK into Properties
        /// <summary>
        public Guid PropertyId
        {
            get { return this._propertyId; }
            set
            {
                if (this._propertyId.CompareTo(value) != 0)
                {
                    this._propertyId = value;
                    this.IsDirty = true;
                }
            }
        }

        public string Property { get; set; }

        /// <summary>
        /// Id of the asset to which this property value belongs; FK into Assets
        /// <summary>
        public Guid AssetId
        {
            get { return this._assetId; }
            set
            {
                if (this._assetId.CompareTo(value) != 0)
                {
                    this._assetId = value;
                    this.IsDirty = true;
                }
            }
        }

        public Guid? SubmittalGroupId
        {
            get { return this._submittalGroupId; }
            set
            {
                if ((this._submittalGroupId.HasValue) && (this._submittalGroupId.Value.CompareTo(value) == 0))
                {
                    return;
                }
                else
                {
                    this._submittalGroupId = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Actual value
        /// <summary>
        public string Value
        {
            get { return this._value; }
            set
            {
                if (this._value != value)
                {
                    this._value = value;
                    this.IsDirty = true;
                }
            }
        }

        public string DisplayValueHtml { get; set; }

        /// <summary>
        /// Numerical order of this value; applicable only for properties that allow multiple values
        /// <summary>
        public int? Index
        {
            get { return this._index; }
            set
            {
                if (this._index != value)
                {
                    this._index = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Rejected
        /// <summary>
        public DateTime? Rejected
        {
            get { return this._rejected; }
            set
            {
                if (this._rejected != value)
                {
                    this._rejected = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// RejectedBy
        /// <summary>
        public Guid? RejectedBy
        {
            get { return this._rejectedBy; }
            set
            {
                if ((this._rejectedBy.HasValue) && (this._rejectedBy.Value.CompareTo(value) == 0))
                {
                    return;
                }
                else
                {
                    this._rejectedBy = value;
                    this.IsDirty = true;
                }
            }
        }

        //public PropertyValue Clone(Guid newSubmittalId, Guid userId)
        //{
        //    PropertyValue pv = new PropertyValue(this.PropertyId, this.AssetId, newSubmittalId, this.Value, userId);
        //    if (this.Order.HasValue) { pv.Order = this.Order.Value; }
        //    pv.Created = this.Created;
        //    pv.CreatedBy = this.CreatedBy;
        //    if (this.Approved.HasValue) { pv.Approved = this.Approved.Value; }
        //    if (this.ApprovedBy.HasValue) { pv.ApprovedBy = this.ApprovedBy.Value; }
        //    if (this.Deleted.HasValue) { pv.Deleted = this.Deleted.Value; }
        //    if (this.DeletedBy.HasValue) { pv.DeletedBy = this.DeletedBy.Value; }
        //    return pv;
        //}

        public XValue()
        {
            this.Id = System.Guid.NewGuid();
            this.Created = DateTime.Now;
            this.IsNew = true;
            this.IsDirty = true;
        }

        public XValue(Guid propertyId, Guid assetId, string value, Guid userId)
            : this()
        {
            this.PropertyId = propertyId;
            this.AssetId = assetId;
            this.Value = value;
            this.CreatedBy = userId;
        }

        public XValue(Guid propertyId, Guid assetId, Guid submittalGroupId, string value, Guid userId)
            : this()
        {
            this.PropertyId = propertyId;
            this.AssetId = assetId;
            this.SubmittalGroupId = submittalGroupId;
            this.Value = value;
            this.CreatedBy = userId;
        }

        public XValue(Guid propertyId, Guid assetId, string value, Guid submittalId, Guid userId)
            : this()
        {
            this.AssetId = assetId;
            this.CreatedBy = userId;
            this.PropertyId = propertyId;
            this.Value = value;
            this.SubmittalGroupId = submittalId;
        }

    }

}