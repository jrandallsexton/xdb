
using System;
using System.Collections.Generic;

using XDB.DataObjects;
using XDB.Enumerations;
using XDB.Exceptions;
using XDB.DAL;
using XDB.Interfaces;

namespace XDB.BLL
{

    /// <summary>
    /// Primary entry point for manipulating <see cref="Image"/> instances.
    /// This is NOT the same class as System.Drawing.Image
    /// </summary>
    public class XImageLayer
    {

        private XImageDal dal = new XImageDal();

        public XImageLayer() { }

        //public ImageLayer(EApplicationInstance target)
        //{
        //    string connString = SystemFrameworkHelper.DbConnStringByInstance(target);
        //    this.dal = new ImageDal(connString);
        //}

        public XImage Get(Guid id, bool omitData)
        {
            XImage image = this.dal.Get(id, omitData);
            //if (image != null)
            //{
            //    image.RequestUrl = SystemFrameworkHelper.ViewImageUrl + image.Id.ToString();
            //}
            return image;
        }

        public XImage Image_GetForAsset(Guid assetId, ICoreInstanceConfig instanceConfig)
        {
            XValue pv = new XValueLayer().Get(instanceConfig.AssetLogoPropertyId, assetId);
            if (pv != null)
            {
                if (!string.IsNullOrEmpty(pv.Value))
                {
                    Guid imageId = new Guid(pv.Value);
                    return this.Get(imageId, false);
                }
            }
            return null;
        }

        public bool Save(XImage image)
        {
            return this.ImageIsValid(image) && this.dal.Save(image);
        }

        public bool Delete(Guid imageId, Guid userId)
        {
            return this.dal.Delete(imageId, userId);
        }

        public Dictionary<Guid, string> ImageDictionary_GetSystem()
        {
            return this.dal.ImageDictionary_GetSystem();
        }

        private bool ImageIsValid(XImage image)
        {
            //if (image.ImageData == null) { throw new LogicalException("Image Data cannot be null", "ImageData"); }
            if (image.ImageType == EImageType.Undefined) { throw new LogicalException("ImageType cannot be undefined", "ImageType"); }
            if (string.IsNullOrEmpty(image.Name)) { throw new LogicalException("Name cannot be undefined", "Name"); }
            //if (string.IsNullOrEmpty(image.Description)) { throw new LogicalException("Description cannot be undefined", "Description"); }
            if (image.CreatedBy.CompareTo(new Guid()) == 0) { throw new LogicalException("CreateBy cannot be null", "CreatedBy"); }
            if (new XUserLayer().ValidId(image.CreatedBy) == false)
            {
                throw new LogicalException("Invalid user id", "CreatedBy");
            }
            return true;
        }

        public bool IsValidId(Guid id)
        {
            return this.dal.IsValidId(id);
        }

        public EImageType Image_GetType(string fileName)
        {
            string extension = System.IO.Path.GetExtension(fileName).ToLower();

            switch (extension)
            {
                case ".bmp":
                    return EImageType.Bitmap;
                case ".gif":
                    return EImageType.GIF;
                case ".ico":
                    return EImageType.ICO;
                case ".jpg":
                case ".jpeg":
                    return EImageType.JPEG;
                case ".png":
                    return EImageType.PNG;
                case ".tif":
                    return EImageType.TIF;
                default:
                    return EImageType.Undefined;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageStreamUrl">URL for returning an image stream. This method will append the image id to the end of the provided value</param>
        /// <returns></returns>
        public List<XImage> GetSystem()
        {
            return this.dal.GetSystem();
        }

        public XImageInfo Image_GetInfo(byte[] imageData)
        {
            XImageInfo info;

            using (System.IO.MemoryStream str = new System.IO.MemoryStream(imageData, false))
            {
                System.Drawing.Image img = System.Drawing.Image.FromStream(str);
                info = new XImageInfo();
                info.height = img.Height;
                info.width = img.Width;
            }

            return info;
        }

    }

}