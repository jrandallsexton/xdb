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

    /// <summary>The view's SELECT for the report as stored; null for an absent or stored-procedure report.</summary>
    Task<string?> BuildSelectAsync(Guid reportId, CancellationToken cancellationToken = default);

    /// <summary>
    /// ReportLayer.Save's view step: drop the view, then (ViewCreate) drop it again and create it. A failure of the
    /// second part goes to <paramref name="onCreateFailed"/>; the view is already gone by then.
    /// </summary>
    Task ReplaceAsync(Guid reportId, string select, Func<Exception, Task> onCreateFailed, CancellationToken cancellationToken = default);
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
        if (await BuildSelectAsync(reportId, cancellationToken) is not { } select)
            return false;
        var viewName = GeneratedObjectNames.ReportView(reportId);
        await procedures.ExecuteAsync(ViewSql.DropView(viewName), DdlTimeout, cancellationToken);
        await procedures.ExecuteAsync(ViewSql.CreateView(viewName, select), DdlTimeout, cancellationToken);
        return true;
    }

    public async Task<string?> BuildSelectAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        var (report, properties) = await reports.GetReportAsync(reportId, cancellationToken);
        if (report is null || !string.IsNullOrEmpty(report.StoredProc))
            return null;

        var columns = properties
            .Select(p => new ViewColumn(p.PropertyId ?? Guid.Empty, p.SubPropertyId, p.CustomReportFieldTypeId, p.Property, p.SubProperty))
            .ToList();
        var filters = (await reports.GetReportFiltersAsync(reportId, cancellationToken))
            .Select(f => new ViewFilter(f.PropertyId, f.OperatorId ?? throw new InvalidOperationException("Report filter without an operator."), f.Operator, f.Value))
            .ToList();

        var input = await loader.LoadAsync(report.AssetTypeId ?? Guid.Empty, report.IsInstance, columns, filters, report.FilterExpression, cancellationToken);
        return ViewSql.BuildSelect(input);
    }

    public async Task ReplaceAsync(Guid reportId, string select, Func<Exception, Task> onCreateFailed, CancellationToken cancellationToken = default)
    {
        var viewName = GeneratedObjectNames.ReportView(reportId);
        await procedures.ExecuteAsync(ViewSql.DropView(viewName), DdlTimeout, cancellationToken);
        try
        {
            await procedures.ExecuteAsync(ViewSql.DropView(viewName), DdlTimeout, cancellationToken);
            await procedures.ExecuteAsync(ViewSql.CreateView(viewName, select), DdlTimeout, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            await onCreateFailed(e);
        }
    }
}
