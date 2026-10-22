using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Common.Identity;
using Xdb.Api.Application.Roles.Commands;
using Xdb.Api.Application.Roles.Queries;
using Xdb.Api.Application.Views.Legacy;
using Xdb.Core.Common;
using Xdb.Core.LegacyWire;

namespace Xdb.Api.Application.Roles.Legacy;

/// <summary>
/// The original service's role administration operations, byte-compatible for the original UI. The operations that
/// caught a LogicalException answer it as the original's HandleLogicalException did (an Errors row, then 500 with an
/// ErrorId header and false).
/// </summary>
[Route(LegacyWire.BaseRoute)]
public sealed class RolesWireController : LegacyWireControllerBase
{
    // ---- Reads ----

    [HttpGet("Roles_GetDictionary")]
    public async Task<IActionResult> RolesGetDictionary([FromServices] IGetRolesDictionaryQueryHandler handler, [FromQuery] Guid userId,
        [FromQuery] bool includeDeleted, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetRolesDictionaryQuery(includeDeleted), cancellationToken))
            .ToLegacyResult(values => LegacyWire.JsonStream(LegacyFormatters.FormatDictionary(values)));

    [HttpGet("Role_Get")]
    public async Task<IActionResult> RoleGet([FromServices] IGetRoleQueryHandler handler, [FromQuery] Guid roleId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetRoleQuery(roleId), cancellationToken)).ToLegacyResult(LegacyWire.TypedOrNull);

    [HttpGet("Users_GetForRole")]
    public async Task<IActionResult> UsersGetForRole([FromServices] IGetRoleMembersQueryHandler handler, [FromQuery] Guid roleId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetRoleMembersQuery(roleId, InRole: true), cancellationToken)).ToLegacyResult(Html);

    [HttpGet("Users_GetNotInRole")]
    public async Task<IActionResult> UsersGetNotInRole([FromServices] IGetRoleMembersQueryHandler handler, [FromQuery] Guid roleId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetRoleMembersQuery(roleId, InRole: false), cancellationToken)).ToLegacyResult(Html);

    [HttpGet("Reports_GetForRole")]
    public async Task<IActionResult> ReportsGetForRole([FromServices] IGetRoleReportsQueryHandler handler, [FromQuery] Guid roleId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetRoleReportsQuery(roleId, InRole: true), cancellationToken)).ToLegacyResult(Html);

    [HttpGet("Reports_GetNotInRole")]
    public async Task<IActionResult> ReportsGetNotInRole([FromServices] IGetRoleReportsQueryHandler handler, [FromQuery] Guid roleId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetRoleReportsQuery(roleId, InRole: false), cancellationToken)).ToLegacyResult(Html);

    [HttpGet("UserIds_GetByRole")]
    public async Task<IActionResult> UserIdsGetByRole([FromServices] IGetRoleUserIdsQueryHandler handler, [FromQuery] Guid roleId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new GetRoleUserIdsQuery(roleId), cancellationToken)).ToLegacyResult(values => Html(values));

    /// <summary>Dead in the deployed release: always an empty list.</summary>
    [HttpGet("Processes_GetByRoleId")]
    public IActionResult ProcessesGetByRoleId([FromQuery] Guid roleId) => LegacyWire.Typed(new List<RoleProcessRelationContract>());

    // ---- Client-object factories (no writes) ----

    [HttpGet("Role_Create")]
    public IActionResult RoleCreate([FromServices] IDateTimeProvider dateTimeProvider, [FromQuery] Guid userId) =>
        LegacyWire.Typed(new RoleContract { Id = Guid.NewGuid(), Created = dateTimeProvider.UtcNow().ToLocalTime(), CreatedBy = userId });

    [HttpGet("Role_Clone")]
    public async Task<IActionResult> RoleClone([FromServices] ICloneRoleQueryHandler handler, [FromQuery] Guid roleId, [FromQuery] Guid userId,
        CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new CloneRoleQuery(roleId, userId), cancellationToken)).ToLegacyResult(LegacyWire.TypedOrNull);

    [HttpGet("RoleViewRelationList_Clone")]
    public async Task<IActionResult> RoleViewRelationListClone([FromServices] ICloneRoleViewRelationsQueryHandler handler, [FromQuery] Guid srcRoleId,
        [FromQuery] Guid newRoleId, [FromQuery] Guid userId, CancellationToken cancellationToken) =>
        (await handler.ExecuteAsync(new CloneRoleViewRelationsQuery(srcRoleId, newRoleId, userId), cancellationToken)).ToLegacyResult(list => LegacyWire.Typed(list));

    // ---- Writes ----

    [HttpPost("Role_Save")]
    public async Task<IActionResult> RoleSave([FromServices] ISaveRoleCommandHandler handler, [FromServices] LegacyErrorLog errors, CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken);
        if (error is not null)
            return error;
        RoleContract? role;
        try
        {
            role = RoleBodies.Role(root);
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        return await LogicalAsync(errors, async () => await handler.ExecuteAsync(new SaveRoleCommand(role), cancellationToken), cancellationToken);
    }

    [HttpPost("Role_SaveViewRelations")]
    public async Task<IActionResult> RoleSaveViewRelations([FromServices] ISaveRoleViewRelationsCommandHandler handler, [FromServices] LegacyErrorLog errors,
        CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken);
        if (error is not null)
            return error;
        List<RoleViewRelationContract>? relations;
        try
        {
            relations = RoleBodies.ViewRelations(root);
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        return await LogicalAsync(errors, async () => await handler.ExecuteAsync(new SaveRoleViewRelationsCommand(relations), cancellationToken), cancellationToken);
    }

    [HttpPost("Role_SaveProcessRelations")]
    public async Task<IActionResult> RoleSaveProcessRelations([FromServices] ISaveRoleProcessRelationsCommandHandler handler, [FromServices] LegacyErrorLog errors,
        CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken);
        if (error is not null)
            return error;
        List<RoleProcessRelationContract>? relations;
        try
        {
            relations = RoleBodies.ProcessRelations(root);
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        return await LogicalAsync(errors, async () => await handler.ExecuteAsync(new SaveRoleProcessRelationsCommand(relations), cancellationToken), cancellationToken);
    }

    [HttpGet("Role_Kill")]
    public async Task<IActionResult> RoleKill([FromServices] IKillRoleCommandHandler handler, [FromServices] LegacyErrorLog errors, [FromQuery] Guid roleId,
        [FromQuery] Guid userId, CancellationToken cancellationToken) =>
        await LogicalAsync(errors, async () => await handler.ExecuteAsync(new KillRoleCommand(roleId, userId), cancellationToken), cancellationToken);

    /// <summary>HandleLogicalException: a logical failure saves an Errors row and answers 500 with its id; anything else faults.</summary>
    private static async Task<IActionResult> LogicalAsync(LegacyErrorLog errors, Func<Task<Result<bool>>> run, CancellationToken cancellationToken)
    {
        try
        {
            return (await run()).ToLegacyResult(ok => LegacyWire.Typed(ok));
        }
        catch (LegacyLogicalException e)
        {
            return LegacyWire.LogicalFailure(await errors.SaveAsync(e.Message, cancellationToken));
        }
    }

    private static IActionResult Html(IReadOnlyList<KeyValuePair<Guid, string>> values) => LegacyWire.HtmlStream(LegacyFormatters.FormatDictionary(values));
}
