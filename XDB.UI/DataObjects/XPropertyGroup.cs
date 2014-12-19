
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.DataObjects;

namespace XDB.UI.DataObjects
{

    public class XPropertyGroup : XBase
    {

        private Guid _assetTypeId;
        private bool _isInstance = false;
        private List<XProperty> _properties = new List<XProperty>();
        private List<XPropertyGroupPropertyRelation> _propertyRelations = new List<XPropertyGroupPropertyRelation>();

        public XPropertyGroup() : base() { }

        public XPropertyGroup(string name, string display, Guid assetTypeId, bool assetTypeIsInstance, List<Guid> propertyIds, Guid userId)
            : this()
        {
            this.Name = name;
            this.DisplayValue = display;
            this.AssetTypeId = assetTypeId;
            this.IsInstance = assetTypeIsInstance;
            this.CreatedBy = userId;
            int index = 0;
            foreach (Guid id in propertyIds)
            {
                this.PropertyMembers.Add(new XPropertyGroupPropertyRelation(this.Id, id, index, true, userId));
                index++;
            }
            this.IsDirty = true;
        }

        public XPropertyGroup(Guid id, string name, string display, Guid assetTypeId, bool assetTypeIsInstance, List<Guid> propertyIds, Guid userId)
            : this()
        {
            this.Id = id;
            this.Name = name;
            this.DisplayValue = display;
            this.AssetTypeId = assetTypeId;
            this.IsInstance = assetTypeIsInstance;
            this.CreatedBy = userId;
            int index = 0;
            foreach (Guid propId in propertyIds)
            {
                this.PropertyMembers.Add(new XPropertyGroupPropertyRelation(this.Id, propId, index, false, userId));
                index++;
            }
            this.IsDirty = true;
        }

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

        public List<XProperty> Properties
        {
            get { return this._properties; }
            set { this._properties = value; }
        }

        public List<XPropertyGroupPropertyRelation> PropertyMembers
        {
            get
            {
                return this._propertyRelations;
            }
        }

        public void AddPropertyMember(XPropertyGroupPropertyRelation relation)
        {
            this._propertyRelations.Add(relation);

            this.IsDirty = true;
        }

        public void AddPropertyMember(Guid propertyId, Guid userId, int index, bool isRequired)
        {

            XPropertyGroupPropertyRelation relation = new XPropertyGroupPropertyRelation();
            relation.PropertyId = propertyId;
            relation.PropertyGroupId = this.Id;
            relation.CreatedBy = userId;
            relation.Created = DateTime.Now;
            relation.Index = index;
            relation.IsRequired = isRequired;

            this._propertyRelations.Add(relation);

            this.IsDirty = true;
        }

        public void RemovePropertyMember(Guid propertyId, Guid userId)
        {
            foreach (XPropertyGroupPropertyRelation relation in this._propertyRelations)
            {
                if (relation.PropertyId.CompareTo(propertyId) == 0)
                {
                    relation.DeletedBy = userId;
                    break;
                }
            }
        }

    }

}