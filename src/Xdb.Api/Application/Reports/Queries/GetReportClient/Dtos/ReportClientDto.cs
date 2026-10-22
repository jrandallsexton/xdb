using System;
using System.Collections.Generic;

namespace Xdb.Api.Application.Reports.Queries.GetReportClient.Dtos;

/// <summary>What the report viewer needs before loading data: column headers, grouping and a readable filter.</summary>
/// <param name="PropertyNames">
/// Column headers in report order; an element can be null (a custom field without a label). Null as a whole when a
/// stored-procedure report's procedure fails.
/// </param>
/// <param name="GroupColumns">Grouping property ids in report order, or null when the report does not group.</param>
/// <param name="GroupColumnName">Display name of the last grouping property, or null.</param>
/// <param name="FilterString">The report's filter rendered for people, "" when it has none.</param>
public sealed record ReportClientDto(
    string Name,
    IReadOnlyList<string?>? PropertyNames,
    IReadOnlyList<Guid>? GroupColumns,
    string? GroupColumnName,
    string FilterString);
