
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Common;

namespace XDB.Models
{

    //<summary>
    //</summary>
    public class XUser : XBase
    {

        private string _userId = string.Empty;
        private string _lastName = string.Empty;
        private string _firstName = string.Empty;
        private char? _middleInitial;

        public XUser() { }

        public XUser(string userId, string ln, string fn, Guid createdBy)
        {
            this.Id = Guid.NewGuid();
            this.Created = DateTime.Now;
            this.CreatedBy = createdBy;
            this.UserId = userId;
            this.LastName = ln;
            this.FirstName = fn;
            this.IsDirty = true;
            this.IsNew = true;
        }

        public XUser(string userName, string lName, string fName, string mName, Guid userId)
        {
            this.Id = System.Guid.NewGuid();
            this.UserId = userName;
            this.LastName = lName;
            this.FirstName = fName;
            this.IsDirty = true;
            this.IsNew = true;
            this.Created = DateTime.Now;
            this.CreatedBy = userId;
            this.IsDirty = true;
            this.IsNew = true;
        }

        #region public properties

        /// <summary>
        /// UserId of the member
        /// <summary>
        public string UserId
        {
            get { return this._userId; }
            set
            {

                if (!string.IsNullOrEmpty(value))
                {
                    if (value.Length > 50) value = value.Substring(0, 49);
                }

                if (this._userId != value)
                {
                    this._userId = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Last name of the member
        /// </summary>
        public string LastName
        {
            get { return this._lastName; }
            set
            {

                if (!string.IsNullOrEmpty(value))
                {
                    if (value.Length > 50) value = value.Substring(0, 49);
                }

                if (this._lastName != value)
                {
                    this._lastName = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// First name of the member
        /// </summary>
        public string FirstName
        {
            get { return this._firstName; }
            set
            {

                if (!string.IsNullOrEmpty(value))
                {
                    if (value.Length > 50) value = value.Substring(0, 49);
                }

                if (this._firstName != value)
                {
                    this._firstName = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Middle initial of the member
        /// </summary>
        public char? MiddleInitial
        {
            get { return this._middleInitial; }
            set
            {
                if ((this._middleInitial.HasValue) && (this._middleInitial.Value.CompareTo(value) == 0))
                {
                    return;
                }
                else
                {
                    this._middleInitial = value;
                    this.IsDirty = true;
                }
            }
        }

        public new string DisplayValue
        {
            get
            {
                if (this.MiddleInitial.HasValue)
                {
                    return string.Format("{0}, {1} {2} [{3}]", this.LastName, this.FirstName, this.MiddleInitial.Value.ToString(), this.UserId);
                }
                else
                {
                    return string.Format("{0}, {1} [{2}]", this.LastName, this.FirstName, this.UserId);
                }
            }
            private set { return; } // dirty hack exists b/c WCF doesn't allow readonly properties
        }

        #endregion

        public new string ToString()
        {
            bool hasLast = !string.IsNullOrEmpty(this.LastName);
            bool hasFirst = !string.IsNullOrEmpty(this.FirstName);
            bool hasMI = this.MiddleInitial.HasValue;

            if (hasLast && hasFirst && hasMI)
            {
                return string.Format("{0}, {1} {2} ({3})", this.LastName, this.FirstName, this.MiddleInitial.ToString(), this.UserId);
            }
            else if (hasLast && hasFirst)
            {
                return string.Format("{0}, {1} ({2})", this.LastName, this.FirstName, this.UserId);
            }
            else
            {
                return this.UserId;
            }
        }

    }

}