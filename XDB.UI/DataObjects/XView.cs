
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.DataObjects;

namespace XDB.UI.DataObjects
{

    ///<summary>
    ///Class object for defining views that appear within the Asset Editor in the Client UI
    ///Corresponding class objects are found in the database table: Views
    ///</summary>
    public class View : XBase
    {

        private bool _isInstance = false;
        private bool _isReadOnly = false;
        private bool _isStandAlone = true;
        private bool _isCreation = false;
        private bool _allowCloning = false;
        private Guid _assetTypeId;
        private string _driverCaption = string.Empty;
        private string _newItemCaption = string.Empty;
        private bool _allowNewValues = false;
        private string _confirmationLabel = string.Empty;
        private IList<XPropertyGroup> _propertygroups = new List<XPropertyGroup>();
        private List<ViewPropertyGroupRelation> _viewRelations = new List<ViewPropertyGroupRelation>();
        private List<XFilter> _filters = new List<XFilter>();

        public View() : base() { }

        public View(string name, string display, string desc, string driverCaption, string newItemCaption, Guid assetTypeId, bool isInstance, Guid userId)
            : this()
        {
            this.Name = name;
            this.DisplayValue = display;
            this.Description = desc;
            this.DriverCaption = driverCaption;
            this.NewItemCaption = newItemCaption;
            this.AssetTypeId = assetTypeId;
            this.IsInstance = isInstance;
            this.CreatedBy = userId;
        }

        /// <summary>
        /// The id of the asset type associated with this view and its property groups
        /// FK into AssetTypes
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
        /// Whether or not this view works with asset instances; asset definitions if false
        /// </summary>
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

        /// <summary>
        /// Whether or not assets can be edited within this view
        /// The API will set this field dynmically based on the user and their permissions
        /// </summary>
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

        /// <summary>
        /// Whether or not this view should appear within the Asset Editor menu
        /// </summary>
        public bool IsStandAlone
        {
            get { return this._isStandAlone; }
            set
            {
                if (this._isStandAlone != value)
                {
                    this._isStandAlone = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Whether nor not this view should be used to create assets
        /// </summary>
        public bool IsCreation
        {
            get { return this._isCreation; }
            set
            {
                if (this._isCreation != value)
                {
                    this._isCreation = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Defines the text shown within the Asset Editor for the item selector
        /// </summary>
        public string DriverCaption
        {
            get { return this._driverCaption; }
            set
            {
                if (this._driverCaption != value)
                {
                    this._driverCaption = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Defines the text shown when creating a new asset from within the Asset Editor
        /// </summary>
        public string NewItemCaption
        {
            get { return this._newItemCaption; }
            set
            {
                if (this._newItemCaption != value)
                {
                    this._newItemCaption = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Whether or not new assets can be added via this view
        /// Set at runtime (in the business layer) based on the requesting user's permissions
        /// Not stored in the database
        /// </summary>
        public bool AllowNewValues
        {
            get { return this._allowNewValues; }
            set { this._allowNewValues = value; }
        }

        /// <summary>
        /// Whether or not assets are allowed to be cloned into new assets from this view
        /// </summary>
        public bool AllowCloning
        {
            get { return this._allowCloning; }
            set { this._allowCloning = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string ConfirmationLabel
        {
            get { return this._confirmationLabel; }
            set
            {
                if (this._confirmationLabel != value)
                {
                    this._confirmationLabel = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Defines the collection of Property Groups for this view
        /// </summary>
        public IList<XPropertyGroup> PropertyGroups
        {
            get { return this._propertygroups; }
            set { this._propertygroups = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public List<ViewPropertyGroupRelation> PropertyGroupMembers
        {
            get { return this._viewRelations; }
        }

        /// <summary>
        /// Collection of filters that should be applied to assets selectable within this view
        /// Filters are optional
        /// </summary>
        public List<XFilter> Filters
        {
            get { return this._filters; }
            set { this._filters = value; }
        }

        public string FilterLogic { get; set; }

        /// <summary>
        /// Whether or not property values submitted within this view should be automatically approved by the API
        /// </summary>
        public bool IsAutoApprove { get; set; }

        public void AddPropertyGroupMember(ViewPropertyGroupRelation relation)
        {
            this._viewRelations.Add(relation);
            this.IsDirty = true;
        }

        public void AddPropertyGroupMember(Guid propertyGroupId, Guid userId)
        {
            //if (this._viewRelations.Contains(propertyGroupId)) { return; }
            ViewPropertyGroupRelation relation = new ViewPropertyGroupRelation();
            relation.PropertyGroupId = propertyGroupId;
            relation.ViewId = this.Id;
            relation.Created = DateTime.Now;
            relation.CreatedBy = userId;
            this._viewRelations.Add(relation);
            this.IsDirty = true;
        }

        public void RemovePropertyGroupMember(Guid propertyGroupId, Guid userId)
        {
            foreach (ViewPropertyGroupRelation relation in this._viewRelations)
            {
                if (relation.PropertyGroupId.CompareTo(propertyGroupId) == 0)
                {
                    relation.DeletedBy = userId;
                    break;
                }
            }
        }

    }

    //public class ViewButton
    //{
    //    public Guid Id { get; set; }
    //    public string Name { get; set; }
    //    public string Text { get; set; }
    //    public Dictionary<Guid, string> Preconditions = new Dictionary<Guid, string>();
    //    public Dictionary<Guid, string> Postconditions = new Dictionary<Guid, string>();
    //}

}