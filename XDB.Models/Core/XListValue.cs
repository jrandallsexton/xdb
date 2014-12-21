
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Common.Interfaces;

using XDB.Common;

namespace XDB.Models
{

    public class XListValue : XBase, IXListValue
    {

        private Guid _xListId;
        private string _value = string.Empty;
        private int _index;
        private string _bgColor = string.Empty;

        /// <summary>
        /// XListId
        /// <summary>
        public Guid XListId
        {
            get { return this._xListId; }
            set
            {
                if (this._xListId.CompareTo(value) == 0) { return; }
                this._xListId = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Value
        /// <summary>
        public string Value
        {
            get { return this._value; }
            set
            {
                if (this._value.Equals(value, StringComparison.Ordinal)) { return; }
                this._value = value;
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Numerical order in which the item should appear in UI instances; zero-based index
        /// <summary>
        public int Index
        {
            get { return this._index; }
            set
            {
                if (this._index == value) { return; }
                this._index = value;
                this.IsDirty = true;
            }
        }

        public string BGColor
        {
            get { return this._bgColor; }
            set
            {
                if (this._bgColor.Equals(value, StringComparison.Ordinal)) { return; }
                this._bgColor = value;
                this.IsDirty = true;
            }
        }

        public XListValue() { }

        public XListValue(Guid xListId, string value, string displayValue, int index, Guid memberId)
        {
            this.XListId = xListId;
            this.Value = value;
            this.DisplayValue = displayValue;
            this.Index = index;
            this.CreatedBy = memberId;
            this.Created = DateTime.Now;
        }

    }

}