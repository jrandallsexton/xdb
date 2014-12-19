
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDB.DataObjects
{

    /// <summary>
    /// Class for storing Property Values whose corresponding Property.DataType is EDataType.URL
    /// Corresponding class objects are found in the database table: URLs
    /// </summary>
    public class XUrl : XBase
    {

        private string _url = string.Empty;

        /// <summary>
        /// The actual URL
        /// </summary>
        public string Url
        {
            get { return this._url; }
            set
            {
                if (this._url != value)
                {
                    this._url = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public XUrl() { }

        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="id">id of the URL</param>
        /// <param name="name">Name of the URL (if applicable)</param>
        /// <param name="url">The URL's URI</param>
        /// <param name="createdBy">Id of the user who is creating this URL; FK into Members</param>
        public XUrl(Guid id, string name, string url, Guid createdBy)
        {
            this.Id = id;
            this.Name = name;
            this.Url = url;
            this.Created = DateTime.Now;
            this.CreatedBy = createdBy;
        }

    }

}