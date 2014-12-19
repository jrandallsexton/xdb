
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Common.Enumerations;

namespace XDB.Models
{

    public class XReport
    {

        //private List<Guid> _propertyIds = new List<Guid>();
        //private Dictionary<Guid, string> _properties = new Dictionary<Guid, string>();
        private List<XReportProperty> _reportProperties = new List<XReportProperty>();
        private List<XFilter> _filters = new List<XFilter>();
        private List<Guid> _groupColumns = new List<Guid>();
        private Guid _assetTypeId;

        public Guid Id { get; set; }

        // TODO: Rename this to simply 'Name'
        public string ReportName { get; set; }

        public string ReportingLabel { get; set; }

        public Guid AssetTypeId
        {
            get { return this._assetTypeId; }
            set
            {
                if (this._assetTypeId.CompareTo(value) != 0)
                {
                    this._assetTypeId = value;
                }
            }
        }

        public bool IsInstance { get; set; }

        public DateTime Created { get; set; }

        public Guid CreatedBy { get; set; }

        public DateTime? Deleted { get; set; }

        public Guid? DeletedBy { get; set; }

        // TODO: Rename this to simply 'Properties'
        public List<XReportProperty> ReportProperties
        {
            get { return this._reportProperties; }
            set { this._reportProperties = value; }
        }

        public List<String> PropertyNames { get; set; }

        public List<Guid> PropertyIds { get; set; }

        public List<XFilter> Filters
        {
            get { return this._filters; }
            set { this._filters = value; }
        }

        public List<Guid> GroupColumns
        {
            get { return this._groupColumns; }
            set { this._groupColumns = value; }
        }

        /// <summary>
        /// Stores the string.format expression
        /// </summary>
        public string FilterLogic { get; set; }

        /// <summary>
        /// Provides a user-friendly version of the report filters
        /// </summary>
        public string FilterString { get; set; }

        public string StoredProc { get; set; }

        public bool IsNew { get; set; }

        public XReport()
        {
            this.Id = Guid.NewGuid();
            this.PropertyIds = new List<Guid>();
            this.GroupColumns = new List<Guid>();
            this.FilterLogic = string.Empty;
            this.IsNew = true;
        }

        public bool HasCustomFields()
        {
            if (this.ReportProperties == null) { return false; }
            foreach (XReportProperty rp in this.ReportProperties)
            {
                if (rp.CustomReportFieldType != ECustomReportFieldType.NotApplicable) { return true; }
            }
            return false;
        }

    }
}
