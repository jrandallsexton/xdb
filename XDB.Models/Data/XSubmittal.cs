
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Common;
using XDB.Common.Interfaces;

namespace XDB.Models
{

    public class XSubmittal : XBase, IXSubmittal
    {

        private Guid _assetId;
        private string _notes = string.Empty;
        private IList<IXValue> _propertyValues = null;

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

        public string AssetName { get; set; }

        public string PickListValue { get; set; }

        public string Notes
        {
            get { return this._notes; }
            set
            {
                if (this._notes != value)
                {
                    this._notes = value;
                    this.IsDirty = true;
                }
            }
        }

        public string CreatedByDisplay { get; set; }

        public string ApprovedByDisplay { get; set; }

        public IList<IXValue> PropertyValues
        {
            get { return this._propertyValues; }
            set { this._propertyValues = value; }
        }

        public XSubmittal() { }

        public XSubmittal(Guid assetId, string assetName, Guid userId)
        {
            this.AssetId = assetId;
            this.AssetName = assetName;
            this.Created = DateTime.Now;
            this.CreatedBy = userId;
            this.IsDirty = true;
            this.IsNew = true;
        }

        public XSubmittal(Guid submittalId, Guid assetId, string assetName, Guid userId)
        {
            this.Id = submittalId;
            this.AssetId = assetId;
            this.AssetName = assetName;
            this.Created = DateTime.Now;
            this.CreatedBy = userId;
            this.IsDirty = true;
            this.IsNew = true;
        }

    }

}