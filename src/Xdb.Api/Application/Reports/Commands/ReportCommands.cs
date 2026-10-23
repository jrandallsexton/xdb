using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Xdb.Api.Application.Common.Legacy;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Metadata.Queries.Lookups;
using Xdb.Api.Application.Reports.Legacy;
using Xdb.Api.Application.Views.Legacy;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Reports.Commands;

public sealed record SaveReportCommand(ReportContract? Report);

public interface ISaveReportCommandHandler
{
    Task<Result<bool>> ExecuteAsync(SaveReportCommand command, CancellationToken cancellationToken = default);
}

/// <summary>Report_Save: <see cref="ReportSaver"/> over the posted report (a null body fails).</summary>
public sealed class SaveReportCommandHandler(ReportSaver saver) : ISaveReportCommandHandler
{
    public async Task<Result<bool>> ExecuteAsync(SaveReportCommand command, CancellationToken cancellationToken = default) =>
        new Success<bool>(await saver.SaveAsync(command.Report ?? throw new NullReferenceException("report is null."), cancellationToken));
}

public sealed record CreateReportFromMemberFilterCommand(string? FilterId);

public interface ICreateReportFromMemberFilterCommandHandler
{
    Task<Result<bool>> ExecuteAsync(CreateReportFromMemberFilterCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Report_CreateFromMemberFilter: a quick filter becomes a report with the same id: its filters ("=" operators), an
/// Asset Name column, then every property of the view's groups in order; saved as new (a failed view kills it, even an
/// existing one); then the owner is marked updated. Logical failures are not caught here: they fault.
/// </summary>
public sealed class CreateReportFromMemberFilterCommandHandler(
    IFilterProcedures filters, IViewProcedures views, ReportSaver saver, IDashboardProcedures members, IDateTimeProvider dateTimeProvider)
    : ICreateReportFromMemberFilterCommandHandler
{
    private const int MaxOperatorId = 6;

    public async Task<Result<bool>> ExecuteAsync(CreateReportFromMemberFilterCommand command, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(command.FilterId, out var filterId))
            throw new InvalidOperationException("Invalid filter id");
        var memberFilter = await filters.GetMemberFilterAsync(filterId, cancellationToken) ?? throw new InvalidOperationException("Invalid QuickFilter");
        var view = await views.GetViewAsync(memberFilter.ViewId, cancellationToken) ?? throw new NullReferenceException("View not found.");

        var report = new ReportContract
        {
            AssetTypeId = view.AssetTypeId,
            IsInstance = view.IsInstance ?? throw new InvalidCastException("View IsInstance is NULL."),
            Created = dateTimeProvider.UtcNow().ToLocalTime(),
            CreatedBy = memberFilter.MemberId,
            Id = filterId,
            FilterLogic = memberFilter.FilterExpression,
            IsNew = true,
            ReportName = memberFilter.Name
        };
        foreach (var item in memberFilter.Filters)
        {
            if (item.OperatorId is < 0 or > MaxOperatorId)
                throw new InvalidOperationException("Invalid value for EFilterOperator");
            report.Filters!.Add(new FilterContract { Id = Guid.NewGuid(), PropertyId = item.PropertyId, Property = string.Empty, OperatorId = item.OperatorId, Operator = "=", Value = item.Value, Order = item.Order });
        }
        report.ReportProperties!.Add(new ReportPropertyContract { Id = Guid.NewGuid(), PropertyId = SystemPropertyIds.AssetName, PropertyName = "Name" });
        var index = 0;
        foreach (var group in await views.GetViewPropertyGroupsAsync(memberFilter.ViewId, cancellationToken))
        {
            foreach (var property in await views.GetPropertyGroupPropertiesAsync(group.Id, cancellationToken))
            {
                report.ReportProperties.Add(new ReportPropertyContract
                {
                    Id = Guid.NewGuid(), PropertyId = property.Id, PropertyName = string.IsNullOrEmpty(property.DisplayValue) ? property.Name : property.DisplayValue, Index = index
                });
                index++;
            }
        }

        if (!await saver.SaveAsync(report, cancellationToken))
            return new Success<bool>(false);
        await members.MarkMemberUpdatedAsync(report.CreatedBy, report.CreatedBy, cancellationToken);
        return new Success<bool>(true);
    }
}

public sealed record DeleteReportCommand(Guid ReportId, Guid UserId);

public interface IDeleteReportCommandHandler
{
    Task<Result<bool>> ExecuteAsync(DeleteReportCommand command, CancellationToken cancellationToken = default);
}

/// <summary>Report_Delete: an administrator or the report's creator soft-deletes it (a full load first; others fault).</summary>
public sealed class DeleteReportCommandHandler(ReportLoader loader, IReportStore store, IMetadataProcedures metadata) : IDeleteReportCommandHandler
{
    public async Task<Result<bool>> ExecuteAsync(DeleteReportCommand command, CancellationToken cancellationToken = default)
    {
        var report = (await loader.LoadAsync(command.ReportId, cancellationToken)).Report;
        if (!await metadata.IsAdminAsync(command.UserId, SystemRoleIds.Admin, cancellationToken) && report.CreatedBy != command.UserId)
            throw new InvalidOperationException("User cannot delete this report.  User did not create this report.");
        await store.DeleteAsync(command.ReportId, command.UserId, cancellationToken);
        return new Success<bool>(true);
    }
}

public sealed record KillReportCommand(Guid ReportId, Guid UserId);

public interface IKillReportCommandHandler
{
    Task<Result<bool>> ExecuteAsync(KillReportCommand command, CancellationToken cancellationToken = default);
}

/// <summary>Report_Kill: an administrator or the creator (no load: an unknown report passes for the empty user) hard-deletes it.</summary>
public sealed class KillReportCommandHandler(IReportStore store, IMetadataProcedures metadata) : IKillReportCommandHandler
{
    public async Task<Result<bool>> ExecuteAsync(KillReportCommand command, CancellationToken cancellationToken = default)
    {
        if (!await metadata.IsAdminAsync(command.UserId, SystemRoleIds.Admin, cancellationToken)
            && await store.GetCreatedByAsync(command.ReportId, cancellationToken) != command.UserId)
            return new Success<bool>(false);
        await store.KillAsync(command.ReportId, cancellationToken);
        return new Success<bool>(true);
    }
}

public sealed record MigrateReportCommand(Guid ReportId, int Target);

public interface IMigrateReportCommandHandler
{
    Task<Result<bool>> ExecuteAsync(MigrateReportCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// MigrateReport (stub; the targets are other environments' databases): to the configured own instance it re-saves the
/// loaded report as new, as the original did; to any other target the report is loaded (an unknown one fails first)
/// and the call faults without writing, as the original did when the target was unreachable.
/// </summary>
public sealed class MigrateReportCommandHandler(ReportLoader loader, ReportSaver saver, IOptions<LegacyEnvironmentOptions> environment) : IMigrateReportCommandHandler
{
    public async Task<Result<bool>> ExecuteAsync(MigrateReportCommand command, CancellationToken cancellationToken = default)
    {
        var report = (await loader.LoadAsync(command.ReportId, cancellationToken)).Report;
        if (!MigrateBody.IsOwnInstance(command.Target, environment.Value.ApplicationInstance))
            throw new InvalidOperationException("The target instance is not available.");
        report.IsNew = true;
        return new Success<bool>(await saver.SaveAsync(report, cancellationToken));
    }
}

public sealed record AddReportBookmarkCommand(Guid UserId, Guid ReportId, bool ShowOnHome);

public interface IReportBookmarkCommandHandler
{
    Task<Result<bool>> AddAsync(AddReportBookmarkCommand command, CancellationToken cancellationToken = default);
    Task<Result<bool>> RemoveAsync(Guid userId, Guid reportId, CancellationToken cancellationToken = default);
    Task<Result<bool>> ClearCacheAsync(Guid reportId, CancellationToken cancellationToken = default);
}

/// <summary>Bookmark_AddForReport (once per member and report), Bookmark_RemoveForReport, Report_ClearCached: always true.</summary>
public sealed class ReportBookmarkCommandHandler(IReportStore store) : IReportBookmarkCommandHandler
{
    public async Task<Result<bool>> AddAsync(AddReportBookmarkCommand command, CancellationToken cancellationToken = default)
    {
        if (await store.CountBookmarksAsync(command.UserId, command.ReportId, cancellationToken) == 0)
            await store.AddBookmarkAsync(command.UserId, command.ReportId, command.ShowOnHome, cancellationToken);
        return new Success<bool>(true);
    }

    public async Task<Result<bool>> RemoveAsync(Guid userId, Guid reportId, CancellationToken cancellationToken = default)
    {
        await store.RemoveBookmarkAsync(userId, reportId, cancellationToken);
        return new Success<bool>(true);
    }

    public async Task<Result<bool>> ClearCacheAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        await store.ClearCacheAsync(reportId, cancellationToken);
        return new Success<bool>(true);
    }
}
