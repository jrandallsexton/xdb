using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Reports.Queries.GetReportExecutionStats;

public interface IGetReportExecutionStatsQueryHandler
{
    Task<Result<string>> ExecuteAsync(GetReportExecutionStatsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// "15 records in 00:00.123 (mm:ss:ms)" for the member's latest run of a report, or "" when there is none.
/// Elapsed time is computed by SQL (its datetime rounding is part of the output) and trimmed as the original did,
/// including its quirks: a zero count renders as "", a whole-second time collapses to one character, and a run
/// whose log is not closed yet fails.
/// </summary>
public sealed class GetReportExecutionStatsQueryHandler(IReportProcedures procedures) : IGetReportExecutionStatsQueryHandler
{
    public async Task<Result<string>> ExecuteAsync(GetReportExecutionStatsQuery query, CancellationToken cancellationToken = default)
    {
        var row = await procedures.GetExecutionStatsAsync(query.ReportId, query.UserId, cancellationToken);
        return new Success<string>(row is null ? string.Empty : Format(row));
    }

    public static string Format(ReportExecutionStatsRow row)
    {
        var recordsReturned = row.RecordsReturned ?? -1;
        var wasCached = row.WasCached ?? false;
        var val0 = row.CompletedToStarted?.ToString() ?? string.Empty;
        var val1 = row.StartedToCompleted?.ToString() ?? string.Empty;

        var recordCount = string.Format(CultureInfo.InvariantCulture, "{0:#,####,###}", recordsReturned);
        var message = wasCached ? "{0} records (from cache) in {1} (mm:ss:ms)" : "{0} records in {1} (mm:ss:ms)";

        string elapsed;
        if (val0.Length == 0 && val1.Length != 0)
            elapsed = Cut(val1);
        else if (val0.Length != 0 && val1.Length == 0)
            elapsed = Cut(val0);
        else if (val0.Length != 0)
            elapsed = val0[..2] == "23" ? Cut(val1) : Cut(val0);
        else
            elapsed = val1.Substring(0, 2) == "23" ? Cut(val0) : Cut(val1); // both empty: throws, as the original did

        return string.Format(CultureInfo.InvariantCulture, message, recordCount, elapsed);
    }

    /// <summary>"00:00:00.1233333" -> "00:00.123"; "00:00:01" -> "0".</summary>
    private static string Cut(string time) => time.Substring(3, time.Length - 7);
}
