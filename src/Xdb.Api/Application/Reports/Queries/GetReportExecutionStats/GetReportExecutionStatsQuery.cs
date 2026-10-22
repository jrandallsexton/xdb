using System;

namespace Xdb.Api.Application.Reports.Queries.GetReportExecutionStats;

public sealed record GetReportExecutionStatsQuery(Guid ReportId, Guid UserId);
