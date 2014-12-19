
using System;
using System.Collections.Generic;

using XDB.Common.Interfaces;

using XDB.Common;

namespace XDB.Models
{

    ///<summary>
    ///Class object for defining PickLists
    ///Corresponding class objects are found in the database table: PickLists
    ///</summary>
    public class XList : XBase, IXList
    {

        private string _serviceUrl = string.Empty;
        private string _serviceMethod = string.Empty;
        private string _serviceUsername = string.Empty;
        private string _servicePassword = string.Empty;
        private bool _allowNewValues = false;
        private bool _isMemberList = false;
        private IList<IXListValue> _values = null;

        /// <summary>
        /// URL for the web service that will supply the values for this picklist (if applicable)
        /// <summary>
        public string ServiceUrl
        {
            get { return this._serviceUrl; }
            set
            {
                if (this._serviceUrl.Equals(value, StringComparison.Ordinal)) { return; }
                this._serviceUrl = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Name of the method on the web service that should be invoked for the values; return value should be a Dictionary<string, string>
        /// (if applicable)
        /// <summary>
        public string ServiceMethod
        {
            get { return this._serviceMethod; }
            set
            {
                if (this._serviceMethod.Equals(value, StringComparison.Ordinal)) { return; }
                this._serviceMethod = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Username required by the web service
        /// <summary>
        public string ServiceUsername
        {
            get { return this._serviceUsername; }
            set
            {
                if (this._serviceUsername.Equals(value, StringComparison.Ordinal)) { return; }
                this._serviceUsername = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Password required by the web service
        /// <summary>
        public string ServicePassword
        {
            get { return this._servicePassword; }
            set
            {
                if (this._servicePassword.Equals(value, StringComparison.Ordinal)) { return; }
                this._servicePassword = value;
                this.IsDirty = true;
            }
        }

        public bool AllowNewValues
        {
            get { return this._allowNewValues; }
            set
            {
                if (this._allowNewValues == value) { return; }
                this._allowNewValues = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// IsMemberList
        /// <summary>
        public bool IsMemberList
        {
            get { return this._isMemberList; }
            set
            {
                if (this._isMemberList != value)
                {
                    this._isMemberList = value;
                    this.IsDirty = true;
                }
            }
        }

        public IList<IXListValue> Values
        {
            get { return this._values; }
        }

        public XList()
        {
            this.Id = System.Guid.NewGuid();
            this._values = (IList<IXListValue>)new List<XListValue>();
            this.Created = DateTime.Now;
        }

        public XList(string name, string displayValue, string desc, bool allowNewValues, Guid createdBy) : this()
        {            
            this.Name = name;
            this.DisplayValue = displayValue;
            this.Description = desc;
            this.AllowNewValues = allowNewValues;
            this.CreatedBy = createdBy;
        }

        public XList(Guid id, string name, string displayValue, string desc, bool allowNewValues, DateTime created, Guid createdBy) : this()
        {
            this.Id = id;
            this.Name = name;
            this.DisplayValue = displayValue;
            this.Description = desc;
            this.AllowNewValues = allowNewValues;
            this.Created = created;
            this.CreatedBy = createdBy;
        }

        public bool AddValue(IXListValue value)
        {
            this._values.Add(value);
            return true;
        }

        public string GetValueById(Guid picklistValueId)
        {
            if ((this.Values == null) || (this.Values.Count == 0)) { return string.Empty; }

            foreach (XListValue value in this.Values)
            {
                if (value.Id.CompareTo(picklistValueId) == 0) { return value.Value; }
            }

            return string.Empty;
        }

        public string GetDisplayValueById(Guid pickListValueId)
        {
            if ((this.Values == null) || (this.Values.Count == 0)) { return string.Empty; }

            foreach (XListValue value in this.Values)
            {
                if (value.Id.CompareTo(pickListValueId) == 0) { return value.DisplayValue; }
            }

            return string.Empty;
        }

    }

}