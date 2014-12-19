
using System;
using System.Collections.Generic;

using XDB.DataObjects;
using XDB.Enumerations;
using XDB.Exceptions;
using XDB.DAL;

namespace XDB.BLL
{

    public class XUrlLayer
    {

        private XUrlDal _dal = new XUrlDal();

        public XUrl Get(Guid id)
        {
            return this._dal.Get(id);
        }

        public bool Save(XUrl url)
        {
            this.Validate(url);
            return this._dal.Save(url);
        }

        private void Validate(XUrl url)
        {

            if (url.Id.CompareTo(new Guid()) == 0)
            {
                throw new LogicalException("URL must have an Id");
            }

            if (string.IsNullOrEmpty(url.Url))
            {
                throw new LogicalException("URL must have a URI for the URL property");
            }

            Uri myUri;
            if (!Uri.TryCreate(url.Url, UriKind.RelativeOrAbsolute, out myUri))
            {
                throw new LogicalException("URL must have a valid URI for the URL property");
            }

            //// http://stackoverflow.com/questions/3228984/a-better-way-to-validate-url-in-c-sharp-than-try-catch
            //string regular = @"^(ht|f|sf)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&amp;%\$#_]*)?$";
            //string regular123 = @"^(www.)[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&amp;%\$#_]*)?$";

            //if (Regex.IsMatch(url.Url, regular))
            //{
            //    // ok
            //}
            //else if (Regex.IsMatch(url.Url, regular123))
            //{
            //    // ok
            //}
            //else
            //{
            //    throw new LogicalException("URL must have a valid URI for the URL property");
            //}

        }
    
    }

}