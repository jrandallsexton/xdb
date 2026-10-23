using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Common.Legacy;
using Xdb.Api.Application.Views.Legacy;
using Xdb.Core.LegacyWire;

namespace Xdb.Api.Application.Admin.Legacy;

/// <summary>
/// The original service's administration operations (spec M9): environment list, help sections, service-module
/// metadata, and the Windows-only operations (deferred: they fault as the original did for every input they answer
/// here). Operations of the original without a client are not routed: the unknown-operation page answers them.
/// </summary>
[Route(LegacyWire.BaseRoute)]
public sealed class AdminWireController : LegacyWireControllerBase
{
    [HttpGet("ApplicationInstances")]
    public IActionResult ApplicationInstances([FromServices] IAdminQueryHandler handler) =>
        LegacyWire.HtmlStream(LegacyFormatters.FormatDictionary(handler.ApplicationInstances()));

    [HttpGet("HelpSection_Create")]
    public async Task<IActionResult> HelpSectionCreate([FromServices] IAdminQueryHandler handler, [FromQuery] Guid userId, CancellationToken cancellationToken) =>
        (await handler.CreateHelpSectionAsync(userId, cancellationToken)).ToLegacyResult(LegacyWire.TypedOrNull);

    [HttpPost("HelpSection_Save")]
    public async Task<IActionResult> HelpSectionSave([FromServices] DcjsReader reader, [FromServices] IAdminCommandHandler handler, CancellationToken cancellationToken)
    {
        var (body, error) = await reader.ReadBodyAsync<HelpSectionContract>(Request, cancellationToken);
        if (error is not null)
            return error;
        return (await handler.SaveHelpSectionAsync(body, cancellationToken)).ToLegacyResult(done => LegacyWire.Typed(done));
    }

    /// <summary>userId is bound and unused.</summary>
    [HttpGet("ServiceModule_Create")]
    public IActionResult ServiceModuleCreate([FromServices] IAdminQueryHandler handler, [FromQuery] Guid userId) =>
        handler.CreateServiceModule().ToLegacyResult(module => LegacyWire.Typed(module));

    [HttpGet("ServiceModule_Get")]
    public async Task<IActionResult> ServiceModuleGet([FromServices] IAdminQueryHandler handler, [FromQuery] Guid moduleId, CancellationToken cancellationToken) =>
        (await handler.GetServiceModuleAsync(moduleId, cancellationToken)).ToLegacyResult(module => LegacyWire.Typed(module));

    [HttpPost("ServiceModule_Save")]
    public async Task<IActionResult> ServiceModuleSave([FromServices] DcjsReader reader, [FromServices] IAdminCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var (body, error) = await reader.ReadBodyAsync<ServiceModuleMetadataContract>(Request, cancellationToken);
        if (error is not null)
            return error;
        return (await handler.SaveServiceModuleAsync(body, cancellationToken)).ToLegacyResult(done => LegacyWire.Typed(done));
    }

    [HttpGet("ServiceModules_GetDictionary")]
    public async Task<IActionResult> ServiceModulesGetDictionary([FromServices] IAdminQueryHandler handler, CancellationToken cancellationToken) =>
        (await handler.GetServiceModulesAsync(cancellationToken)).ToLegacyResult(modules => LegacyWire.HtmlStream(LegacyFormatters.FormatDictionary(modules)));

    /// <summary>The module read always returns an object, so the original's insert branch never ran: read (it may fault), then true. moduleName is unused.</summary>
    [HttpGet("prepareUploadObjectServiceModule")]
    public async Task<IActionResult> PrepareUploadObjectServiceModule([FromServices] IAdminQueryHandler handler, [FromQuery] Guid moduleId,
        [FromQuery] string? moduleName, CancellationToken cancellationToken) =>
        (await handler.GetServiceModuleAsync(moduleId, cancellationToken)).ToLegacyResult(_ => LegacyWire.Typed(true));

    [HttpGet("WmiQueryableObjects")]
    public IActionResult WmiQueryableObjects([FromServices] IAdminQueryHandler handler) => LegacyWire.Typed(handler.WmiQueryableObjects());

    /// <summary>DEFERRED (D2, Windows-only): faults, as the original did for every query type it rejected before connecting.</summary>
    [HttpGet("WmiQuery")]
    public IActionResult WmiQuery([FromQuery] string? queryType, [FromQuery] string? user, [FromQuery] string? pass, [FromQuery] string? domain,
        [FromQuery] string? machine) =>
        throw DeferredAdmin.WmiQuery();

    /// <summary>DEFERRED (D1, Windows-only): faults, as the original did on a host without the service.</summary>
    [HttpGet("WindowsService_GetStatus")]
    public IActionResult WindowsServiceGetStatus() => throw DeferredAdmin.ServiceStatus();

    /// <summary>
    /// DEFERRED (D3): the wrapped body is read as the original read it (its binding answers stay identical), then the
    /// call faults, as the original did for every message it could not address.
    /// </summary>
    [HttpPost("SendMail")]
    public async Task<IActionResult> SendMail(CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken, wrapped: true);
        if (error is not null)
            return error;
        // An empty body was the request-error page too (measured).
        if (root is null || root.Value.ValueKind != JsonValueKind.Object)
            return LegacyWire.RequestError();
        throw DeferredAdmin.SendMail();
    }
}
