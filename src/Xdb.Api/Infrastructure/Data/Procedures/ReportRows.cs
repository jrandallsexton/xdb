using System;

namespace Xdb.Api.Infrastructure.Data.Procedures;

/// <summary>First result set of spr_Report_Get.</summary>
public sealed class ReportRow
{
    public string ReportName { get; init; } = string.Empty;
    public Guid? AssetTypeId { get; init; }
    public string? FilterExpression { get; init; }
    public string? StoredProc { get; init; }
    public bool IsInstance { get; init; }
    public string? ReportingLabel { get; init; }
}

/// <summary>Second result set of spr_Report_Get (spr_ReportProperties_Get), ordered by [Order].</summary>
public sealed class ReportPropertyRow
{
    public Guid Id { get; init; }
    public Guid? PropertyId { get; init; }
    public Guid? SubPropertyId { get; init; }
    public int Order { get; init; }
    public bool IsGrouping { get; init; }
    public int? CustomReportFieldTypeId { get; init; }
    public string? Label { get; init; }
    public string? Property { get; init; }
    public string? SubProperty { get; init; }
}

/// <summary>spr_ReportFilters_Get, ordered by [Order].</summary>
public sealed class ReportFilterRow
{
    public Guid Id { get; init; }
    public Guid PropertyId { get; init; }
    public string? Property { get; init; }
    public int? OperatorId { get; init; }
    public string Operator { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public int Order { get; init; }
}

/// <summary>The columns of spr_Properties_GetByListOfPropertyIds that reports use.</summary>
public sealed class PropertyRow
{
    public Guid Id { get; init; }
    public byte DataTypeId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? DisplayValue { get; init; }
    public bool IsSystem { get; init; }
    public int? SystemTypeId { get; init; }
}

/// <summary>The columns of spr_PickListValue_Get that reports use.</summary>
public sealed class PickListValueRow
{
    public string Value { get; init; } = string.Empty;
    public string? DisplayValue { get; init; }
}

/// <summary>The columns of spr_Member_Get that reports use.</summary>
public sealed class MemberNameRow
{
    public string UserId { get; init; } = string.Empty;
    public string? LName { get; init; }
    public string? FName { get; init; }
}

/// <summary>spSys_Report_Execution_Stats: the latest live log row for a report and member.</summary>
/// <param name="CompletedToStarted">time of (Started - Completed), wrapped past midnight when negative.</param>
/// <param name="StartedToCompleted">time of (Completed - Started).</param>
public sealed record ReportExecutionStatsRow(int? RecordsReturned, bool? WasCached, TimeSpan? CompletedToStarted, TimeSpan? StartedToCompleted);
