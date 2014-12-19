
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Enumerations;

namespace XDB.DataObjects
{

    //<summary>
    //</summary>
    public class XImage : XBase
    {

        private EImageType _imageType = EImageType.Undefined;
        private Int16 _order = 0;
        private Byte[] _imageData = null;

        /// <summary>
        /// Id of the image's type; FK into [ImageTypes]
        /// <summary>
        public EImageType ImageType
        {
            get { return this._imageType; }
            set
            {
                if (this._imageType != value)
                {
                    this._imageType = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Value of the numerical order in which images should be displayed; zero-based index
        /// <summary>
        public Int16 Order
        {
            get { return this._order; }
            set
            {
                if (this._order != value)
                {
                    this._order = value;
                    this.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Binary data of the image
        /// <summary>
        public Byte[] ImageData
        {
            get { return this._imageData; }
            set
            {
                if (this._imageData != value)
                {
                    this._imageData = value;
                    this.IsDirty = true;
                }
            }
        }

        //public int Width { get; set; }

        //public int Height { get; set; }

        //public string RequestUrl { get; set; }

    }

}