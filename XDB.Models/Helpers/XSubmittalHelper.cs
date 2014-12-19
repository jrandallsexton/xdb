
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDB.Models
{

    public class XSubmittalHelper
    {
        public Guid AssetId { get; set; }
        public Guid AssetTypeId { get; set; }
        public string AssetName { get; set; }
        public string Role { get; set; }
        public Guid SubmittalId { get; set; }
        public string Narrative { get; set; }
        public DateTime Created { get; set; }

        public XSubmittalHelper(Guid assetId, Guid atId, string assetName, string role, Guid submittalId, string narrative, DateTime created)
        {
            this.AssetId = assetId;
            this.AssetTypeId = atId;
            this.AssetName = assetName;
            this.Role = role;
            this.SubmittalId = submittalId;
            this.Narrative = narrative;
            this.Created = created;
        }

    }

}