using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Xdb.Api.Application.Views.Legacy;

namespace Xdb.Api.Application.Reports.Legacy;

/// <summary>A report as the original's data-contract serializer wrote it (16 members, ordinal order, nulls emitted).</summary>
[DataContract(Name = "Report", Namespace = "")]
public sealed class ReportContract
{
    [DataMember] public Guid AssetTypeId { get; set; }
    [DataMember] public DateTime Created { get; set; }
    [DataMember] public Guid CreatedBy { get; set; }
    [DataMember] public DateTime? Deleted { get; set; }
    [DataMember] public Guid? DeletedBy { get; set; }
    [DataMember] public string? FilterLogic { get; set; } = string.Empty;
    [DataMember] public string? FilterString { get; set; }
    [DataMember] public List<FilterContract>? Filters { get; set; } = [];
    [DataMember] public List<Guid>? GroupColumns { get; set; } = [];
    [DataMember] public Guid Id { get; set; }
    [DataMember] public bool IsInstance { get; set; }
    [DataMember] public bool IsNew { get; set; } = true;
    [DataMember] public string? ReportName { get; set; }
    [DataMember] public List<ReportPropertyContract>? ReportProperties { get; set; } = [];
    [DataMember] public string? ReportingLabel { get; set; }
    [DataMember] public string? StoredProc { get; set; }
}

[DataContract(Name = "ReportProperty", Namespace = "")]
public sealed class ReportPropertyContract
{
    [DataMember] public int CustomReportFieldType { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public int Index { get; set; }
    [DataMember] public bool IsGrouping { get; set; }
    [DataMember] public string? Label { get; set; }
    [DataMember] public string? Logic { get; set; }
    [DataMember] public Guid PropertyId { get; set; }
    [DataMember] public List<Guid>? PropertyIds { get; set; } = [];
    [DataMember] public string? PropertyName { get; set; }
    [DataMember] public Guid? SubPropertyId { get; set; }
    [DataMember] public string? SubPropertyName { get; set; }
    [DataMember] public int TimeElapsedFormat { get; set; }
    [DataMember] public DateTime? TimeElapsedSpecificDate { get; set; }
    [DataMember] public int TimeElapsedType { get; set; } = 1;
}

[DataContract(Name = "ReportCacheEntry", Namespace = "")]
public sealed class ReportCacheEntryContract
{
    [DataMember] public string? CacheAge { get; set; }
    [DataMember] public string? CacheTimestamp { get; set; }
    [DataMember] public int HitCount { get; set; }
    [DataMember] public Guid ReportId { get; set; }
    [DataMember] public string? ReportName { get; set; }
}
