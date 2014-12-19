
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Enumerations;

namespace XDB.DataObjects
{

    /// <summary>
    /// Class for defining the relations between an <seealso cref="XObjectType"/> and the
    /// Properties <seealso cref="Property"/> which are associated with them
    /// Corresponding class objects are found in the database table: AssetTypesProperties
    /// </summary>
    public class XObjectTypeProperty : XBase
    {

        private Guid _assetTypeId;
        private Guid _propertyId;
        private bool _isInstance = false;
        private bool _isInherited = false;
        private bool _isInheritedValue = false;

        /// <summary>
        /// Id of the AssetType for this relation; FK into AssetTypes
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

        /// <summary>
        /// Id of the Property for this relation; FK into Properties
        /// </summary>
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

        public bool IsInstance
        {
            get { return this._isInstance; }
            set
            {
                if (this._isInstance != value)
                {
                    this._isInstance = value;
                    this.IsDirty = true;
                }
            }
        }

        public bool IsInherited
        {
            get { return this._isInherited; }
            set
            {
                if (this._isInherited != value)
                {
                    this._isInherited = value;
                    this.IsDirty = true;
                }
            }
        }

        public bool IsInheritedValue
        {
            get { return this._isInheritedValue; }
            set
            {
                if (this._isInheritedValue != value)
                {
                    this._isInheritedValue = value;
                    this.IsDirty = true;
                }
            }
        }

        public XObjectTypeProperty() : base() { }

        public XObjectTypeProperty(Guid assetTypeId, Guid propertyId, Guid userId, EAssetTypeClass typeClass)
        {
            this.AssetTypeId = assetTypeId;
            this.PropertyId = propertyId;
            this.IsInstance = (typeClass == EAssetTypeClass.Instance);
            this.CreatedBy = userId;
            this.Created = DateTime.Now;
        }

    }

}