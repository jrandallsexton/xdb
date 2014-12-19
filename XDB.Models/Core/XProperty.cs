
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Common.Enumerations;
using XDB.Common.Interfaces;

using XDB.Common;

namespace XDB.Models
{

    public class XProperty : XBase, IXProperty
    {

        #region private members

        private EDataType _dataType = EDataType.Undefined;
        private ESystemType _systemType = ESystemType.NotApplicable;
        private Int16? _precision = null;
        private Guid? _pickListId = null;
        private Guid? _dependentPropertyId = null; // used only in UI rendering
        private bool _allowMultiValue = false;
        private bool _allowNewValues = true;
        private bool _isInherited = false;
        private bool _isInheritedValue = false; // used to determine if value should come from parent item; if true, then property should be readOnly also
        private bool _isInstance = false; // used to determine if the value should come from the asset referenced by the InstanceOfId value
        private bool _isOrdered = false;
        private bool _isRequired = false; // used only within the context of a PropertyGroup-Property relation
        private bool _isReadOnly = false; // used only within the context of a PropertyGroup-Property relation
        private string _defaultValue = string.Empty; // used only within the context of a PropertyGroup-Property relation
        private bool _isRelationship = false;
        private Guid? _assetTypeId;
        private bool? _assetTypeIsInstance = null;
        private Guid? _roleId = null;

        #endregion

        #region public properties

        /// <summary>
        /// Id of the data type for this property; FK into DataTypes
        /// </summary>
        public EDataType DataType
        {
            get { return this._dataType; }
            set
            {
                if (this._dataType == value) { return; }
                this._dataType = value;
                this.IsDirty = true;
            }
        }

        public ESystemType SystemType
        {
            get { return this._systemType; }
            set
            {
                if (this._systemType == value) { return; }
                this._systemType = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Precision
        /// <summary>
        public Int16? Precision
        {
            get { return this._precision; }
            set
            {
                if (this._precision != value)
                {
                    this._precision = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Id of the picklist that this property uses; if null, a picklist is not used
        /// <summary>
        public Guid? PickListId
        {
            get { return this._pickListId; }
            set
            {
                if ((this._pickListId.HasValue) && (this._pickListId.Value.CompareTo(value) == 0))
                {
                    return;
                }
                else
                {
                    this._pickListId = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Whether or not multiple values can be entered/selected for this property
        /// <summary>
        public bool AllowMultiValue
        {
            get { return this._allowMultiValue; }
            set
            {
                if (this._allowMultiValue == value) { return; }
                this._allowMultiValue = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// This property's value is set during runtime prior to being returned to the client ui.
        /// It is used by the client ui to determine whether or not the current user
        /// has the required permissions to create a new value for this property.
        /// ex.  If this property is of assetType=Asset, the user must have permissions to create
        /// assets of that assetType (generally determined by View access)
        /// </summary>
        public bool AllowNewValues
        {
            get { return this._allowNewValues; }
            set
            {
                if (this._allowNewValues == value) { return; }
                this._allowNewValues = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Whether or not this property is inherited from its parent assetType definition
        /// Used within the context of an AssetTypePropertyRelation
        /// </summary>
        public bool IsInherited
        {
            get { return this._isInherited; }
            set
            {
                if (this._isInherited == value) { return; }
                this._isInherited = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Whether or not this property's value is inherited from its parent asset; if true, IsReadOnly must also be true
        /// Used within the context of an AssetTypePropertyRelation
        /// </summary>
        public bool IsInheritedValue
        {
            get { return this._isInheritedValue; }
            set
            {
                if (this._isInheritedValue == value) { return; }
                this._isInheritedValue = value;
                this.IsDirty = true;
            }
        }

        public bool IsInstance
        {
            get { return this._isInstance; }
            set
            {
                if (this._isInstance == value) { return; }
                this._isInstance = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Whether or not values are ordered for this property; true only allowed if AllowMultiValue is true
        /// </summary>
        public bool IsOrdered
        {
            get { return this._isOrdered; }
            set
            {
                if (this._isOrdered == value) { return; }
                this._isOrdered = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Whether or not this property is required; used only when property is part of a property group
        /// </summary>
        public bool IsRequired
        {
            get { return this._isRequired; }
            set
            {
                if (this._isRequired == value) { return; }
                this._isRequired = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Whether or not this property is read only; used only when property is part of a property group
        /// </summary>
        public bool IsReadOnly
        {
            get { return this._isReadOnly; }
            set
            {
                if (this._isReadOnly == value) { return; }
                this._isReadOnly = value;
                this.IsDirty = true;
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

        /// <summary>
        /// Defines whether or not this property defines a relationship between assets
        /// If so, the Value field in this instance will contain the id of an asset (Guid)
        /// This Guid will be the Id of the record inserted into the table [AssetRelations]
        /// </summary>
        public bool IsRelationship
        {
            get { return this._isRelationship; }
            set
            {
                if (this._isRelationship == value) { return; }
                this._isRelationship = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Used if the property's dataType is of some type of Asset
        /// <summary>
        public Guid? AssetTypeId
        {
            get { return this._assetTypeId; }
            set
            {
                if ((this._assetTypeId.HasValue) && (this._assetTypeId.Value.CompareTo(value) == 0))
                {
                    return;
                }
                else
                {
                    this._assetTypeId = value;
                    this.IsDirty = true;
                }
            }
        }

        public bool? AssetTypeIsInstance
        {
            get { return this._assetTypeIsInstance; }
            set
            {
                if (this._assetTypeIsInstance != value)
                {
                    this._assetTypeIsInstance = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Id of the role to which this property will trigger membership; used for dynamic role membership
        /// <summary>
        public Guid? RoleId
        {
            get { return this._roleId; }
            set
            {
                if ((this._roleId.HasValue) && (this._roleId.Value.CompareTo(value) == 0))
                {
                    return;
                }
                else
                {
                    this._roleId = value;
                    this.IsDirty = true;
                }
            }
        }

        public bool UsesRole
        {
            get
            {
                if ((this.RoleId.HasValue) && (this.RoleId != new Guid())) { return true; }
                return false;
            }
            private set { return; } // dirty hack exists b/c WCF doesn't allow readonly properties
        }

        public Guid? DependentPropertyId
        {
            get { return this._dependentPropertyId; }
            set
            {
                // TODO: Implement this property in view operations
                if ((this._dependentPropertyId.HasValue) && (this._dependentPropertyId.Value.CompareTo(value) == 0))
                {
                    return;
                }
                else
                {
                    this._dependentPropertyId = value;
                    this.IsDirty = true;
                }
            }
        }

        //public List<PropertyTrigger> Triggers { get; set; }

        #endregion

        #region Constructors

        public XProperty()
        {
            this._isReadOnly = true; // read-only by default
            //this._properties.PropertyAdded += new PropertyAddedEventHandler(_propertys_PropertyAdded);
            //this._properties.PropertyRemoved += new PropertyRemovedEventHandler(_propertys_PropertyRemoved);
        }

        public XProperty(string name, string displayVal, EDataType dataType, string desc, Guid createdBy)
        {
            this.Name = name;
            this.DisplayValue = displayVal;
            this.DataType = dataType;
            this.Description = desc;
            this.CreatedBy = createdBy;
            this.IsDirty = true;
        }

        public XProperty(Guid id, string name, string displayVal, EDataType dataType, string desc, Guid createdBy)
        {
            this.Id = id;
            this.Name = name;
            this.DisplayValue = displayVal;
            this.DataType = dataType;
            this.Description = desc;
            this.CreatedBy = createdBy;
            this.IsDirty = true;
        }

        public XProperty(Guid id, string name, string displayVal, Guid pickListId, string desc, Guid createdBy)
        {
            this.Id = id;
            this.Name = name;
            this.DisplayValue = displayVal;
            this.DataType = EDataType.PickList;
            this.PickListId = pickListId;
            this.Description = desc;
            this.Created = DateTime.Now;
            this.CreatedBy = createdBy;
            this.IsDirty = true;
        }

        #endregion

    }

}