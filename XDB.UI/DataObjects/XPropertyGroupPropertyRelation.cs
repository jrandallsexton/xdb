
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.DataObjects;

namespace XDB.UI.DataObjects
{

    public class XPropertyGroupPropertyRelation : XBase
    {

        private Guid _propertyGroupId;
        private Guid _propertyId;
        private int _order = 0;
        private bool _isRequired = false;
        private bool _isReadOnly = false;
        private string _defaultValue = string.Empty;

        /// <summary>
        /// Id of the property group to which this record belongs; FK into PropertyGroups
        /// <summary>
        public Guid PropertyGroupId
        {
            get { return this._propertyGroupId; }
            set
            {
                if (this._propertyGroupId.CompareTo(value) != 0)
                {
                    this._propertyGroupId = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Id of the property to which this record belongs; FK into Properties
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

        public int Index
        {
            get { return this._order; }
            set
            {
                if (this._order != value)
                {
                    this._order = value;
                    this.IsDirty = true;
                }
            }
        }

        public bool IsRequired
        {
            get { return this._isRequired; }
            set
            {
                if (this._isRequired != value)
                {
                    this._isRequired = value;
                    this.IsDirty = true;
                }
            }
        }

        public bool IsReadOnly
        {
            get { return this._isReadOnly; }
            set
            {
                if (this._isReadOnly != value)
                {
                    this._isReadOnly = value;
                    this.IsDirty = true;
                }
            }
        }

        public string DefaultValue
        {
            get { return this._defaultValue; }
            set
            {
                if (this._defaultValue != value)
                {
                    this._defaultValue = value;
                    this.IsDirty = true;
                }
            }
        }

        public XPropertyGroupPropertyRelation() { }

        public XPropertyGroupPropertyRelation(Guid propertyGroupId, Guid propertyId, int idx, bool isRequired, Guid createdBy)
        {
            this.PropertyGroupId = propertyGroupId;
            this.PropertyId = propertyId;
            this.Index = idx;
            this.IsRequired = isRequired;
            this.CreatedBy = createdBy;
        }

    }

}