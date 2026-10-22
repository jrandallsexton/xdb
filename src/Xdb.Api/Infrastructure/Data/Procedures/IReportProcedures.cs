using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Xdb.Api.Infrastructure.Data.Procedures;

/// <summary>
/// The original system's stored procedures and inline SQL used by reports, called as the original called them
/// (parity first; replaced by LINQ slice by slice once the comparison harness guards the behavior).
/// String results follow the original's helpers: trimmed, and "" when there is no row or the value is NULL.
/// </summary>
public interface IReportProcedures
{
    /// <summary>spr_Report_Get: the report (null when absent) and its properties. Deleted reports are returned.</summary>
    Task<(ReportRow? Report, IReadOnlyList<ReportPropertyRow> Properties)> GetReportAsync(Guid reportId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReportFilterRow>> GetReportFiltersAsync(Guid reportId, CancellationToken cancellationToken);

    /// <summary>spr_Properties_GetByListOfPropertyIds with the ids joined as the original did (an empty list fails in SQL).</summary>
    Task<IReadOnlyList<PropertyRow>> GetPropertiesAsync(string commaSeparatedIds, CancellationToken cancellationToken);

    /// <summary>ISNULL(DisplayValue, Name) of a property.</summary>
    Task<string> GetPropertyDisplayValueAsync(Guid propertyId, CancellationToken cancellationToken);

    /// <summary>The asset type's InstanceLabel (forInstances) or DefinitionLabel; "" when deleted or absent.</summary>
    Task<string> GetReportingLabelAsync(Guid? assetTypeId, bool forInstances, CancellationToken cancellationToken);

    /// <summary>DateParts.DisplayValue, or null when there is no row.</summary>
    Task<string?> GetDatePartDisplayValueAsync(Guid datePartId, CancellationToken cancellationToken);

    Task<string> GetAssetNameAsync(Guid assetId, CancellationToken cancellationToken);

    Task<PickListValueRow?> GetPickListValueAsync(Guid pickListValueId, CancellationToken cancellationToken);

    Task<MemberNameRow?> GetMemberAsync(Guid memberId, CancellationToken cancellationToken);

    Task<string> GetReportStoredProcAsync(Guid reportId, CancellationToken cancellationToken);

    /// <summary>spr_Report_GetOrderByColumnName: label of the Order=0 property, "" when none.</summary>
    Task<string> GetReportOrderByColumnAsync(Guid reportId, CancellationToken cancellationToken);

    /// <summary>SELECT * FROM the report's generated view, optionally ordered by a column.</summary>
    Task<DataTable> QueryReportViewAsync(Guid reportId, string orderByColumn, CancellationToken cancellationToken);

    /// <summary>Executes a report's stored procedure; null when it fails (the original swallowed the error here).</summary>
    Task<DataTable?> ExecuteReportProcedureAsync(string procedureName, CancellationToken cancellationToken);

    /// <summary>spSys_ReportLog_Save: opens the log (soft-deleting earlier ones for the report) or, if it exists, closes it.</summary>
    Task SaveReportLogAsync(Guid logId, Guid reportId, Guid memberId, int recordsReturned, CancellationToken cancellationToken);

    Task<ReportExecutionStatsRow?> GetExecutionStatsAsync(Guid reportId, Guid userId, CancellationToken cancellationToken);
}
