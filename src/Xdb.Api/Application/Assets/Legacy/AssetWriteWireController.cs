using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Xdb.Api.Application.Assets.Writes;
using Xdb.Api.Application.Common.Identity;
using Xdb.Api.Application.Common.Legacy;
using Xdb.Api.Application.Roles.Legacy;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.LegacyWire;

namespace Xdb.Api.Application.Assets.Legacy;

/// <summary>
/// The original service's asset writes, value submittals, asset bookmarks and the process type lookup (spec M6).
/// Asset_Create, Asset_CreateInstance, PickListValue_Create, MigrateAsset and the final save of PropertyValues_Save
/// answer logical failures as HandleLogicalException did; the rest fault.
/// </summary>
[Route(LegacyWire.BaseRoute)]
public sealed class AssetWriteWireController : LegacyWireControllerBase
{
    // ---- Value submittals and assets ----

    [HttpPost("PropertyValues_Save")]
    public async Task<IActionResult> PropertyValuesSave([FromServices] IAssetCommandHandler handler, [FromServices] LegacyErrorLog errors, CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken);
        if (error is not null)
            return error;
        SubmittalBody? body;
        try
        {
            body = AssetBodies.Submittal(root);
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        return await errors.HandleAsync(() => handler.SaveValuesAsync(body, cancellationToken), cancellationToken);
    }

    [HttpPost("Asset_Create")]
    public Task<IActionResult> AssetCreate([FromServices] IAssetCommandHandler handler, [FromServices] LegacyErrorLog errors, CancellationToken cancellationToken) =>
        CreationAsync(errors, body => handler.CreateAsync(body, cancellationToken), cancellationToken);

    [HttpPost("Asset_CreateInstance")]
    public Task<IActionResult> AssetCreateInstance([FromServices] IAssetCommandHandler handler, [FromServices] LegacyErrorLog errors, CancellationToken cancellationToken) =>
        CreationAsync(errors, body => handler.CreateInstanceAsync(body, cancellationToken), cancellationToken);

    private async Task<IActionResult> CreationAsync(LegacyErrorLog errors, Func<AssetCreationBody?, Task<Xdb.Core.Common.Result<bool>>> run,
        CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken);
        if (error is not null)
            return error;
        AssetCreationBody? body;
        try
        {
            body = AssetBodies.Creation(root);
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        return await errors.HandleAsync(() => run(body), cancellationToken);
    }

    [HttpGet("Asset_Clone")]
    public async Task<IActionResult> AssetClone([FromServices] IAssetCommandHandler handler, [FromQuery] Guid assetId, [FromQuery] Guid viewId, [FromQuery] Guid userId,
        [FromQuery] string? newAssetName, CancellationToken cancellationToken) =>
        (await handler.CloneAsync(assetId, viewId, userId, newAssetName, cancellationToken))
            .ToLegacyResult(asset => asset is null ? LegacyWire.NullTyped() : LegacyWire.Typed(asset));

    [HttpGet("Asset_Delete")]
    public async Task<IActionResult> AssetDelete([FromServices] IAssetCommandHandler handler, [FromQuery] Guid userId, [FromQuery] Guid assetId,
        CancellationToken cancellationToken) =>
        (await handler.DeleteAsync(userId, assetId, cancellationToken)).ToLegacyResult(ok => LegacyWire.Typed(ok));

    /// <summary>Wrapped body: userId, pickListValueId, pickListValue, displayValue, pickListId.</summary>
    [HttpPost("PickListValue_Create")]
    public async Task<IActionResult> PickListValueCreate([FromServices] IAssetCommandHandler handler, [FromServices] LegacyErrorLog errors,
        CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken, wrapped: true);
        if (error is not null)
            return error;
        if (WrappedRootFails(root))
            return LegacyWire.RequestError();
        Guid userId, valueId, pickListId;
        string? value, displayValue;
        try
        {
            userId = LegacyJsonBody.Member(root, "userId", LegacyJsonBody.Guid, Guid.Empty);
            valueId = LegacyJsonBody.Member(root, "pickListValueId", LegacyJsonBody.Guid, Guid.Empty);
            value = LegacyJsonBody.Member(root, "pickListValue", LegacyJsonBody.Text, null);
            displayValue = LegacyJsonBody.Member(root, "displayValue", LegacyJsonBody.Text, null);
            pickListId = LegacyJsonBody.Member(root, "pickListId", LegacyJsonBody.Guid, Guid.Empty);
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        return await errors.HandleAsync(() => handler.CreatePickListValueAsync(userId, valueId, value, displayValue, pickListId, cancellationToken), cancellationToken);
    }

    [HttpPost("MigrateAsset")]
    public async Task<IActionResult> MigrateAsset([FromServices] IAssetCommandHandler handler, [FromServices] LegacyErrorLog errors, CancellationToken cancellationToken)
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
        return await errors.HandleAsync(() => handler.MigrateAsync(key, target, cancellationToken), cancellationToken);
    }

    // ---- Process lookup ----

    /// <summary>
    /// Wrapped body {"propsAndValues": [{"Key":..,"Value":..}]}: only the values are used. An entry the dictionary
    /// cannot take resets the connection, as the original host did.
    /// </summary>
    [HttpPost("Process_GetAssetTypeIdByFilters")]
    public async Task<IActionResult> ProcessGetAssetTypeIdByFilters([FromServices] IAssetWriteProcedures writes, CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken, wrapped: true);
        if (error is not null)
            return error;
        if (WrappedRootFails(root))
            return LegacyWire.RequestError();
        List<string?>? values;
        try
        {
            values = AssetBodies.FilterValues(root);
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        catch (ConnectionResetException)
        {
            HttpContext.Abort();
            return new EmptyResult();
        }
        if (values is null)
            throw new NullReferenceException("propsAndValues is null.");
        return LegacyWire.Typed(await writes.GetAssetTypeIdByFiltersAsync(values, cancellationToken));
    }

    // ---- Asset bookmarks ----

    [HttpGet("Bookmarks_Get")]
    public async Task<IActionResult> BookmarksGet([FromServices] IBookmarkCommandHandler handler, [FromQuery] Guid userId, CancellationToken cancellationToken) =>
        (await handler.GetAsync(userId, cancellationToken)).ToLegacyResult(bookmarks => LegacyWire.Typed(bookmarks));

    [HttpGet("Bookmark_Add")]
    public async Task<IActionResult> BookmarkAdd([FromServices] IBookmarkCommandHandler handler, [FromQuery] Guid viewId, [FromQuery] Guid assetId,
        CancellationToken cancellationToken) =>
        (await handler.AddAsync(viewId, assetId, cancellationToken)).ToLegacyResult(ok => LegacyWire.Typed(ok));

    [HttpGet("Bookmark_Remove")]
    public async Task<IActionResult> BookmarkRemove([FromServices] IBookmarkCommandHandler handler, [FromQuery] Guid viewId, [FromQuery] Guid assetId,
        CancellationToken cancellationToken) =>
        (await handler.RemoveAsync(viewId, assetId, cancellationToken)).ToLegacyResult(ok => LegacyWire.Typed(ok));

    [HttpGet("Bookmark_ToggleHome")]
    public async Task<IActionResult> BookmarkToggleHome([FromServices] IBookmarkCommandHandler handler, [FromQuery] string? id, CancellationToken cancellationToken) =>
        (await handler.ToggleHomeAsync(id, cancellationToken)).ToLegacyResult(ok => LegacyWire.Typed(ok));

    [HttpGet("Bookmark_Delete")]
    public async Task<IActionResult> BookmarkDelete([FromServices] IBookmarkCommandHandler handler, [FromQuery] string? id, CancellationToken cancellationToken) =>
        (await handler.DeleteAsync(id, cancellationToken)).ToLegacyResult(ok => LegacyWire.Typed(ok));

    /// <summary>A wrapped body whose root is a literal null or not an object is the Request Error page (measured).</summary>
    private bool WrappedRootFails(JsonElement? root) =>
        root is null ? Request.ContentLength is > 0 : root.Value.ValueKind != JsonValueKind.Object;
}

/// <summary>The host reset the connection without a response (a wrapped dictionary member it could not read).</summary>
public sealed class ConnectionResetException() : Exception("The connection was reset.");

/// <summary>The asset write bodies as the original serializer bound them (spec M6 0.3).</summary>
public static class AssetBodies
{
    private static readonly string[] SubmittalMembers = ["UserId", "ViewId", "AssetId", "NewValues", "ValuesToDelete"];

    /// <summary>SubmittalClient: every member required; dictionaries as [{"Key","Value"}] (an object is empty, a repeated key fails).</summary>
    public static SubmittalBody? Submittal(JsonElement? root)
    {
        if (root is null)
            return null;
        var r = RoleBodies.Object(root.Value, SubmittalMembers);
        return new SubmittalBody(RoleBodies.Guid(r.GetProperty("UserId")), RoleBodies.Guid(r.GetProperty("ViewId")), RoleBodies.Guid(r.GetProperty("AssetId")),
            Dictionary(r.GetProperty("NewValues"), RoleBodies.Guid, LegacyJsonBody.Text),
            Dictionary(r.GetProperty("ValuesToDelete"), LegacyJsonBody.Int, e => e.ValueKind == JsonValueKind.Null ? throw new LegacyBodyException("Null Guid.") : RoleBodies.Guid(e)));
    }

    private static List<KeyValuePair<TKey, TValue>>? Dictionary<TKey, TValue>(JsonElement element, Func<JsonElement, TKey> key, Func<JsonElement, TValue> value)
        where TKey : notnull
    {
        if (element.ValueKind == JsonValueKind.Null)
            return null;
        if (element.ValueKind == JsonValueKind.Object)
            return [];
        if (element.ValueKind != JsonValueKind.Array)
            throw new LegacyBodyException("Not a dictionary.");
        var result = new List<KeyValuePair<TKey, TValue>>();
        var keys = new HashSet<TKey>();
        foreach (var entry in element.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object || !entry.TryGetProperty("Key", out var k) || !entry.TryGetProperty("Value", out var v))
                throw new LegacyBodyException("Missing a dictionary member.");
            var entryKey = key(k);
            if (!keys.Add(entryKey))
                throw new LegacyBodyException("An item with the same key has already been added.");
            result.Add(new KeyValuePair<TKey, TValue>(entryKey, value(v)));
        }
        return result;
    }

    /// <summary>AssetCreationClient: a plain class, so every member is optional and an empty array or object gives the defaults.</summary>
    public static AssetCreationBody? Creation(JsonElement? root)
    {
        if (root is null)
            return null;
        var r = root.Value;
        if (r.ValueKind == JsonValueKind.Array && r.GetArrayLength() == 0)
            return new AssetCreationBody(Guid.Empty, null, Guid.Empty, null, false, Guid.Empty, null);
        if (r.ValueKind != JsonValueKind.Object)
            throw new LegacyBodyException("Not an object.");
        return new AssetCreationBody(
            Get(r, "Id", RoleBodies.Guid, Guid.Empty), Get(r, "Name", LegacyJsonBody.Text, null), Get(r, "AssetTypeId", RoleBodies.Guid, Guid.Empty),
            Get(r, "InstanceOfId", RoleBodies.NullableGuid, null), Get(r, "IsInstance", RoleBodies.Bool, false), Get(r, "ViewId", RoleBodies.Guid, Guid.Empty),
            Get(r, "ParentId", RoleBodies.NullableGuid, null));

        static T Get<T>(JsonElement element, string name, Func<JsonElement, T> read, T fallback) =>
            element.TryGetProperty(name, out var member) ? read(member) : fallback;
    }

    /// <summary>The propsAndValues values in posted order; null when the member is absent or null.</summary>
    public static List<string?>? FilterValues(JsonElement? root)
    {
        if (root is not { } r || !r.TryGetProperty("propsAndValues", out var member) || member.ValueKind == JsonValueKind.Null)
            return null;
        if (member.ValueKind == JsonValueKind.Object)
            return [];
        if (member.ValueKind != JsonValueKind.Array)
            throw new ConnectionResetException();
        var values = new List<string?>();
        var keys = new HashSet<Guid>();
        foreach (var entry in member.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object || !entry.TryGetProperty("Key", out var k) || !entry.TryGetProperty("Value", out var v)
                || k.ValueKind != JsonValueKind.String || !Guid.TryParse(k.GetString(), out var key))
                throw new ConnectionResetException();
            if (!keys.Add(key))
                throw new LegacyBodyException("An item with the same key has already been added.");
            values.Add(LegacyJsonBody.Text(v));
        }
        return values;
    }
}
