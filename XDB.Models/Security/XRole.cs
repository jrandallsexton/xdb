
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Common;

namespace XDB.Models
{

    public class XRole : XBase
    {

        public bool IsActive { get; set; }
        public Dictionary<Guid, string> Members { get; set; }
        //public List<RoleReport> Reports { get; set; } 

        /// <summary>
        /// Id of a Member (within Members) who has the ability to add/remove role members from the role (limited admin)
        /// </summary>
        public Guid? RoleLead { get; set; }

        public XRole()
        {
            this.Id = Guid.NewGuid();
            this.Created = DateTime.Now;
            this.IsActive = true;
            //this.Members = new List<RoleMember>();
            this.IsNew = true;
            this.IsDirty = true;
        }

        public XRole(Guid id, string name, string desc, Guid createdBy)
            : this()
        {
            this.Id = id;
            this.Name = name;
            this.Description = desc;
            this.CreatedBy = createdBy;
        }

        public override string ToString()
        {
            StringBuilder val = new StringBuilder();
            val.AppendFormat("Id:\t{0}", this.Id).AppendLine();
            val.AppendFormat("Name:\t{0}", this.Name).AppendLine();
            val.AppendFormat("Description:\t{0}", this.Description).AppendLine();
            val.AppendFormat("IsSystem:\t{0}", this.IsSystem).AppendLine();
            val.AppendFormat("IsActive:\t{0}", this.IsActive).AppendLine();

            return val.ToString();
        }

    }

}