
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDB.Models
{

    /// <summary>
    /// Class object used by the Bulk Uploader
    /// for creating and/or updating Assets
    /// Corresponding class objects are found in the database table: BulkUploads
    /// </summary>
    public class BulkUpload
    {

        /// <summary>
        /// Id of the object; PK in BulkUploads
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Id of the file associated with this bulk upload; FK into Documents
        /// </summary>
        public Guid FileId { get; set; }

        /// <summary>
        /// When the object was created
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Id of the user who created the object; FK into Members
        /// </summary>
        public Guid CreatedBy { get; set; }

        /// <summary>
        /// When the bulk upload finished processing
        /// </summary>
        public DateTime? Completed { get; set; }

        /// <summary>
        /// How many records were created/updated as a result of the bulk upload process
        /// </summary>
        public int RecordCount { get; set; }

    }

}