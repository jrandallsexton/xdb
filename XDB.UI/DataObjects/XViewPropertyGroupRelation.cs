
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.DataObjects;

namespace XDB.UI.DataObjects
{

    /// <summary>
    /// Class for defining the relations between a <seealso cref="View"/> and the
    /// PropertyGroups <seealso cref="PropertyGroup"/> which are associated with it
    /// Corresponding class objects are found in the database table: ViewsPropertyGroups
    /// </summary>
    public class ViewPropertyGroupRelation : XBase
    {

        private Guid _viewId;
        private Guid _propertyGroupId;
        private int _order = 0;

        public ViewPropertyGroupRelation() : base() { }

        public ViewPropertyGroupRelation(Guid viewId, Guid propertyGroupId, int idx, Guid userId)
            : this()
        {
            this.ViewId = viewId;
            this.PropertyGroupId = propertyGroupId;
            this.Index = idx;
            this.CreatedBy = userId;
            this.IsDirty = true;
        }

        /// <summary>
        /// Id of the view to which this record belongs; FK into Views
        /// </summary>
        public Guid ViewId
        {
            get { return this._viewId; }
            set
            {
                if (this._viewId.CompareTo(value) != 0)
                {
                    this._viewId = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Id of the property group to which this record belongs; FK into PropertyGroups
        /// </summary>
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
        /// The numerical order in which the property group should be displayed within the view
        /// </summary>
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

    }

}