using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Common.Identity;
using Xdb.Api.Application.Common.Legacy;
using Xdb.Api.Application.Metadata.Commands;
using Xdb.Core.Common;
using Xdb.Core.LegacyWire;

namespace Xdb.Api.Application.Metadata.Legacy;

/// <summary>
/// The original service's asset type, property and picklist factories, saves and Migrate stubs (spec M6-M). Saves and
/// migrations answer logical failures as the original's HandleLogicalException did.
/// </summary>
[Route(LegacyWire.BaseRoute)]
public sealed class MetadataWriteWireController : LegacyWireControllerBase
{
    // ---- Factories: new objects, nothing read or written ----

    [HttpGet("AssetType_Create")]
    public IActionResult AssetTypeCreate([FromServices] IDateTimeProvider clock, [FromQuery] Guid userId) =>
        LegacyWire.Typed(new AssetTypeContract
        {
            AllowAssets = true, Created = clock.UtcNow().ToLocalTime(), CreatedBy = userId, Id = Guid.NewGuid(), IsDirty = true, IsNew = true
        });

    [HttpGet("Property_Create")]
    public IActionResult PropertyCreate([FromServices] IDateTimeProvider clock, [FromQuery] Guid userId) =>
        LegacyWire.Typed(new PropertyContract
        {
            Id = Guid.NewGuid(), Created = clock.UtcNow().ToLocalTime(), CreatedBy = userId, IsNew = true, IsDirty = true, AssetTypeIsInstance = false
        });

    [HttpGet("PickList_Create")]
    public IActionResult PickListCreate([FromServices] IDateTimeProvider clock, [FromQuery] Guid userId)
    {
        var created = clock.UtcNow().ToLocalTime();
        return LegacyWire.Typed(new PickListContract
        {
            Id = Guid.NewGuid(), Created = created, CreatedBy = userId, Approved = created, ApprovedBy = userId, IsDirty = true, IsNew = true
        });
    }

    // ---- Saves ----

    [HttpPost("AssetType_Save")]
    public async Task<IActionResult> AssetTypeSave([FromServices] DcjsReader reader, [FromServices] IMetadataSaveCommandHandler handler,
        [FromServices] LegacyErrorLog errors, CancellationToken cancellationToken)
    {
        var (body, error) = await reader.ReadBodyAsync<AssetTypeContract>(Request, cancellationToken);
        return error ?? await errors.HandleAsync(() => handler.SaveAssetTypeAsync(body, cancellationToken), cancellationToken);
    }

    [HttpPost("Property_Save")]
    public async Task<IActionResult> PropertySave([FromServices] DcjsReader reader, [FromServices] IMetadataSaveCommandHandler handler,
        [FromServices] LegacyErrorLog errors, CancellationToken cancellationToken)
    {
        var (body, error) = await reader.ReadBodyAsync<PropertyContract>(Request, cancellationToken);
        return error ?? await errors.HandleAsync(() => handler.SavePropertyAsync(body, cancellationToken), cancellationToken);
    }

    [HttpPost("PickList_Save")]
    public async Task<IActionResult> PickListSave([FromServices] DcjsReader reader, [FromServices] IMetadataSaveCommandHandler handler,
        [FromServices] LegacyErrorLog errors, CancellationToken cancellationToken)
    {
        var (body, error) = await reader.ReadBodyAsync<PickListContract>(Request, cancellationToken);
        return error ?? await errors.HandleAsync(() => handler.SavePickListAsync(body, cancellationToken), cancellationToken);
    }

    // ---- Migrate stubs ----

    [HttpPost("MigrateAssetType")]
    public Task<IActionResult> MigrateAssetType([FromServices] IMetadataMigrateCommandHandler handler, [FromServices] LegacyErrorLog errors,
        CancellationToken cancellationToken) =>
        MigrateAsync(errors, (key, target) => handler.MigrateAssetTypeAsync(key, target, cancellationToken), cancellationToken);

    [HttpPost("MigratePicklist")]
    public Task<IActionResult> MigratePicklist([FromServices] IMetadataMigrateCommandHandler handler, [FromServices] LegacyErrorLog errors,
        CancellationToken cancellationToken) =>
        MigrateAsync(errors, (key, target) => handler.MigratePickListAsync(key, target, cancellationToken), cancellationToken);

    [HttpPost("MigrateProperty")]
    public Task<IActionResult> MigrateProperty([FromServices] IMetadataMigrateCommandHandler handler, [FromServices] LegacyErrorLog errors,
        CancellationToken cancellationToken) =>
        MigrateAsync(errors, (key, target) => handler.MigratePropertyAsync(key, target, cancellationToken), cancellationToken);

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
}
