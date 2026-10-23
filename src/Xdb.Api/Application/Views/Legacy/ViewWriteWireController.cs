using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Common.Identity;
using Xdb.Api.Application.Common.Legacy;
using Xdb.Api.Application.Views.Commands;
using Xdb.Core.Common;
using Xdb.Core.LegacyWire;

namespace Xdb.Api.Application.Views.Legacy;

/// <summary>
/// The original service's property group and view factories, clones, saves, kills, quick-filter writes and Migrate
/// stubs (spec M6-M). Only the saves, View_Kill and the migrations catch logical failures; the rest fault.
/// </summary>
[Route(LegacyWire.BaseRoute)]
public sealed class ViewWriteWireController : LegacyWireControllerBase
{
    // ---- Factories and clones ----

    [HttpGet("PropertyGroup_Create")]
    public IActionResult PropertyGroupCreate([FromServices] IDateTimeProvider clock, [FromQuery] Guid userId) =>
        LegacyWire.Typed(new PropertyGroupContract { Id = Guid.NewGuid(), Created = clock.UtcNow().ToLocalTime(), CreatedBy = userId, IsNew = true, IsDirty = true });

    [HttpGet("View_Create")]
    public IActionResult ViewCreate([FromServices] IDateTimeProvider clock, [FromQuery] Guid userId) =>
        LegacyWire.Typed(new ViewContract
        {
            Id = Guid.NewGuid(), AllowCloning = false, Created = clock.UtcNow().ToLocalTime(), CreatedBy = userId, IsNew = true, IsDirty = true
        });

    [HttpGet("PropertyGroup_Clone")]
    public async Task<IActionResult> PropertyGroupClone([FromServices] IViewCloneQueryHandler handler, [FromQuery] Guid propertyGroupId, [FromQuery] Guid userId,
        CancellationToken cancellationToken) =>
        (await handler.ClonePropertyGroupAsync(propertyGroupId, userId, cancellationToken))
            .ToLegacyResult(group => group is null ? LegacyWire.NullTyped() : LegacyWire.Typed(group));

    [HttpGet("View_Clone")]
    public async Task<IActionResult> ViewClone([FromServices] IViewCloneQueryHandler handler, [FromQuery] Guid viewId, [FromQuery] Guid userId,
        CancellationToken cancellationToken) =>
        (await handler.CloneViewAsync(viewId, userId, cancellationToken)).ToLegacyResult(view => LegacyWire.Typed(view));

    // ---- Saves and kills ----

    [HttpPost("PropertyGroup_Save")]
    public async Task<IActionResult> PropertyGroupSave([FromServices] DcjsReader reader, [FromServices] IViewCommandHandler handler,
        [FromServices] LegacyErrorLog errors, CancellationToken cancellationToken)
    {
        var (body, error) = await reader.ReadBodyAsync<PropertyGroupContract>(Request, cancellationToken);
        return error ?? await errors.HandleAsync(() => handler.SavePropertyGroupAsync(body, cancellationToken), cancellationToken);
    }

    [HttpGet("PropertyGroup_Kill")]
    public async Task<IActionResult> PropertyGroupKill([FromServices] IViewCommandHandler handler, [FromQuery] Guid propertyGroupId, [FromQuery] Guid userId,
        CancellationToken cancellationToken) =>
        (await handler.KillPropertyGroupAsync(propertyGroupId, userId, cancellationToken)).ToLegacyResult(ok => LegacyWire.Typed(ok));

    [HttpPost("View_Save")]
    public async Task<IActionResult> ViewSave([FromServices] DcjsReader reader, [FromServices] IViewCommandHandler handler,
        [FromServices] LegacyErrorLog errors, CancellationToken cancellationToken)
    {
        var (body, error) = await reader.ReadBodyAsync<ViewContract>(Request, cancellationToken);
        return error ?? await errors.HandleAsync(() => handler.SaveViewAsync(body, cancellationToken), cancellationToken);
    }

    [HttpGet("View_Kill")]
    public Task<IActionResult> ViewKill([FromServices] IViewCommandHandler handler, [FromServices] LegacyErrorLog errors, [FromQuery] Guid viewId,
        [FromQuery] Guid userId, CancellationToken cancellationToken) =>
        errors.HandleAsync(() => handler.KillViewAsync(viewId, userId, cancellationToken), cancellationToken);

    // ---- Quick filters ----

    /// <summary>
    /// The body is the asset search client (M4 binding): assetTypeId, assetRequestType, filterName, filterExpression and
    /// filters required, every filter member required; id, userId and viewId optional.
    /// </summary>
    [HttpPost("ViewFilter_Save")]
    public async Task<IActionResult> ViewFilterSave([FromServices] IViewCommandHandler handler, CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken);
        if (error is not null)
            return error;
        if (root is null)
            return LegacyWire.Fault();
        SaveQuickFilterCommand command;
        try
        {
            command = QuickFilter(root.Value);
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        return (await handler.SaveQuickFilterAsync(command, cancellationToken)).ToLegacyResult(ok => LegacyWire.Typed(ok));
    }

    [HttpGet("ViewFilter_Delete")]
    public async Task<IActionResult> ViewFilterDelete([FromServices] IViewCommandHandler handler, [FromQuery] string? viewFilterId,
        CancellationToken cancellationToken) =>
        (await handler.DeleteQuickFilterAsync(viewFilterId, cancellationToken)).ToLegacyResult(ok => LegacyWire.Typed(ok));

    // ---- Migrate stubs ----

    [HttpPost("MigratePropertyGroup")]
    public Task<IActionResult> MigratePropertyGroup([FromServices] IViewMigrateCommandHandler handler, [FromServices] LegacyErrorLog errors,
        CancellationToken cancellationToken) =>
        MigrateAsync(errors, (key, target) => handler.MigratePropertyGroupAsync(key, target, cancellationToken), cancellationToken);

    [HttpPost("MigrateView")]
    public Task<IActionResult> MigrateView([FromServices] IViewMigrateCommandHandler handler, [FromServices] LegacyErrorLog errors,
        CancellationToken cancellationToken) =>
        MigrateAsync(errors, (key, target) => handler.MigrateViewAsync(key, target, cancellationToken), cancellationToken);

    private async Task<IActionResult> MigrateAsync(LegacyErrorLog errors, Func<Guid, int, Task<Result<bool>>> run, CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken);
        if (error is not null)
            return error;
        Guid key;
        int target;
        try
        {
            (key, target) = MigrateBody.Read(root);
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        return await errors.HandleAsync(() => run(key, target), cancellationToken);
    }

    private static readonly string[] SearchClientMembers = ["assetTypeId", "assetRequestType", "filterName", "filterExpression", "filters"];
    private static readonly string[] FilterClientMembers = ["propertyId", "operatorId", "value", "order"];

    private static SaveQuickFilterCommand QuickFilter(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object || SearchClientMembers.Any(name => !root.TryGetProperty(name, out _)))
            throw new LegacyBodyException("Missing a required member.");
        var filters = root.GetProperty("filters");
        List<QuickFilterItem>? items = null;
        if (filters.ValueKind == JsonValueKind.Array)
        {
            items = [];
            foreach (var filter in filters.EnumerateArray())
            {
                if (filter.ValueKind != JsonValueKind.Object || FilterClientMembers.Any(name => !filter.TryGetProperty(name, out _)))
                    throw new LegacyBodyException("Missing a required member.");
                items.Add(new QuickFilterItem(LegacyJsonBody.Text(filter.GetProperty("propertyId")), LegacyJsonBody.Int(filter.GetProperty("operatorId")),
                    LegacyJsonBody.Text(filter.GetProperty("value")), LegacyJsonBody.Int(filter.GetProperty("order"))));
            }
        }
        else if (filters.ValueKind != JsonValueKind.Null)
        {
            throw new LegacyBodyException("Not a list.");
        }

        return new SaveQuickFilterCommand(Optional(root, "id"), Optional(root, "userId"), Optional(root, "viewId"), LegacyJsonBody.Text(root.GetProperty("filterName")),
            LegacyJsonBody.Text(root.GetProperty("filterExpression")), items);

        static string? Optional(JsonElement element, string name) => element.TryGetProperty(name, out var value) ? LegacyJsonBody.Text(value) : null;
    }
}
