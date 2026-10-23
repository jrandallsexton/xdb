using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Metadata.Queries.Lookups;
using Xdb.Api.Application.Reports.Queries.GetReportData;
using Xdb.Api.Application.Users.Queries;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Reports.Export;

/// <summary>An exported workbook and the file name the original gave it.</summary>
public sealed record ExportedWorkbook(byte[] Data, string FileName);

public interface IReportExportHandler
{
    Task<Result<ExportedWorkbook>> ExportAsync(Guid reportId, Guid userId, string? groupColName, bool forBulkUpdate, CancellationToken cancellationToken = default);
}

/// <summary>
/// Report_Export (spec M8-B section 4): the report's data (as Report_GetData, with its report log), currency
/// entities turned into symbols, then the DumpExcel workbook.
/// </summary>
public sealed class ReportExportHandler(
    IGetReportDataQueryHandler reportData, IUserQueryHandler users, ReportLoader loader, IMetadataProcedures metadata, IReportStore store,
    IOptions<LegacyEnvironmentOptions> environment, IDateTimeProvider clock) : IReportExportHandler
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    public async Task<Result<ExportedWorkbook>> ExportAsync(Guid reportId, Guid userId, string? groupColName, bool forBulkUpdate,
        CancellationToken cancellationToken = default)
    {
        var table = (await reportData.ExecuteAsync(new GetReportDataQuery(reportId, userId, 1, string.Empty, string.Empty), cancellationToken)).Value;
        var author = (await users.GetDisplayNameAsync(userId, cancellationToken)).Value;
        var report = (await loader.LoadAsync(reportId, cancellationToken)).Report;

        var propertyIds = new List<Guid>();
        foreach (var rp in report.ReportProperties!)
        {
            if (!propertyIds.Contains(rp.PropertyId))
                propertyIds.Add(rp.PropertyId);
        }
        var properties = propertyIds.Count == 0
            ? []
            : (await metadata.GetPropertiesAsync(propertyIds, cancellationToken)).ToDictionary(p => p.Id);
        foreach (var rp in report.ReportProperties)
        {
            if (properties[rp.PropertyId].DataTypeId != (int)LegacyDataType.Currency)
                continue;
            var column = await metadata.GetPropertyDisplayValueAsync(rp.PropertyId, cancellationToken);
            var symbols = await store.GetCurrencyDisplaysAsync(cancellationToken);
            ReplaceCurrencyEntities(table, column, symbols);
        }

        if (forBulkUpdate)
        {
            // "Export to Excel for Bulk Update" builds a bulk-upload template workbook: deferred with bulk uploads
            // (owner decision 2026-10-23).
            throw new NotSupportedException("Deferred: the bulk-update export belongs to bulk uploads.");
        }

        var now = clock.UtcNow().ToLocalTime();
        var bytes = ReportWorkbook.Write(table, report.ReportName ?? string.Empty, groupColName, author, environment.Value.ProductName, environment.Value.CompanyName,
            now, () => "R" + Guid.NewGuid().ToString("N")[..16]);
        var fileName = $"{(report.ReportName ?? string.Empty).Replace(" ", "_")}_{now.ToString("ddMMMyyyy", EnUs).Replace("/", "_")}.xlsx";
        return new Success<ExportedWorkbook>(new ExportedWorkbook(bytes, fileName));
    }

    /// <summary>A value holding "&amp;#" gets the first symbol entity (in Value order) it contains replaced by the symbol.</summary>
    private static void ReplaceCurrencyEntities(DataTable table, string column, IReadOnlyList<KeyValuePair<string, string>> symbols)
    {
        var dataColumn = table.Columns[column] ?? throw new NullReferenceException("Object reference not set to an instance of an object.");
        dataColumn.ReadOnly = false;
        foreach (DataRow row in table.Rows)
        {
            var text = row.IsNull(dataColumn) ? string.Empty : row[dataColumn].ToString()!;
            if (string.IsNullOrEmpty(text) || !text.Contains("&#", StringComparison.Ordinal))
                continue;
            foreach (var (value, display) in symbols)
            {
                if (text.Contains(value, StringComparison.Ordinal))
                {
                    text = text.Replace(value, display, StringComparison.Ordinal);
                    break;
                }
            }
            row[dataColumn] = text;
        }
    }
}
