
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDB.DataObjects
{

    public abstract class XBase
    {

        private Guid _id = Guid.NewGuid();

        private string _name = string.Empty;
        private string _displayValue = string.Empty;
        private string _desc = string.Empty;

        private string _singular = string.Empty;
        private string _plural = string.Empty;

        private DateTime _created = DateTime.Now;
        private Guid _createdBy;

        private DateTime? _approved;
        private Guid? _approvedBy;

        private DateTime? _lastModified;
        private Guid? _lastModifiedBy;

        private DateTime? _deleted;
        private Guid? _deletedBy;

        private bool _isNew = true;
        private bool _isDirty = true;

        private bool _isSystem = false;

        public XBase() { }

        /// <summary>
        /// PK Id of the object
        /// <summary>
        public Guid Id
        {
            get { return this._id; }
            set
            {
                if (this._id.CompareTo(value) == 0) { return; }
                this._id = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Name of the object
        /// <summary>
        public string Name
        {
            get { return this._name; }
            set
            {
                if (this._name.Equals(value, StringComparison.Ordinal)) { return; }
                this._name = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Name of the property as it should appear in UI instances
        /// <summary>
        public string DisplayValue
        {
            get { return this._displayValue; }
            set
            {
                if (this._displayValue.Equals(value, StringComparison.Ordinal)) { return; }
                this._displayValue = value;
                this.IsDirty = true;
            }
        }

        public string Description
        {
            get { return this._desc; }
            set
            {
                if (this._desc.Equals(value, StringComparison.Ordinal)) { return; }
                this._desc = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// When the record was created
        /// <summary>
        public DateTime Created
        {
            get { return this._created; }
            set
            {
                if (this._created.CompareTo(value) == 0) { return; }
                this._created = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Id of the user who created the record; FK into [Members]
        /// <summary>
        public Guid CreatedBy
        {
            get { return this._createdBy; }
            set
            {
                if (this._createdBy.CompareTo(value) == 0) { return; }
                this._createdBy = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// When the object/record was approved (if applicable)
        /// <summary>
        public DateTime? Approved
        {
            get { return this._approved; }
            set
            {
                if (this._approved == value) { return; }
                this._approved = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Id of the user who approved the object/record (if applicable); FK into [Members]
        /// <summary>
        public Guid? ApprovedBy
        {
            get { return this._approvedBy; }
            set
            {
                if ((this._approvedBy.HasValue) && (this._approvedBy.Value.CompareTo(value) == 0))
                {
                    return;
                }
                else
                {
                    this._approvedBy = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// When the record/object was last modified
        /// <summary>
        public DateTime? LastModified
        {
            get { return this._lastModified; }
            set
            {
                if (this._lastModified == value) { return; }
                this._lastModified = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Id of the user who last modified the object/record (if applicable); FK into [Members]
        /// <summary>
        public Guid? LastModifiedBy
        {
            get { return this._lastModifiedBy; }
            set
            {
                if ((this._lastModifiedBy.HasValue) && (this._lastModifiedBy.Value.CompareTo(value) == 0))
                {
                    return;
                }
                else
                {
                    this._lastModifiedBy = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// When the record/object was deleted
        /// <summary>
        public DateTime? Deleted
        {
            get { return this._deleted; }
            set
            {
                if ((this._deleted.HasValue) && (this._deleted.Value.CompareTo(value) == 0))
                {
                    return;
                }
                else
                {
                    this._deleted = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Id of the user who deleted the object/record (if applicable); FK into [Members]
        /// <summary>
        public Guid? DeletedBy
        {
            get { return this._deletedBy; }
            set
            {
                if ((this._deletedBy.HasValue) && (this._deletedBy.Value.CompareTo(value) == 0))
                {
                    return;
                }
                else
                {
                    this._deletedBy = value;
                    this.IsDirty = true;
                }
            }
        }

        public bool IsDirty
        {
            get { return this._isDirty; }
            set
            {
                if (this._isDirty == value) { return; }
                this._isDirty = value;
            }

        }

        public bool IsNew
        {
            get { return this._isNew; }
            set { this._isNew = value; }
        }

        public bool IsSystem
        {
            get { return this._isSystem; }
            set
            {
                if (this._isSystem != value)
                {
                    this._isSystem = value;
                    this.IsDirty = true;
                }
            }
        }

        public string Singular
        {
            get { return this._singular; }
            set
            {
                if (this._singular.Equals(value, StringComparison.Ordinal)) { return; }
                this._singular = value;
                this.IsDirty = true;
            }
        }

        public string Plural
        {
            get { return this._plural; }
            set
            {
                if (this._plural.Equals(value, StringComparison.Ordinal)) { return; }
                this._plural = value;
                this.IsDirty = true;
            }
        }

    }

}