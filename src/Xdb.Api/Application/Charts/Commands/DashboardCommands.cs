using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Common.Identity;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Charts.Commands;

public sealed record SaveDashboardCommand(DashboardBody? Dashboard);

public interface ISaveDashboardCommandHandler
{
    Task<Result<bool>> ExecuteAsync(SaveDashboardCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Dashboard_Save (UILayer.Dashboard_Save): converts the client (item titles and subtitles are not copied), marks the
/// member modified and saves it through spr_Member_Save, then saves the dashboard. The member save is called exactly
/// as the original called it; whatever that procedure does (it inserts unconditionally after its update) is the result.
/// </summary>
public sealed class SaveDashboardCommandHandler(IDashboardProcedures dashboards) : ISaveDashboardCommandHandler
{
    public async Task<Result<bool>> ExecuteAsync(SaveDashboardCommand command, CancellationToken cancellationToken = default) =>
        new Success<bool>(await SaveAsync(command.Dashboard ?? throw new NullReferenceException("dash is null."), cancellationToken));

    internal async Task<bool> SaveAsync(DashboardBody dash, CancellationToken cancellationToken)
    {
        var id = new Guid(dash.Id!);
        var memberId = new Guid(dash.MemberId!);
        var items = new List<DashboardItemRow>();
        foreach (var item in dash.Items ?? throw new NullReferenceException("items is null."))
            items.Add(new DashboardItemRow(new Guid(item.Id!), item.Index, item.ItemType, new Guid(item.ChartId!), null, null));

        var member = await dashboards.GetMemberAsync(memberId, cancellationToken) ?? throw new NullReferenceException("Member not found.");
        // MemberLayer.Save validates before the DAL save.
        if (string.IsNullOrEmpty(member.UserId))
            throw new InvalidOperationException("UserId cannot be null");
        if (!await dashboards.IsLiveMemberAsync(member.CreatedBy, cancellationToken))
            throw new InvalidOperationException("Invalid user id");
        await dashboards.SaveMemberAsync(member, cancellationToken);

        await dashboards.SaveDashboardAsync(new DashboardRow(id, dash.Caption, dash.Index, items), memberId, cancellationToken);
        return true;
    }
}

public sealed record AddDashboardItemCommand(DashboardItemBody? Item);

public interface IAddDashboardItemCommandHandler
{
    Task<Result<bool>> ExecuteAsync(AddDashboardItemCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// DashboardItem_Add: appends the item to an existing dashboard (false when there is none), marks the caller updated,
/// then saves the dashboard as the caller's (the dashboard's owner is not checked).
/// </summary>
public sealed class AddDashboardItemCommandHandler(IDashboardProcedures dashboards, SaveDashboardCommandHandler saver, CurrentMember currentMember)
    : IAddDashboardItemCommandHandler
{
    public async Task<Result<bool>> ExecuteAsync(AddDashboardItemCommand command, CancellationToken cancellationToken = default)
    {
        var memberId = await currentMember.GetIdAsync(cancellationToken);
        var item = command.Item ?? throw new NullReferenceException("item is null.");
        if (!Guid.TryParse(item.DashboardId, out var dashboardId))
            throw new InvalidOperationException("Invalid dashboard id.");
        if (await dashboards.GetDashboardAsync(dashboardId, cancellationToken) is not { } dashboard)
            return new Success<bool>(false);

        var items = dashboard.Items
            .Select(i => new DashboardItemBody(i.Id.ToString("D"), dashboard.Id.ToString("D"), i.Index, i.ItemTypeId, i.ChartId.ToString("D"), i.Title, i.Subtitle))
            .ToList();
        items.Add(item with { Index = items.Count });
        await dashboards.MarkMemberUpdatedAsync(memberId, memberId, cancellationToken);
        var client = new DashboardBody(dashboard.Id.ToString("D"), dashboard.Caption, dashboard.Index, items, memberId.ToString("D"));
        return new Success<bool>(await saver.SaveAsync(client, cancellationToken));
    }
}

public sealed record SetMappingCommand(string? Key, string? Value);

public interface ISetMappingCommandHandler
{
    Task<Result<bool>> ExecuteAsync(SetMappingCommand command, CancellationToken cancellationToken = default);
}

/// <summary>Mapping_Set: sets a mapping's value when both are Guids (true whatever the row count), else false.</summary>
public sealed class SetMappingCommandHandler(IDashboardProcedures dashboards) : ISetMappingCommandHandler
{
    public async Task<Result<bool>> ExecuteAsync(SetMappingCommand command, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(command.Key, out var id) || !Guid.TryParse(command.Value, out var value))
            return new Success<bool>(false);
        await dashboards.SetMappingAsync(id, value, cancellationToken);
        return new Success<bool>(true);
    }
}
