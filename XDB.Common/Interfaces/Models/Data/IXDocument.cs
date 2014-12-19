
using System;

using XDB.Common.Enumerations;

namespace XDB.Common.Interfaces
{

    public interface IXDocument
    {
        byte[] Data { get; set; }
        EDocumentType DocumentType { get; set; }
        string Title { get; set; }
    }

}