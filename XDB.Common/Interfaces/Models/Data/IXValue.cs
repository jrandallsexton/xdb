
using System;

namespace XDB.Common.Interfaces
{

    public interface IXValue
    {
        Guid AssetId { get; set; }
        string DisplayValueHtml { get; set; }
        int? Index { get; set; }
        string Property { get; set; }
        Guid PropertyId { get; set; }
        DateTime? Rejected { get; set; }
        Guid? RejectedBy { get; set; }
        Guid? SubmittalGroupId { get; set; }
        string Value { get; set; }
    }

}