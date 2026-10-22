using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Xdb.Api.Application.Charts.Legacy;

/// <summary>
/// The original's chart object as its data-contract serializer wrote it: 34 members in ordinal name order (so
/// <c>yAxisLabel</c> sorts last), enums as numbers, nulls emitted. The list members are the original's field-initialised
/// empty lists; <see cref="DataDictionary"/> starts null.
/// </summary>
[DataContract(Name = "Chart", Namespace = "")]
public sealed class ChartContract
{
    [DataMember] public string? AppearanceName { get; set; }
    [DataMember] public int AssetRequestType { get; set; }
    [DataMember] public string? AssetType { get; set; }
    [DataMember] public Guid AssetTypeId { get; set; }
    [DataMember] public string? ChartTitle { get; set; }
    [DataMember] public int ChartType { get; set; }
    [DataMember] public DateTime Created { get; set; }
    [DataMember] public Guid CreatedBy { get; set; }
    [DataMember] public Dictionary<string, string> Data { get; set; } = [];
    [DataMember] public Dictionary<string, string>? DataDictionary { get; set; }
    [DataMember] public DateTime? Deleted { get; set; }
    [DataMember] public Guid? DeletedBy { get; set; }
    [DataMember] public string? FilterLogic { get; set; }
    [DataMember] public List<ChartFilterContract> Filters { get; set; } = [];
    [DataMember] public string? FullSql { get; set; }
    [DataMember] public List<string> GroupColumns { get; set; } = [];
    [DataMember] public string? GroupSql { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public bool IsInstance { get; set; }
    [DataMember] public bool IsNew { get; set; }
    [DataMember] public bool IsPublic { get; set; }
    [DataMember] public string? LegendText { get; set; }
    [DataMember] public bool LegendVisible { get; set; }
    [DataMember] public string? Name { get; set; }
    [DataMember] public string? PaletteName { get; set; }
    [DataMember] public List<string> Properties { get; set; } = [];
    [DataMember] public List<Guid> PropertyIds { get; set; } = [];
    [DataMember] public List<string> PropertyNames { get; set; } = [];
    [DataMember] public int RecordCount { get; set; }
    [DataMember] public string? ReportingLabel { get; set; }
    [DataMember] public string? SeriesName { get; set; }
    [DataMember] public string? Subtitle { get; set; }
    [DataMember] public string? Title { get; set; }
    [DataMember] public string? ViewType { get; set; }
    [DataMember(Name = "yAxisLabel")] public string? YAxisLabel { get; set; }
}

/// <summary>
/// The dashboard chart client: 28 members in ordinal order. A new instance is the original's constructor state (new
/// id, created now, empty data, null data dictionary, isNew).
/// </summary>
[DataContract(Name = "BarChartClient", Namespace = "")]
public sealed class BarChartClientContract
{
    [DataMember(Name = "appearanceName")] public string? AppearanceName { get; set; }
    [DataMember(Name = "assetTypeId")] public Guid AssetTypeId { get; set; }
    [DataMember(Name = "chartTitle")] public string? ChartTitle { get; set; }
    [DataMember(Name = "created")] public DateTime Created { get; set; }
    [DataMember(Name = "createdBy")] public Guid CreatedBy { get; set; }
    [DataMember(Name = "data")] public Dictionary<string, string> Data { get; set; } = [];
    [DataMember(Name = "dataDictionary")] public Dictionary<string, string>? DataDictionary { get; set; }
    [DataMember(Name = "deleted")] public DateTime? Deleted { get; set; }
    [DataMember(Name = "deletedBy")] public Guid? DeletedBy { get; set; }
    [DataMember(Name = "filterLogic")] public string? FilterLogic { get; set; }
    [DataMember(Name = "groupColumns")] public List<Guid> GroupColumns { get; set; } = [];
    [DataMember(Name = "id")] public Guid Id { get; set; }
    [DataMember(Name = "isInstance")] public bool IsInstance { get; set; }
    [DataMember(Name = "isNew")] public bool IsNew { get; set; } = true;
    [DataMember(Name = "isPublic")] public bool IsPublic { get; set; }
    [DataMember(Name = "legendText")] public string? LegendText { get; set; }
    [DataMember(Name = "legendVisible")] public bool LegendVisible { get; set; }
    [DataMember(Name = "name")] public string? Name { get; set; }
    [DataMember(Name = "paletteName")] public string? PaletteName { get; set; }
    [DataMember(Name = "propertyIds")] public List<Guid> PropertyIds { get; set; } = [];
    [DataMember(Name = "propertyNames")] public List<string> PropertyNames { get; set; } = [];
    [DataMember(Name = "recordCount")] public int RecordCount { get; set; }
    [DataMember(Name = "reportingLabel")] public string? ReportingLabel { get; set; }
    [DataMember(Name = "seriesName")] public string? SeriesName { get; set; }
    [DataMember(Name = "subtitle")] public string? Subtitle { get; set; }
    [DataMember(Name = "title")] public string? Title { get; set; }
    [DataMember(Name = "viewType")] public string? ViewType { get; set; }
    [DataMember(Name = "yAxisLabel")] public string? YAxisLabel { get; set; }
}

/// <summary>A chart filter as the chart object carries it (filled by the chart reads).</summary>
[DataContract(Name = "Filter", Namespace = "")]
public sealed class ChartFilterContract
{
}
