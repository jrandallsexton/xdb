
using System;

using XDB.Common.Enumerations;

namespace XDB.Common.Interfaces
{
    public interface IXImage
    {
        byte[] ImageData { get; set; }
        EImageType ImageType { get; set; }
        short Order { get; set; }
    }
}
