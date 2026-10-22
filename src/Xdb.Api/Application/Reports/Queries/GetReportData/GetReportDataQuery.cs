using System;

namespace Xdb.Api.Application.Reports.Queries.GetReportData;

/// <param name="UserId">Recorded as the member who ran the report (not validated, as in the original).</param>
/// <param name="Page">1 returns every row; the original's paging ignores the caller's page size.</param>
/// <param name="SortColumn">Optional in-memory sort column (the original UI never sends one).</param>
public sealed record GetReportDataQuery(Guid ReportId, Guid UserId, int Page, string? SortColumn, string? SortOrder);
