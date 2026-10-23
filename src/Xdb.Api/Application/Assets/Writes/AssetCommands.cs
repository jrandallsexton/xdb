using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Common.Identity;
using Xdb.Api.Application.Common.Legacy;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects;
using Xdb.Api.Application.Metadata;
using Xdb.Api.Application.Metadata.Commands;
using Xdb.Api.Application.Metadata.Legacy;
using Xdb.Api.Application.Metadata.Queries.Lookups;
using Xdb.Api.Infrastructure.Background;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Assets.Writes;

/// <summary>The posted SubmittalClient: the values to save by property (JSON order kept) and the values to delete.</summary>
public sealed record SubmittalBody(Guid UserId, Guid ViewId, Guid AssetId, List<KeyValuePair<Guid, string?>>? NewValues, List<KeyValuePair<int, Guid>>? ValuesToDelete);

/// <summary>The posted AssetCreationClient (a plain class: every member optional).</summary>
public sealed record AssetCreationBody(Guid Id, string? Name, Guid AssetTypeId, Guid? InstanceOfId, bool IsInstance, Guid ViewId, Guid? ParentId);

/// <summary>A failure the operation did not catch: the original's LogicalException raised outside its try (a plain fault).</summary>
public sealed class UncaughtLogicalException(string message) : InvalidOperationException(message);

public interface IAssetCommandHandler
{
    Task<Result<bool>> SaveValuesAsync(SubmittalBody? submittal, CancellationToken cancellationToken = default);
    Task<Result<bool>> CreateAsync(AssetCreationBody? asset, CancellationToken cancellationToken = default);
    Task<Result<bool>> CreateInstanceAsync(AssetCreationBody? asset, CancellationToken cancellationToken = default);
    Task<Result<AssetContract?>> CloneAsync(Guid assetId, Guid viewId, Guid userId, string? newAssetName, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(Guid userId, Guid assetId, CancellationToken cancellationToken = default);
    Task<Result<bool>> CreatePickListValueAsync(Guid userId, Guid pickListValueId, string? pickListValue, string? displayValue, Guid pickListId,
        CancellationToken cancellationToken = default);
    Task<Result<bool>> MigrateAsync(Guid assetId, int target, CancellationToken cancellationToken = default);
}

public sealed partial class AssetCommandHandler(
    SubmittalEngine engine, AssetWriter assets, IAssetWriteProcedures writes, IAssetProcedures reads, IMetadataProcedures metadata, PropertyValueLoader valueLoader,
    MetadataLoader metadataLoader, PickListSaver pickLists, CurrentMember currentMember, IBackgroundWorkQueue background, IOptions<LegacyEnvironmentOptions> environment,
    IDateTimeProvider clock) : IAssetCommandHandler
{
    private const char MultiValueDelimiter = '^';
    private const int PickListTextLimit = 255;
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    private DateTime Now() => clock.UtcNow().ToLocalTime();

    /// <summary>
    /// PropertyValues_Save (spec M6 3.1): values are built (URLs and currency amounts stored on the way) and deletions
    /// applied before the try; only the submittal save is inside it, and the asset's editor cache is dropped in its
    /// finally. The posted UserId is replaced by the caller identity.
    /// </summary>
    public async Task<Result<bool>> SaveValuesAsync(SubmittalBody? body, CancellationToken cancellationToken = default)
    {
        var submittal = body ?? throw new NullReferenceException("submittal is null.");
        if ((submittal.NewValues is null || submittal.NewValues.Count == 0) && (submittal.ValuesToDelete is null || submittal.ValuesToDelete.Count == 0))
            return new Success<bool>(false);

        var userId = await currentMember.GetIdAsync(cancellationToken);
        var pending = new Submittal(Guid.NewGuid(), submittal.AssetId, Now(), userId) { Reviewed = Now(), ReviewedBy = userId };
        var newValues = submittal.NewValues ?? throw new NullReferenceException("NewValues is null.");
        var properties = await engine.PropertiesAsync(newValues.Select(v => v.Key), cancellationToken);

        foreach (var (propertyId, text) in newValues)
        {
            var property = properties[propertyId];
            if (property.AllowMultiValue)
            {
                foreach (var part in text!.Split(MultiValueDelimiter))
                {
                    var value = part;
                    if (property.DataType == (int)LegacyDataType.URL)
                    {
                        var bits = part.Split('|');
                        var urlId = Guid.NewGuid();
                        string? name = bits.Length > 1 && bits[0].Length > 0 ? bits[0] : null;
                        await SaveUrlAsync(urlId, name, bits.Length > 1 ? bits[1] : bits[0], userId, cancellationToken);
                        value = urlId.ToString();
                    }
                    pending.Values.Add(NewValue(pending, propertyId, value, userId));
                }
                continue;
            }

            var single = NewValue(pending, propertyId, string.Empty, userId);
            if (property.DataType == (int)LegacyDataType.URL)
            {
                var bits = text!.Split('|');
                var name = bits[0];
                var url = bits[1];
                var urlId = Guid.NewGuid();
                await SaveUrlAsync(urlId, name, url, await currentMember.GetIdAsync(cancellationToken), cancellationToken);
                single.Value = urlId.ToString();
            }
            else if (property.DataType == (int)LegacyDataType.Currency)
            {
                var bits = text!.Split(MultiValueDelimiter);
                if (Guid.TryParse(bits[0], out var symbolId) && decimal.TryParse(bits[1], NumberStyles.Number, EnUs, out var amount))
                {
                    if (symbolId == Guid.Empty)
                        throw new UncaughtLogicalException("Currency symbol not specified.");
                    var currencyId = Guid.NewGuid();
                    await writes.SaveCurrencyValueAsync(currencyId, symbolId, amount, cancellationToken);
                    single.Value = currencyId.ToString();
                }
            }
            else
            {
                single.Value = text!.Replace("&#92;", "\\");
            }
            pending.Values.Add(single);
        }

        foreach (var (_, valueId) in submittal.ValuesToDelete ?? [])
        {
            if (await LoadValueAsync(valueId, cancellationToken))
                await writes.DeleteValueAsync(valueId, userId, cancellationToken);
        }

        try
        {
            return new Success<bool>(await engine.SaveAsync(pending, isAutoApprove: true, userId, cancellationToken));
        }
        finally
        {
            var assetId = submittal.AssetId;
            background.Enqueue($"asset clients delete {assetId}", (services, ct) => services.GetRequiredService<IAssetWriteProcedures>().DeleteAssetClientsAsync(assetId, ct));
        }
    }

    private SubmittalValue NewValue(Submittal submittal, Guid propertyId, string? value, Guid userId) => new()
    {
        Id = Guid.NewGuid(), CreatedBy = userId, Created = Now(), AssetId = submittal.AssetId, PropertyId = propertyId, Value = value, Approved = submittal.Reviewed,
        ApprovedBy = userId, SubmittalGroupId = submittal.Id
    };

    /// <summary>PropertyValueLayer.Get: the row and its display formatting (whose failures fault); false when absent.</summary>
    private async Task<bool> LoadValueAsync(Guid valueId, CancellationToken cancellationToken)
    {
        var row = await reads.GetPropertyValueAsync(valueId, cancellationToken);
        if (row is null)
            return false;
        var propertyRow = await metadata.GetPropertyAsync(row.PropertyId, cancellationToken);
        await valueLoader.DisplayValueAsync(row.Value ?? string.Empty, propertyRow is null ? null : MetadataLoader.Property(propertyRow), cancellationToken);
        return true;
    }

    /// <summary>UrlLayer.Save: validated before spr_URL_Save; a failure is outside the operation's try.</summary>
    private async Task SaveUrlAsync(Guid id, string? name, string? url, Guid createdBy, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(url))
            throw new UncaughtLogicalException("URL must have a URI for the URL property");
        if (!WebUrl().IsMatch(url) && !WwwUrl().IsMatch(url))
            throw new UncaughtLogicalException("URL must have a valid URI for the URL property");
        await writes.SaveUrlAsync(id, name, url, Now(), createdBy, cancellationToken);
    }

    [GeneratedRegex(@"^(ht|f|sf)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&amp;%\$#_]*)?$")]
    private static partial Regex WebUrl();

    [GeneratedRegex(@"^(www.)[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&amp;%\$#_]*)?$")]
    private static partial Regex WwwUrl();

    /// <summary>Asset_Create (spec M6 3.2): the trimmed name; a parent adds the parent relation and an AssetParent value.</summary>
    public async Task<Result<bool>> CreateAsync(AssetCreationBody? body, CancellationToken cancellationToken = default)
    {
        var userId = await currentMember.GetIdAsync(cancellationToken);
        var request = body ?? throw new NullReferenceException("asset is null.");
        var parentId = request.ParentId is { } parent && parent != Guid.Empty ? parent : (Guid?)null;
        var name = (request.Name ?? throw new NullReferenceException("Name is null.")).Trim();

        var created = Now();
        var asset = new AssetContract
        {
            Id = request.Id, CreatedBy = userId, Created = created, Approved = Now(), ApprovedBy = userId, Name = AssetWriter.Name(name), DisplayValue = string.Empty,
            Description = string.Empty, AssetTypeId = request.AssetTypeId, IsNew = true, IsDirty = true
        };
        var relations = parentId is { } p
            ? new List<RelationToSave> { new(Guid.NewGuid(), p, asset.Id, RelationTypes.ParentChild, Now(), Now(), userId, userId) }
            : [];
        if (!await assets.SaveAsync(asset, relations, cancellationToken))
            return new Success<bool>(false);

        var submittal = new Submittal(Guid.NewGuid(), asset.Id, Now(), userId);
        if (parentId is { } parentAsset)
            submittal.Values.Add(CreationValue(NarrativeIds.AssetParentProperty, asset.Id, parentAsset.ToString(), userId));
        submittal.Values.Add(CreationValue(SystemPropertyIds.AssetName, asset.Id, asset.Name, userId));
        if (!await engine.SaveAsync(submittal, isAutoApprove: true, userId, cancellationToken))
            return new Success<bool>(false);

        QueueGenTableInsert(asset.Id, isInstance: false, asset.AssetTypeId);
        return new Success<bool>(true);
    }

    /// <summary>The creation values: approved at their own creation time, no submittal group (so no narrative).</summary>
    private SubmittalValue CreationValue(Guid propertyId, Guid assetId, string? value, Guid userId)
    {
        var created = Now();
        return new SubmittalValue { Id = Guid.NewGuid(), PropertyId = propertyId, AssetId = assetId, Value = value, CreatedBy = userId, Created = created, Approved = created, ApprovedBy = userId };
    }

    private void QueueGenTableInsert(Guid assetId, bool isInstance, Guid assetTypeId) =>
        background.Enqueue($"gen tables insert {assetId}", (services, ct) => services.GetRequiredService<GenTableRefresher>().InsertAsync(assetId, isInstance, assetTypeId, ct));

    /// <summary>
    /// Asset_CreateInstance (spec M6 3.3): the untrimmed name; then the view's default values as a second submittal.
    /// If that submittal is refused the new instance is killed and the answer is false.
    /// </summary>
    public async Task<Result<bool>> CreateInstanceAsync(AssetCreationBody? body, CancellationToken cancellationToken = default)
    {
        var userId = await currentMember.GetIdAsync(cancellationToken);
        var request = body ?? throw new NullReferenceException("asset is null.");
        var instanceOfId = request.InstanceOfId ?? throw new InvalidOperationException("Nullable object must have a value.");

        var asset = new AssetContract
        {
            Id = request.Id, CreatedBy = userId, Created = Now(), Approved = Now(), ApprovedBy = userId, Name = AssetWriter.Name(request.Name), DisplayValue = string.Empty,
            AssetTypeId = request.AssetTypeId, IsInstance = true, InstanceOfId = instanceOfId, IsNew = true, IsDirty = true
        };
        if (!await assets.SaveAsync(asset, [], cancellationToken))
            return new Success<bool>(false);
        var nameSubmittal = new Submittal(Guid.NewGuid(), request.Id, Now(), userId);
        nameSubmittal.Values.Add(CreationValue(SystemPropertyIds.AssetName, request.Id, request.Name, userId));
        if (!await engine.SaveAsync(nameSubmittal, isAutoApprove: true, userId, cancellationToken))
            return new Success<bool>(false);
        QueueGenTableInsert(asset.Id, isInstance: true, asset.AssetTypeId);

        var defaults = new Submittal(Guid.NewGuid(), request.Id, Now(), userId);
        foreach (var (propertyId, value) in await writes.GetViewDefaultsAsync(request.ViewId, cancellationToken))
            defaults.Values.Add(new SubmittalValue
            {
                Id = Guid.NewGuid(), PropertyId = propertyId, AssetId = request.Id, SubmittalGroupId = defaults.Id, Value = value, CreatedBy = userId, Created = Now()
            });
        if (defaults.Values.Count > 0 && !await engine.SaveAsync(defaults, isAutoApprove: true, userId, cancellationToken))
        {
            if (!await metadata.IsAdminAsync(SystemMemberIds.Admin, SystemRoleIds.Admin, cancellationToken))
                throw new LegacyLogicalException("Invalid permission to kill assets.");
            await writes.KillAssetAsync(request.Id, cancellationToken);
            return new Success<bool>(false);
        }
        return new Success<bool>(true);
    }

    /// <summary>
    /// Asset_Clone (spec M6 3.4): a new unapproved asset, then the source's view values copied as an approved
    /// submittal. Nothing is caught: every failure faults. The answer is the in-memory clone.
    /// </summary>
    public async Task<Result<AssetContract?>> CloneAsync(Guid assetId, Guid viewId, Guid userId, string? newAssetName, CancellationToken cancellationToken = default)
    {
        var propertyIds = (await metadata.GetViewPropertyIdsAsync(viewId, cancellationToken)).ToList();
        var source = await assets.GetAsync(assetId, userId, cancellationToken) ?? throw new UncaughtLogicalException("Invalid source asset id for cloning.");
        var clone = new AssetContract
        {
            Id = Guid.NewGuid(), Name = AssetWriter.Name(newAssetName), Description = string.Empty, Created = Now(), CreatedBy = userId, IsNew = true,
            IsInstance = source.IsInstance, InstanceOfId = source.InstanceOfId, AssetTypeId = source.AssetTypeId, IsDirty = true, AllowDelete = true, AllowEdit = true
        };
        try
        {
            await assets.SaveAsync(clone, [], cancellationToken);
            if (propertyIds.Count > 0)
            {
                var values = await valueLoader.ForAssetAsync(assetId, propertyIds, includeUnapproved: false, includeDeleted: false, cancellationToken);
                if (values is { Count: > 0 })
                {
                    var submittal = new Submittal(Guid.NewGuid(), clone.Id, Now(), userId);
                    foreach (var value in values)
                        submittal.Values.Add(new SubmittalValue
                        {
                            Id = Guid.NewGuid(), AssetId = clone.Id, Created = submittal.Created, CreatedBy = userId, Order = value.Order, PropertyId = value.PropertyId,
                            SubmittalGroupId = submittal.Id, Value = value.Value
                        });
                    await engine.SaveAsync(submittal, isAutoApprove: true, userId, cancellationToken);
                }
            }
        }
        catch (LegacyLogicalException e)
        {
            throw new UncaughtLogicalException(e.Message);
        }
        return new Success<AssetContract?>(clone);
    }

    /// <summary>Asset_Delete: spr_Asset_Delete for anyone, then the gen-table rows in the background.</summary>
    public async Task<Result<bool>> DeleteAsync(Guid userId, Guid assetId, CancellationToken cancellationToken = default)
    {
        await writes.DeleteAssetAsync(assetId, userId, cancellationToken);
        background.Enqueue($"gen tables delete {assetId}", (services, ct) => services.GetRequiredService<GenTableRefresher>().DeleteAsync(assetId, ct));
        return new Success<bool>(true);
    }

    /// <summary>
    /// PickListValue_Create (spec M6 3.6): the new value is added to the loaded list, every value is re-ordered by its
    /// text in en-US order and stamped, and the whole list is saved (PickList_Save's path).
    /// </summary>
    public async Task<Result<bool>> CreatePickListValueAsync(Guid userId, Guid pickListValueId, string? pickListValue, string? displayValue, Guid pickListId,
        CancellationToken cancellationToken = default)
    {
        var isAdmin = await metadata.IsAdminAsync(userId, SystemRoleIds.Admin, cancellationToken);
        var created = Now();
        var value = new PickListValueContract
        {
            Id = pickListValueId, PickListId = pickListId, Created = created, CreatedBy = userId, Value = Truncate(pickListValue)!, IsNew = true, IsDirty = true,
            DisplayValue = pickListValue != displayValue ? Truncate(displayValue)! : string.Empty
        };
        if (isAdmin)
        {
            value.Approved = Now();
            value.ApprovedBy = userId;
        }

        var pickList = await metadataLoader.PickListAsync(pickListId, cancellationToken) ?? throw new NullReferenceException("Pick list not found.");
        pickList.PickListValues.Add(value);
        pickList.IsDirty = true;
        pickList.LastModified = created;
        pickList.LastModifiedBy = userId;

        var texts = pickList.PickListValues.Select(v => string.IsNullOrEmpty(v.DisplayValue) ? v.Value : v.DisplayValue).ToList();
        var sorted = texts.ToList();
        sorted.Sort(LegacyCollation.EnUs);
        for (var i = 0; i < pickList.PickListValues.Count; i++)
        {
            var item = pickList.PickListValues[i];
            item.Order = sorted.IndexOf(texts[i]);
            item.LastModified = created;
            item.LastModifiedBy = userId;
            item.IsDirty = true;
        }
        return new Success<bool>(await pickLists.SaveAsync(pickList, cancellationToken));

        static string? Truncate(string? text) => text is { Length: > PickListTextLimit } ? text[..(PickListTextLimit - 1)] : text;
    }

    /// <summary>
    /// MigrateAsset stub (spec M6 3.11): an unknown asset answers false for every target. To the own instance a live
    /// asset already has every value, so nothing is written; a deleted one fails the asset validation.
    /// </summary>
    public async Task<Result<bool>> MigrateAsync(Guid assetId, int target, CancellationToken cancellationToken = default)
    {
        var row = await reads.GetAssetAsync(assetId, cancellationToken);
        if (row is null)
            return new Success<bool>(false);
        if (!MigrateBody.IsOwnInstance(target, environment.Value.ApplicationInstance))
            throw new MigrationTargetUnavailableException();
        if (!await writes.IsLiveAssetAsync(assetId, cancellationToken))
        {
            var asset = await assets.GetAsync(assetId, SystemMemberIds.Admin, cancellationToken);
            asset!.AllowDelete = false;
            asset.AllowEdit = false;
            asset.IsNew = true;
            asset.IsDirty = true;
            await assets.SaveAsync(asset, [], cancellationToken);
        }
        return new Success<bool>(true);
    }
}

public interface IBookmarkCommandHandler
{
    Task<Result<List<BookmarkContract>>> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> AddAsync(Guid viewId, Guid assetId, CancellationToken cancellationToken = default);
    Task<Result<bool>> RemoveAsync(Guid viewId, Guid assetId, CancellationToken cancellationToken = default);
    Task<Result<bool>> ToggleHomeAsync(string? id, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(string? id, CancellationToken cancellationToken = default);
}

/// <summary>Asset bookmarks (spec M6 1.1, 3.7-3.10); the caller identity stands in for an empty user id.</summary>
public sealed class BookmarkCommandHandler(IAssetWriteProcedures writes, CurrentMember currentMember) : IBookmarkCommandHandler
{
    /// <summary>A concrete list: the typed response serializer is created for the declared type.</summary>
    public async Task<Result<List<BookmarkContract>>> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            userId = await currentMember.GetIdAsync(cancellationToken);
        return new Success<List<BookmarkContract>>([.. await writes.GetBookmarksAsync(userId, cancellationToken)]);
    }

    public async Task<Result<bool>> AddAsync(Guid viewId, Guid assetId, CancellationToken cancellationToken = default)
    {
        var memberId = await currentMember.GetIdAsync(cancellationToken);
        if (await writes.CountBookmarksAsync(memberId, viewId, assetId, cancellationToken) == 0)
            await writes.AddBookmarkAsync(memberId, viewId, assetId, cancellationToken);
        return new Success<bool>(true);
    }

    public async Task<Result<bool>> RemoveAsync(Guid viewId, Guid assetId, CancellationToken cancellationToken = default)
    {
        await writes.RemoveBookmarkAsync(await currentMember.GetIdAsync(cancellationToken), assetId, viewId, cancellationToken);
        return new Success<bool>(true);
    }

    public async Task<Result<bool>> ToggleHomeAsync(string? id, CancellationToken cancellationToken = default)
    {
        await writes.ToggleBookmarkHomeAsync(Parse(id), cancellationToken);
        return new Success<bool>(true);
    }

    public async Task<Result<bool>> DeleteAsync(string? id, CancellationToken cancellationToken = default)
    {
        await writes.DeleteBookmarkAsync(Parse(id), cancellationToken);
        return new Success<bool>(true);
    }

    private static Guid Parse(string? id) =>
        Guid.TryParse(id, out var bookmarkId) ? bookmarkId : throw new UncaughtLogicalException("Bookmark Id was in an invalid format.");
}
