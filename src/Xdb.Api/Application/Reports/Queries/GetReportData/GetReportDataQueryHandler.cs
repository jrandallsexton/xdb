using System;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Xdb.Api.Infrastructure.Background;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Reports.Queries.GetReportData;

public interface IGetReportDataQueryHandler
{
    Task<Result<DataTable>> ExecuteAsync(GetReportDataQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Runs a report: its generated view (or its stored procedure), with the original's paging, optional in-memory sort
/// and two-step logging. The log is opened before the query (soft-deleting earlier logs for the report) and closed
/// in the background with the row count, which is what execution stats later read.
/// </summary>
public sealed class GetReportDataQueryHandler(IReportProcedures procedures, IBackgroundWorkQueue background) : IGetReportDataQueryHandler
{
    /// <summary>The original replaced the caller's page size with this before paging.</summary>
    private const int ServerPageSize = 1_000_000;

    private static readonly CultureInfo SortCulture = CultureInfo.GetCultureInfo("en-US");

    public async Task<Result<DataTable>> ExecuteAsync(GetReportDataQuery query, CancellationToken cancellationToken = default)
    {
        var logId = Guid.NewGuid();
        await procedures.SaveReportLogAsync(logId, query.ReportId, query.UserId, recordsReturned: 0, cancellationToken);

        var storedProcedure = await procedures.GetReportStoredProcAsync(query.ReportId, cancellationToken);
        var table = storedProcedure.Length == 0
            ? await procedures.QueryReportViewAsync(query.ReportId, await procedures.GetReportOrderByColumnAsync(query.ReportId, cancellationToken), cancellationToken)
            : await procedures.ExecuteReportProcedureAsync(storedProcedure, cancellationToken)
              ?? throw new InvalidOperationException($"Report procedure {storedProcedure} failed.");

        var page = Page(table, query.Page);
        if (!string.IsNullOrEmpty(query.SortColumn))
            page.DefaultView.Sort = query.SortColumn + " " + (string.IsNullOrEmpty(query.SortOrder) ? "asc" : query.SortOrder);
        var result = page.DefaultView.ToTable();

        var total = table.Rows.Count;
        var reportId = query.ReportId;
        var userId = query.UserId;
        background.Enqueue($"close report log {logId}", (services, ct) =>
            services.GetRequiredService<IReportProcedures>().SaveReportLogAsync(logId, reportId, userId, total, ct));

        return new Success<DataTable>(result);
    }

    /// <summary>
    /// The original's paging, kept exactly: unchecked arithmetic, an inclusive upper bound (page 1 holds up to
    /// 1,000,001 rows), page 0 returns only the first row and later pages are empty in practice.
    /// </summary>
    private static DataTable Page(DataTable table, int page)
    {
        var startRec = unchecked((page - 1) * ServerPageSize);
        var endRec = unchecked(startRec + ServerPageSize);

        var paged = table.Clone();
        paged.Locale = SortCulture;
        for (var i = 0; i < table.Rows.Count; i++)
        {
            if (i > endRec)
                break;
            if (i >= startRec && i <= endRec)
                paged.Rows.Add(table.Rows[i].ItemArray);
        }
        return paged;
    }
}
