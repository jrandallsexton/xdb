
using System;

using XDB.Common;
using XDB.Common.Enumerations;
using XDB.Common.Interfaces;

namespace XDB.Models
{

    public class XDocument : XBase, IXDocument
    {

        private EDocumentType _documentType = EDocumentType.Undefined;

        private string _title = string.Empty;
        private Byte[] _data = null;

        /// <summary>
        /// Id of the document's type; FK into [gRef].[DocumentTypes]
        /// <summary>
        public EDocumentType DocumentType
        {
            get { return this._documentType; }
            set
            {
                if (this._documentType != value)
                {
                    this._documentType = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// The title of the document
        /// <summary>
        public string Title
        {
            get { return this._title; }
            set
            {

                if (!string.IsNullOrEmpty(value))
                {
                    if (value.Length > 255) value = value.Substring(0, 254);
                }

                if (this._title != value)
                {
                    this._title = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Compressed binary data which is the actual document
        /// <summary>
        public Byte[] Data
        {
            get { return this._data; }
            set
            {
                if (this._data != value)
                {
                    this._data = value;
                    this.IsDirty = true;
                }
            }
        }

        public XDocument() : base() { }

        public XDocument(Guid id, bool isNew, bool isDirty)
            : this()
        {
            this.Id = id;
            this.IsNew = isNew;
            this.IsDirty = isDirty;
        }

    }

}