using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Xdb.Api.Application.GeneratedObjects.GenTables;
using Xdb.Api.Application.GeneratedObjects.Views;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.GeneratedObjects.Commands.RegenerateAll;

public sealed record RegenerateAllCommand;

/// <summary>An object whose generation failed, and why. Names are ids; the message is SQL Server's.</summary>
public sealed record GenerationFailure(string Kind, Guid Id, string Error);

public sealed record RegenerateAllResult(int GenTables, int ReportViews, int RoleViews, IReadOnlyList<GenerationFailure> Failures);

public interface IRegenerateAllCommandHandler
{
    Task<Result<RegenerateAllResult>> ExecuteAsync(RegenerateAllCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Rebuilds every generated object from the current metadata: the gen tables and MERGE procedures of every live
/// asset type, then the views of every live report, then the views of every active, live role. Views come after
/// the tables because a view binds its columns when it is created. The original had no such operation (a developer
/// tool did the same in 2013); objects of deleted types, reports and roles are left alone, as the original left them.
/// A failure is recorded and the run continues, so one bad definition does not block the rest.
/// </summary>
public sealed class RegenerateAllCommandHandler(
    IGeneratedObjectProcedures procedures,
    IGenTableGenerator genTables,
    IReportViewGenerator reportViews,
    IRoleViewGenerator roleViews,
    ILogger<RegenerateAllCommandHandler> logger) : IRegenerateAllCommandHandler
{
    public async Task<Result<RegenerateAllResult>> ExecuteAsync(RegenerateAllCommand command, CancellationToken cancellationToken = default)
    {
        var failures = new List<GenerationFailure>();
        int tables = 0, reports = 0, roles = 0;

        foreach (var assetTypeId in await procedures.GetAssetTypeIdsAsync(cancellationToken))
            tables += await TryAsync("AssetType", assetTypeId, () => genTables.GenerateAsync(assetTypeId, cancellationToken), failures);

        foreach (var reportId in await procedures.GetReportIdsAsync(cancellationToken))
            reports += await TryAsync("Report", reportId, async () => await reportViews.GenerateAsync(reportId, cancellationToken) ? 1 : 0, failures);

        foreach (var roleId in await procedures.GetRoleIdsAsync(cancellationToken))
            roles += await TryAsync("Role", roleId, () => roleViews.GenerateAsync(roleId, cancellationToken), failures);

        logger.LogInformation("Regenerated {Tables} gen tables, {Reports} report views and {Roles} role views; {Failures} failure(s).",
            tables, reports, roles, failures.Count);

        return new Success<RegenerateAllResult>(new RegenerateAllResult(tables, reports, roles, failures));
    }

    private async Task<int> TryAsync(string kind, Guid id, Func<Task<int>> generate, List<GenerationFailure> failures)
    {
        try
        {
            return await generate();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Generating objects for {Kind} {Id} failed", kind, id);
            failures.Add(new GenerationFailure(kind, id, ex.Message));
            return 0;
        }
    }
}
