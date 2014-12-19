
using System;
using System.Collections.Generic;

namespace XDB.Common.Interfaces
{
    public interface IXSubmittal
    {
        string ApprovedByDisplay { get; set; }
        Guid AssetId { get; set; }
        string AssetName { get; set; }
        string CreatedByDisplay { get; set; }
        string Notes { get; set; }
        string PickListValue { get; set; }
        IList<IXValue> PropertyValues { get; set; }
    }
}
