using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects;
using Xdb.Api.Application.Metadata;
using Xdb.Api.Infrastructure.Background;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;
using Xdb.Core.LegacyWire;

namespace Xdb.Api.Application.Assets.Queries;

public sealed record GetAssetClientQuery(Guid AssetId, Guid UserId, Guid ViewId);

public interface IGetAssetClientQueryHandler
{
    /// <returns>The response body (the typed serializer's bytes).</returns>
    Task<Result<byte[]>> ExecuteAsync(GetAssetClientQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// AssetClient_Get: logs a hit (always, first, in the background), then returns the cached client for
/// (asset, view, user) or builds it and caches it in the background. A hit is returned as stored, however stale.
/// </summary>
public sealed class GetAssetClientQueryHandler(
    IAssetProcedures assets,
    IMetadataProcedures metadata,
    IViewProcedures views,
    PropertyValueLoader values,
    IBackgroundWorkQueue background) : IGetAssetClientQueryHandler
{
    private const byte JsonMarker = (byte)'{';
    private const int EditPermission = 1;
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    public async Task<Result<byte[]>> ExecuteAsync(GetAssetClientQuery query, CancellationToken cancellationToken = default)
    {
        var (assetId, userId, viewId) = (query.AssetId, query.UserId, query.ViewId);
        background.Enqueue($"asset hit {assetId}", (services, ct) => services.GetRequiredService<IAssetProcedures>().InsertAssetHitAsync(assetId, userId, ct));

        if (await assets.GetAssetClientCacheAsync(assetId, viewId, userId, cancellationToken) is { Length: > 0 } cached)
            return new Success<byte[]>(cached[0] == JsonMarker ? cached : LegacyWire.TypedBytes(NrbfAssetClientReader.Read(cached)));

        var asset = await AssetWithPermissionsAsync(assetId, userId, viewId, cancellationToken);
        var propertyValues = await ValuesForViewAsync(values, metadata, assetId, viewId, includeUnapproved: false, cancellationToken);
        if (asset is null)
            throw new NullReferenceException("Asset not found.");

        var (row, allowEdit, allowDelete) = asset.Value;
        var displayValue = PropertyValueLoader.Truncate(row.DisplayValue ?? string.Empty, 100, 99);
        var client = new AssetClientContract
        {
            Id = assetId,
            Name = string.IsNullOrEmpty(displayValue) ? PropertyValueLoader.Truncate(row.Name ?? string.Empty, 255, 254) : displayValue,
            AssetTypeId = row.AssetTypeId,
            IsInstance = row.IsInstance ?? false,
            InstanceOfId = row.InstanceOfId,
            Description = row.Description ?? string.Empty,
            LastModified = (row.LastModified ?? row.Created).ToString("M/d/yyyy h:mm:ss tt", EnUs),
            AllowEdit = allowEdit,
            AllowDelete = allowDelete,
            PropertyValues = propertyValues
        };
        var body = LegacyWire.TypedBytes(client);
        background.Enqueue($"asset client cache {assetId}", (services, ct) =>
            services.GetRequiredService<IAssetProcedures>().InsertAssetClientCacheAsync(assetId, viewId, userId, body, ct));
        return new Success<byte[]>(body);
    }

    /// <summary>
    /// UILayer.Asset_Get: the asset (deleted ones included) with AllowEdit/AllowDelete: all for an administrator;
    /// otherwise when one of the member's editable roles on the view matches the asset in that role's Edit view for the
    /// view's asset type (Delete also uses the Edit view, as in the original).
    /// </summary>
    private async Task<(AssetRow Row, bool AllowEdit, bool AllowDelete)?> AssetWithPermissionsAsync(Guid assetId, Guid userId, Guid viewId,
        CancellationToken cancellationToken)
    {
        var row = await assets.GetAssetAsync(assetId, cancellationToken);
        if (row is null)
            return null;
        if (await metadata.IsAdminAsync(userId, SystemRoleIds.Admin, cancellationToken))
            return (row, true, true);

        bool allowEdit = false, allowDelete = false;
        var viewRoles = await assets.GetRolesForUserAndViewAsync(userId, viewId, cancellationToken);
        if (viewRoles.Count > 0)
        {
            var editable = await assets.GetEditableRoleIdsAsync(userId, cancellationToken);
            if (editable.Count > 0)
            {
                var roles = new List<Guid>();
                foreach (var role in editable)
                {
                    if (viewRoles.Contains(role) && !roles.Contains(role))
                        roles.Add(role);
                }
                var viewAssetTypeId = await views.GetViewAssetTypeIdAsync(viewId, cancellationToken);
                foreach (var role in roles)
                {
                    if (await MatchesAsync(role, viewAssetTypeId, assetId, cancellationToken))
                    {
                        allowEdit = true;
                        break;
                    }
                }
                foreach (var role in roles)
                {
                    if (await MatchesAsync(role, viewAssetTypeId, assetId, cancellationToken))
                    {
                        allowDelete = true;
                        break;
                    }
                }
            }
        }
        return (row, allowEdit, allowDelete);
    }

    private async Task<bool> MatchesAsync(Guid roleId, Guid assetTypeId, Guid assetId, CancellationToken cancellationToken) =>
        await assets.CountInRoleViewAsync(GeneratedObjectNames.RoleView(roleId, assetTypeId, EditPermission), assetId, cancellationToken) > 0;

    /// <summary>PropertyValues_GetForAnAssetByViewId: the asset's values for the view's properties ([] when the view has none).</summary>
    internal static async Task<List<PropertyValueClientContract>> ValuesForViewAsync(PropertyValueLoader values, IMetadataProcedures metadata, Guid assetId,
        Guid viewId, bool includeUnapproved, CancellationToken cancellationToken)
    {
        var result = new List<PropertyValueClientContract>();
        var propertyIds = await metadata.GetViewPropertyIdsAsync(viewId, cancellationToken);
        if (propertyIds.Count > 0)
        {
            var loaded = await values.ForAssetAsync(assetId, propertyIds, includeUnapproved, includeDeleted: false, cancellationToken)
                ?? throw new NullReferenceException("Asset or asset type not found.");
            result.AddRange(loaded.Select(PropertyValueClientContract.From));
        }
        return result;
    }
}

public sealed record GetViewValuesQuery(Guid AssetId, Guid ViewId, bool IncludeUnapproved);

public interface IGetViewValuesQueryHandler
{
    Task<Result<List<PropertyValueClientContract>>> ExecuteAsync(GetViewValuesQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetViewValuesQueryHandler(PropertyValueLoader values, IMetadataProcedures metadata) : IGetViewValuesQueryHandler
{
    public async Task<Result<List<PropertyValueClientContract>>> ExecuteAsync(GetViewValuesQuery query, CancellationToken cancellationToken = default) =>
        new Success<List<PropertyValueClientContract>>(
            await GetAssetClientQueryHandler.ValuesForViewAsync(values, metadata, query.AssetId, query.ViewId, query.IncludeUnapproved, cancellationToken));
}

public sealed record GetHistoryQuery(Guid AssetId, Guid ViewId, bool OrderChronologically);

public interface IGetHistoryQueryHandler
{
    Task<Result<List<HistoricalValue>>> ExecuteAsync(GetHistoryQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// HistoricalValues_Get: every approved value of the view's properties, with history, as (property, value, "date by
/// member"): newest first by Created (reversed, so ties also reverse), or grouped by property name under en-US.
/// </summary>
public sealed class GetHistoryQueryHandler(PropertyValueLoader values, IMetadataProcedures metadata, IAssetProcedures assets, IReportProcedures reports)
    : IGetHistoryQueryHandler
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    public async Task<Result<List<HistoricalValue>>> ExecuteAsync(GetHistoryQuery query, CancellationToken cancellationToken = default)
    {
        var result = new List<HistoricalValue>();
        var propertyIds = await metadata.GetViewPropertyIdsAsync(query.ViewId, cancellationToken);
        var loaded = await values.ForAssetAsync(query.AssetId, propertyIds, includeUnapproved: false, includeDeleted: true, cancellationToken);
        if (loaded is null || loaded.Count == 0)
            return new Success<List<HistoricalValue>>(result);

        var byName = new SortedDictionary<string, List<HistoricalValue>>(LegacyCollation.EnUs);
        var byDate = new SortedList<DateTime, List<HistoricalValue>>();
        foreach (var item in loaded)
        {
            var name = await metadata.GetPropertyDisplayValueAsync(item.PropertyId, cancellationToken);
            var text = string.IsNullOrEmpty(item.DisplayValue) ? item.Value : item.DisplayValue;
            if (await metadata.GetPropertyDataTypeIdAsync(item.PropertyId, cancellationToken) == (int)LegacyDataType.Currency && !string.IsNullOrEmpty(item.DisplayValueHtml))
                text = item.DisplayValueHtml.Replace("^", " ");
            if (await metadata.GetPropertySystemTypeIdAsync(item.PropertyId, cancellationToken) == (int)SystemType.InstanceOf)
            {
                var assetTypeId = await metadata.GetAssetTypeIdOfAssetAsync(query.AssetId, cancellationToken)
                    ?? throw new InvalidOperationException("Nullable object must have a value.");
                name = await metadata.GetReportingLabelAsync(assetTypeId, await assets.IsInstanceAsync(query.AssetId, cancellationToken), cancellationToken);
            }
            var creationInfo = $"{item.Created.ToString("dd-MMM-yyyy", EnUs)} by {await MemberDisplayValueAsync(item.CreatedBy, cancellationToken)}";
            var history = new HistoricalValue(name, text, creationInfo);
            if (query.OrderChronologically)
            {
                if (byDate.TryGetValue(item.Created, out var sameTime))
                    sameTime.Add(history);
                else
                    byDate.Add(item.Created, [history]);
            }
            else if (byName.TryGetValue(name, out var sameName))
            {
                sameName.Add(history);
            }
            else
            {
                byName.Add(name, [history]);
            }
        }

        if (query.OrderChronologically)
        {
            foreach (var group in byDate.Values)
                result.AddRange(group);
            result.Reverse();
        }
        else
        {
            foreach (var group in byName.Values)
                result.AddRange(group);
        }
        return new Success<List<HistoricalValue>>(result);
    }

    /// <summary>MemberLayer.DisplayValue: "" when unknown, the user id when both names are empty, else "Last, First [user]".</summary>
    private async Task<string> MemberDisplayValueAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var member = await reports.GetMemberAsync(memberId, cancellationToken);
        if (member is null)
            return string.Empty;
        if (string.IsNullOrEmpty(member.FName) && string.IsNullOrEmpty(member.LName))
            return member.UserId;
        return $"{member.LName}, {member.FName} [{member.UserId}]";
    }
}

public sealed record GetAssetValueTextQuery(Guid AssetId, Guid PropertyId);

public interface IGetAssetValueTextQueryHandler
{
    Task<Result<string?>> ExecuteAsync(GetAssetValueTextQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// PropertyValues_GetForAsset: the first value's HTML rendering, else its display text, else its value. An existing
/// asset without a value for the property fails (the original indexed an empty list).
/// </summary>
public sealed class GetAssetValueTextQueryHandler(PropertyValueLoader values, IMetadataProcedures metadata) : IGetAssetValueTextQueryHandler
{
    public async Task<Result<string?>> ExecuteAsync(GetAssetValueTextQuery query, CancellationToken cancellationToken = default)
    {
        // PropertyLayer.Get maps the row (and fails on a non-NULL Precision, as the original did).
        if (await metadata.GetPropertyAsync(query.PropertyId, cancellationToken) is not { } row)
            return new Success<string?>(string.Empty);
        _ = MetadataLoader.Property(row);
        var list = await values.ForAssetAsync(query.AssetId, [query.PropertyId], includeUnapproved: false, includeDeleted: false, cancellationToken);
        if (list is null)
            return new Success<string?>(string.Empty);
        var first = list.Count > 0 ? list[0] : throw new ArgumentOutOfRangeException(nameof(query), "Index was out of range.");
        if (string.IsNullOrEmpty(first.DisplayValueHtml))
            return new Success<string?>(string.IsNullOrEmpty(first.DisplayValue) ? first.Value : first.DisplayValue);
        return new Success<string?>(first.DisplayValueHtml);
    }
}

public sealed record GetAssetNameQuery(Guid AssetId);

public interface IGetAssetNameQueryHandler
{
    Task<Result<string>> ExecuteAsync(GetAssetNameQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Asset_GetName: the display value, else the name (untrimmed, deleted assets included); "" when absent.</summary>
public sealed class GetAssetNameQueryHandler(IAssetProcedures assets) : IGetAssetNameQueryHandler
{
    public async Task<Result<string>> ExecuteAsync(GetAssetNameQuery query, CancellationToken cancellationToken = default)
    {
        var row = await assets.GetAssetAsync(query.AssetId, cancellationToken);
        if (row is null)
            return new Success<string>(string.Empty);
        var display = PropertyValueLoader.Truncate(row.DisplayValue ?? string.Empty, 100, 99);
        return new Success<string>(string.IsNullOrEmpty(display) ? PropertyValueLoader.Truncate(row.Name ?? string.Empty, 255, 254) : display);
    }
}

public sealed record GetAssetDescriptionQuery(Guid AssetId);

public interface IGetAssetDescriptionQueryHandler
{
    Task<Result<string>> ExecuteAsync(GetAssetDescriptionQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetAssetDescriptionQueryHandler(IAssetProcedures assets) : IGetAssetDescriptionQueryHandler
{
    public async Task<Result<string>> ExecuteAsync(GetAssetDescriptionQuery query, CancellationToken cancellationToken = default) =>
        new Success<string>(await assets.GetAssetDescriptionAsync(query.AssetId, cancellationToken));
}

public sealed record GetAssetParentQuery(Guid AssetId);

public interface IGetAssetParentQueryHandler
{
    Task<Result<Guid?>> ExecuteAsync(GetAssetParentQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetAssetParentQueryHandler(IAssetProcedures assets) : IGetAssetParentQueryHandler
{
    public async Task<Result<Guid?>> ExecuteAsync(GetAssetParentQuery query, CancellationToken cancellationToken = default) =>
        new Success<Guid?>(await assets.GetParentAssetIdAsync(query.AssetId, cancellationToken));
}

public sealed record AssetModifiedQuery(Guid AssetId, string? CheckDateTime);

public interface IAssetModifiedQueryHandler
{
    Task<Result<bool>> ExecuteAsync(AssetModifiedQuery query, CancellationToken cancellationToken = default);
}

/// <summary>AssetClientModified: as ViewClientModified, against Assets.LastModified.</summary>
public sealed class AssetModifiedQueryHandler(IAssetProcedures assets) : IAssetModifiedQueryHandler
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    public async Task<Result<bool>> ExecuteAsync(AssetModifiedQuery query, CancellationToken cancellationToken = default)
    {
        var lastModified = await assets.GetAssetLastModifiedAsync(query.AssetId, cancellationToken);
        if (DateTime.TryParse(query.CheckDateTime, EnUs, DateTimeStyles.None, out var check))
            return new Success<bool>(lastModified.HasValue && lastModified.Value.CompareTo(check) != 0);
        return new Success<bool>(lastModified.HasValue);
    }
}

public sealed record AssetExistsQuery(string? AssetName, Guid AssetTypeId);

public interface IAssetExistsQueryHandler
{
    Task<Result<bool>> ExecuteAsync(AssetExistsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Asset_Exists: an asset named (Name or DisplayValue) so among the type's descendants and itself, deleted included.</summary>
public sealed class AssetExistsQueryHandler(IAssetProcedures assets, MetadataRules rules) : IAssetExistsQueryHandler
{
    public async Task<Result<bool>> ExecuteAsync(AssetExistsQuery query, CancellationToken cancellationToken = default)
    {
        foreach (var assetTypeId in await rules.ChildrenAsync(query.AssetTypeId, includeAllDescendants: true, cancellationToken))
        {
            if (await assets.FindAssetIdAsync(query.AssetName, assetTypeId, cancellationToken) is { } id && await assets.GetAssetAsync(id, cancellationToken) is not null)
                return new Success<bool>(true);
        }
        return new Success<bool>(false);
    }
}

public sealed record FindAssetIdQuery(string? AssetName, Guid AssetTypeId);

public interface IFindAssetIdQueryHandler
{
    Task<Result<Guid>> ExecuteAsync(FindAssetIdQuery query, CancellationToken cancellationToken = default);
}

/// <summary>AssetId_Get: the first live asset with that Name among the type's descendants and itself (any asset when no type).</summary>
public sealed class FindAssetIdQueryHandler(IAssetProcedures assets, MetadataRules rules) : IFindAssetIdQueryHandler
{
    public async Task<Result<Guid>> ExecuteAsync(FindAssetIdQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Guid>? types = query.AssetTypeId != Guid.Empty
            ? await rules.ChildrenAsync(query.AssetTypeId, includeAllDescendants: true, cancellationToken)
            : null;
        var ids = await assets.GetAssetIdsByNameAsync(query.AssetName, types, cancellationToken);
        return new Success<Guid>(ids.Count == 0 ? Guid.Empty : ids[0]);
    }
}

public sealed record GetValueDisplayQuery(Guid PropertyId, string? PropertyValue);

public interface IGetValueDisplayQueryHandler
{
    Task<Result<string?>> ExecuteAsync(GetValueDisplayQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// PropertyValue_GetDisplayValue: a raw value rendered by the property's data type (asset name, currency symbol and
/// amount, date part, document/image name, picklist text, URL name, member). Values that are not Guids where one is
/// required fail, as the original's new Guid(...) did.
/// </summary>
public sealed class GetValueDisplayQueryHandler(IMetadataProcedures metadata, IAssetProcedures assets, IReportProcedures reports) : IGetValueDisplayQueryHandler
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    public async Task<Result<string?>> ExecuteAsync(GetValueDisplayQuery query, CancellationToken cancellationToken = default)
    {
        if (await metadata.GetPropertyAsync(query.PropertyId, cancellationToken) is not { } row)
            return new Success<string?>(string.Empty);
        var property = MetadataLoader.Property(row);
        var value = query.PropertyValue;
        string? result;
        switch (property.DataType)
        {
            case (int)LegacyDataType.Asset:
            case (int)LegacyDataType.Relation_ParentChild:
            case (int)LegacyDataType.Relation_ChildParent:
            case (int)LegacyDataType.Dependency:
            case (int)LegacyDataType.System_AssetType:
                result = await assets.GetAssetNameTextAsync(new Guid(value!), cancellationToken);
                break;
            case (int)LegacyDataType.Currency:
                result = string.Empty;
                if (!string.IsNullOrEmpty(value))
                {
                    if (Guid.TryParse(value[..36], out var symbol))
                        result = await assets.GetCurrencySymbolTextAsync(symbol, cancellationToken) + " " + value.Remove(0, 36);
                }
                else
                {
                    result = value;
                }
                break;
            case (int)LegacyDataType.Date:
                result = Guid.TryParse(value, out var datePart)
                    ? await reports.GetDatePartDisplayValueAsync(datePart, cancellationToken) ?? throw new NullReferenceException("Date part not found.")
                    : value;
                break;
            case (int)LegacyDataType.Document:
                result = (await assets.GetDocumentAsync(new Guid(value!), cancellationToken) ?? throw new NullReferenceException("Document not found.")).Name;
                break;
            case (int)LegacyDataType.Image:
                result = (await metadata.GetImageAsync(new Guid(value!), cancellationToken) ?? throw new NullReferenceException("Image not found.")).Name;
                break;
            case (int)LegacyDataType.PickList:
                var pickListValue = await reports.GetPickListValueAsync(new Guid(value!), cancellationToken);
                result = pickListValue is null ? value
                    : string.IsNullOrEmpty(pickListValue.DisplayValue) ? PropertyValueLoader.Truncate(pickListValue.Value, 255, 254)
                    : PropertyValueLoader.Truncate(pickListValue.DisplayValue, 255, 254);
                break;
            case (int)LegacyDataType.URL:
                var url = await assets.GetUrlAsync(new Guid(value!), cancellationToken);
                result = url is null ? string.Empty : string.IsNullOrEmpty(url.Name) ? url.Url : url.Name;
                break;
            case (int)LegacyDataType.User:
                result = value!.ToLower(EnUs) == "current_user" ? value : await MemberDisplayValueAsync(new Guid(value), cancellationToken);
                break;
            case (int)LegacyDataType.Relation_Other:
                result = string.Empty;
                break;
            default:
                result = value;
                break;
        }
        return new Success<string?>(result);
    }

    private async Task<string> MemberDisplayValueAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var member = await reports.GetMemberAsync(memberId, cancellationToken);
        if (member is null)
            return string.Empty;
        if (string.IsNullOrEmpty(member.FName) && string.IsNullOrEmpty(member.LName))
            return member.UserId;
        return $"{member.LName}, {member.FName} [{member.UserId}]";
    }
}
