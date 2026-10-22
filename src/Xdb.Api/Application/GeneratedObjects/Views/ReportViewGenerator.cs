using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Infrastructure.Data.Procedures;

namespace Xdb.Api.Application.GeneratedObjects.Views;

public interface IReportViewGenerator
{
    /// <summary>(Re)creates a report's view from the report as stored.</summary>
    /// <returns>False when the report does not exist or is a stored-procedure report (which has no view).</returns>
    Task<bool> GenerateAsync(Guid reportId, CancellationToken cancellationToken = default);
}

/// <summary>
/// The view part of the original's ReportLayer.Save: reload the report, build the SQL, drop the view, create it.
/// If the CREATE fails the old view is already gone, as in the original.
/// </summary>
public sealed class ReportViewGenerator(IReportProcedures reports, IGeneratedObjectProcedures procedures, ViewInputLoader loader) : IReportViewGenerator
{
    /// <summary>The original's timeout for view DDL, in seconds.</summary>
    private const int DdlTimeout = 3000;

    public async Task<bool> GenerateAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        var (report, properties) = await reports.GetReportAsync(reportId, cancellationToken);
        if (report is null || !string.IsNullOrEmpty(report.StoredProc))
            return false;

        var columns = properties
            .Select(p => new ViewColumn(p.PropertyId ?? Guid.Empty, p.SubPropertyId, p.CustomReportFieldTypeId, p.Property, p.SubProperty))
            .ToList();
        var filters = (await reports.GetReportFiltersAsync(reportId, cancellationToken))
            .Select(f => new ViewFilter(f.PropertyId, f.OperatorId ?? throw new InvalidOperationException("Report filter without an operator."), f.Operator, f.Value))
            .ToList();

        var input = await loader.LoadAsync(report.AssetTypeId ?? Guid.Empty, report.IsInstance, columns, filters, report.FilterExpression, cancellationToken);
        var select = ViewSql.BuildSelect(input);

        var viewName = GeneratedObjectNames.ReportView(reportId);
        await procedures.ExecuteAsync(ViewSql.DropView(viewName), DdlTimeout, cancellationToken);
        await procedures.ExecuteAsync(ViewSql.CreateView(viewName, select), DdlTimeout, cancellationToken);
        return true;
    }
}
