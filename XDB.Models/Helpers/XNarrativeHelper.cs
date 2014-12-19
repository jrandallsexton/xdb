
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDB.Models
{

    public sealed class XNarrativeHelper
    {
        public Guid AssetId { get; set; }
        public string AssetName { get; set; }

        public Guid AssetTypeId { get; set; }
        public string AssetType { get; set; }

        public bool IsInstance { get; set; }
        public string InstanceOf { get; set; }
        public DateTime AssetCreated { get; set; }
        public Guid SubmittalId { get; set; }
        public DateTime SubmittalDate { get; set; }
        public string Submitter { get; set; }

        public int ValueCount
        {
            get { return this.Values.Count; }
        }

        public bool WasCreation
        {
            get { return this.AssetCreated.ToShortDateString() == this.SubmittalDate.ToShortDateString(); }
        }

        public List<Guid> PropertyIds { get; set; }
        public Dictionary<Guid, string> Values { get; set; }
        public Dictionary<Guid, string> Decoded { get; set; }

        public XNarrativeHelper()
        {
            this.PropertyIds = new List<Guid>();
            this.Values = new Dictionary<Guid, string>();
            this.Decoded = new Dictionary<Guid, string>();
        }

    }

}