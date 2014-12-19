
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDB.DataObjects
{

    /// <summary>
    /// Class object used by the Bulk Upload process for logging information during the process
    /// </summary>
    public class BulkUploadLog
    {

        /// <summary>
        /// Id of a single log message
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Category of the message (i.e. Warning, Error, Info, etc)
        /// No enumeration used so as to remain flexible
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Id of the <seealso cref="Asset"/> referred to by this log entry (if applicable)
        /// </summary>
        public Guid? AssetId { get; set; }

        /// <summary>
        /// Name of the <seealso cref="Asset"/> referred to by this log entry (if applicable)
        /// </summary>
        public string AssetName { get; set; }

        /// <summary>
        /// Id of the <seealso cref="AssetType"/> of the <seealso cref="Asset"/> referred to by this log entry
        /// </summary>
        public Guid? AssetTypeId { get; set; }

        /// <summary>
        /// Whether or not the <seealso cref="Asset"/> referred to by this log entry is an instance or a definition
        /// </summary>
        public bool IsInstance { get; set; }

        /// <summary>
        /// The text of the log entry
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The numerical placedment of this entry in relation to other entries for a group of logs associated with a single bulk upload process
        /// </summary>
        public int Order { get; set; }

    }

}