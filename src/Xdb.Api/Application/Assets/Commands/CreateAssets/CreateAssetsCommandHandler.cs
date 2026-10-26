using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentValidation;
using FluentValidation.Results;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Xdb.Api.Application.Assets.Commands.CreateAsset;
using Xdb.Api.Application.Assets.Writes;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects.Commands.RegenerateAll;
using Xdb.Api.Application.GeneratedObjects.GenTables;
using Xdb.Api.Infrastructure.Data;
using Xdb.Api.Infrastructure.Data.Entities;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

using DataType = Xdb.Api.Application.Common.Metadata.DataType;

namespace Xdb.Api.Application.Assets.Commands.CreateAssets;

/// <param name="Created">The assets created, definitions first.</param>
/// <param name="Rejected">The assets not created, in command order, with why.</param>
/// <param name="Failures">Gen-table work that failed; the assets were still created.</param>
public sealed record CreateAssetsResult(IReadOnlyList<Guid> Created, IReadOnlyList<RejectedAsset> Rejected, IReadOnlyList<GenerationFailure> Failures);

/// <param name="Status">Conflict when the id is taken, Error when the write failed, else Validation.</param>
public sealed record RejectedAsset(Guid Id, ResultStatus Status, IReadOnlyList<ValidationFailure> Errors);

public interface ICreateAssetsCommandHandler
{
    /// <summary>Created, with what was and was not created; Validation when the command itself is bad (shape, actor, type).</summary>
    Task<Result<CreateAssetsResult>> ExecuteAsync(CreateAssetsCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Writes each asset as CreateAsset describes (the asset, one reviewed submittal, its name value and its property values,
/// approved by the actor), for a whole batch at once. The checks are set-based: links, pick list values, referenced assets
/// and members, taken ids and definitions are each read once per batch, not once per value. Rows go in with SqlBulkCopy,
/// <see cref="WriteChunk"/> assets per transaction. Gen tables: up to <see cref="PerAssetRefreshLimit"/> new assets are
/// merged one by one; more rebuild the type's tables, and its ancestors' (which hold their descendants' assets), once.
/// </summary>
public sealed class CreateAssetsCommandHandler(
    XdbDataContext db,
    IValidator<CreateAssetsCommand> validator,
    IValidator<CreateAssetsItem> itemValidator,
    GenTableRefresher genTables,
    IGenTableGenerator genTableGenerator,
    IMetadataProcedures metadata,
    IDateTimeProvider clock,
    ILogger<CreateAssetsCommandHandler> logger) : ICreateAssetsCommandHandler
{
    /// <summary>Up to this many new assets are merged into the gen tables one by one; more rebuild the tables once.</summary>
    public const int PerAssetRefreshLimit = 200;

    /// <summary>Assets written per transaction.</summary>
    private const int WriteChunk = 5_000;

    /// <summary>Ids per lookup query: SQL Server takes at most 2100 parameters.</summary>
    private const int QueryChunk = 2_000;

    /// <summary>The order of an error that is not about one value: before the value errors.</summary>
    private const int ItemError = -1;

    private sealed record LinkedProperty(Guid Id, string Name, DataType DataType, Guid? PickListId, Guid? AssetTypeId, bool? AssetTypeIsInstance,
        Guid? RoleId, bool AllowMultiValue);

    /// <param name="Index">The value's position in the item, for its error.</param>
    /// <param name="Reference">The id a PickList, Asset or User value names.</param>
    private sealed record Candidate(int Index, LinkedProperty Property, string Text, Guid Reference);

    private sealed class Pending(CreateAssetsItem item)
    {
        public CreateAssetsItem Item { get; } = item;
        public bool IsInstance => Item.InstanceOfId.HasValue;
        /// <summary>False when the item's shape is bad: it is rejected without the database checks.</summary>
        public bool IsWellFormed { get; set; } = true;
        public List<Candidate> Candidates { get; } = [];
        public List<(int Order, ValidationFailure Error)> Errors { get; } = [];
        public ResultStatus Status { get; set; } = ResultStatus.Validation;
        public List<(Guid PropertyId, string Text, int? Order)> Values { get; } = [];

        public void Reject(int order, string field, string message) => Errors.Add((order, new ValidationFailure(field, message)));
    }

    /// <summary>What the batch's references name, read once.</summary>
    private sealed class References
    {
        public Dictionary<Guid, HashSet<Guid>> PickListValues { get; } = [];
        public Dictionary<Guid, (Guid TypeId, bool IsInstance)> Assets { get; } = [];
        public Dictionary<Guid, HashSet<Guid>> TypeAndDescendants { get; } = [];
        public HashSet<Guid> Members { get; } = [];
        public HashSet<(Guid MemberId, Guid RoleId)> RoleMembers { get; } = [];
    }

    public async Task<Result<CreateAssetsResult>> ExecuteAsync(CreateAssetsCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Failed(validation.Errors);

        var (hasInstances, typeErrors) = await ValidateCommandAsync(command, cancellationToken);
        if (typeErrors.Count > 0)
            return Failed(typeErrors);

        var stopwatch = Stopwatch.StartNew();
        var linked = await LinkedPropertiesAsync(command.AssetTypeId, cancellationToken);
        var all = command.Assets.Select(a => new Pending(a)).ToList();
        foreach (var asset in all)
            CheckShapeAndValues(asset, hasInstances, linked[asset.IsInstance]);

        // Definitions first: an instance's definition may be in the batch. Items with bad values are still checked, so
        // every value's error is reported; they are not written.
        var pending = all.Where(p => p.IsWellFormed).OrderBy(p => p.IsInstance).ToList();
        var taken = await ExistingIdsAsync(pending.Select(p => p.Item.Id), cancellationToken);
        var definitions = await LiveDefinitionsAsync(command.AssetTypeId,
            pending.Where(p => p.IsInstance).Select(p => p.Item.InstanceOfId!.Value).Distinct().ToList(), cancellationToken);
        var references = await ReferencesAsync(pending.SelectMany(p => p.Candidates).ToList(), cancellationToken);

        var acceptedDefinitions = new HashSet<Guid>();
        foreach (var asset in pending)
        {
            CheckAgainstDatabase(asset, taken, definitions, acceptedDefinitions, references);
            if (asset.Errors.Count == 0 && !asset.IsInstance)
                acceptedDefinitions.Add(asset.Item.Id);
        }

        var accepted = pending.Where(p => p.Errors.Count == 0).ToList();
        var checkedIn = stopwatch.Elapsed;
        var created = await WriteAsync(command, accepted, cancellationToken);
        var writtenIn = stopwatch.Elapsed - checkedIn;
        var failures = await RefreshGenTablesAsync(command.AssetTypeId, accepted.Take(created.Count).ToList(), cancellationToken);
        logger.LogInformation("{Count} assets of type {AssetTypeId}: checked in {Checked:N1}s, written in {Written:N1}s, gen tables in {Gen:N1}s",
            created.Count, command.AssetTypeId, checkedIn.TotalSeconds, writtenIn.TotalSeconds, (stopwatch.Elapsed - checkedIn - writtenIn).TotalSeconds);

        var rejected = all.Where(p => p.Errors.Count > 0)
            .Select(p => new RejectedAsset(p.Item.Id, p.Status, p.Errors.OrderBy(e => e.Order).Select(e => e.Error).ToList()))
            .ToList();
        return new Success<CreateAssetsResult>(new CreateAssetsResult(created, rejected, failures), ResultStatus.Created);
    }

    /// <returns>Whether the type has instances, and the command's errors.</returns>
    private async Task<(bool HasInstances, List<ValidationFailure> Errors)> ValidateCommandAsync(CreateAssetsCommand command, CancellationToken cancellationToken)
    {
        var errors = new List<ValidationFailure>();

        if (!await db.Members.AsNoTracking().AnyAsync(m => m.Id == command.ActorId && m.Deleted == null, cancellationToken))
            errors.Add(new ValidationFailure(nameof(CreateAssetsCommand.ActorId), "The actor is not a live member."));

        var type = await db.AssetTypes.AsNoTracking()
            .Where(t => t.Id == command.AssetTypeId && t.Deleted == null)
            .Select(t => new { t.AllowAssets, t.InstanceLabel })
            .SingleOrDefaultAsync(cancellationToken);
        if (type is null)
            errors.Add(new ValidationFailure(nameof(CreateAssetsCommand.AssetTypeId), $"Asset type {command.AssetTypeId} does not exist."));
        else if (!type.AllowAssets)
            errors.Add(new ValidationFailure(nameof(CreateAssetsCommand.AssetTypeId), "The asset type holds no assets of its own."));

        return (!string.IsNullOrEmpty(type?.InstanceLabel), errors);
    }

    /// <summary>The properties linked to the type, by level (false: definitions, true: instances).</summary>
    private async Task<Dictionary<bool, Dictionary<Guid, LinkedProperty>>> LinkedPropertiesAsync(Guid assetTypeId, CancellationToken cancellationToken)
    {
        var links = await db.AssetTypesProperties.AsNoTracking()
            .Where(l => l.AssetTypeId == assetTypeId && l.Deleted == null && l.Property.Deleted == null)
            .Select(l => new
            {
                l.IsInstance, l.Property.Id, l.Property.Name, l.Property.DataTypeId, l.Property.PickListId, l.Property.AssetTypeId,
                l.Property.AssetTypeIsInstance, l.Property.RoleId, l.Property.AllowMultiValue
            })
            .ToListAsync(cancellationToken);

        var byLevel = new Dictionary<bool, Dictionary<Guid, LinkedProperty>> { [false] = [], [true] = [] };
        foreach (var link in links)
            byLevel[link.IsInstance].TryAdd(link.Id, new LinkedProperty(link.Id, link.Name, (DataType)link.DataTypeId, link.PickListId, link.AssetTypeId,
                link.AssetTypeIsInstance, link.RoleId, link.AllowMultiValue));
        return byLevel;
    }

    /// <summary>The item's shape, then each value's link and form; references are checked once the batch's are read.</summary>
    private void CheckShapeAndValues(Pending asset, bool hasInstances, IReadOnlyDictionary<Guid, LinkedProperty> linked)
    {
        var shape = itemValidator.Validate(asset.Item);
        if (!shape.IsValid)
        {
            asset.Errors.AddRange(shape.Errors.Select(e => (ItemError, e)));
            asset.IsWellFormed = false;
            return;
        }
        if (asset.IsInstance && !hasInstances)
            asset.Reject(ItemError, nameof(CreateAssetsItem.InstanceOfId), "The asset type has no instances.");

        for (var i = 0; i < asset.Item.Values.Count; i++)
        {
            var value = asset.Item.Values[i];
            var field = $"{nameof(CreateAssetsItem.Values)}[{i}]";
            if (!linked.TryGetValue(value.PropertyId, out var property))
            {
                asset.Reject(i, field, $"Property {value.PropertyId} is not linked to the asset type at this level.");
                continue;
            }

            var text = PropertyValueText.Normalize(property.DataType, value.Value, out var error);
            if (text is null)
            {
                asset.Reject(i, field, $"'{property.Name}': {error}");
                continue;
            }

            var reference = property.DataType is DataType.PickList or DataType.Asset or DataType.User && Guid.TryParse(text, out var id) ? id : Guid.Empty;
            asset.Candidates.Add(new Candidate(i, property, text, reference));
        }
    }

    private void CheckAgainstDatabase(Pending asset, HashSet<Guid> taken, HashSet<Guid> definitions, HashSet<Guid> acceptedDefinitions, References references)
    {
        if (taken.Contains(asset.Item.Id))
        {
            asset.Status = ResultStatus.Conflict;
            asset.Reject(ItemError, nameof(CreateAssetsItem.Id), $"Asset {asset.Item.Id} already exists.");
            return;
        }

        if (asset.Item.InstanceOfId is { } definitionId && !definitions.Contains(definitionId) && !acceptedDefinitions.Contains(definitionId))
            asset.Reject(ItemError, nameof(CreateAssetsItem.InstanceOfId), $"{definitionId} is not a live definition of the asset type.");

        var counts = new Dictionary<Guid, int>();
        foreach (var candidate in asset.Candidates)
        {
            var property = candidate.Property;
            var field = $"{nameof(CreateAssetsItem.Values)}[{candidate.Index}]";
            if (ReferenceError(candidate, references) is { } referenceError)
            {
                asset.Reject(candidate.Index, field, $"'{property.Name}': {referenceError}");
                continue;
            }

            counts.TryGetValue(property.Id, out var seen);
            if (seen > 0 && !property.AllowMultiValue)
            {
                asset.Reject(candidate.Index, field, $"'{property.Name}' takes a single value.");
                continue;
            }
            if (asset.Values.Any(v => v.PropertyId == property.Id && string.Equals(v.Text, candidate.Text, StringComparison.OrdinalIgnoreCase)))
            {
                asset.Reject(candidate.Index, field, $"'{property.Name}' has the same value twice.");
                continue;
            }

            counts[property.Id] = seen + 1;
            asset.Values.Add((property.Id, candidate.Text, property.AllowMultiValue ? seen : null));
        }
    }

    /// <summary>The id a reference value names must exist and fit the property; scalar types have nothing to check.</summary>
    private static string? ReferenceError(Candidate candidate, References references)
    {
        var property = candidate.Property;
        var id = candidate.Reference;
        switch (property.DataType)
        {
            case DataType.PickList:
                return property.PickListId is { } listId && references.PickListValues.TryGetValue(listId, out var values) && values.Contains(id)
                    ? null
                    : $"{id} is not a value of the property's pick list.";
            case DataType.Asset:
                // A grouping type holds no assets itself; its descendants' assets are its selectable values.
                return property.AssetTypeId is { } targetId
                    && references.Assets.TryGetValue(id, out var asset)
                    && references.TypeAndDescendants[targetId].Contains(asset.TypeId)
                    && (property.AssetTypeIsInstance == null || asset.IsInstance == property.AssetTypeIsInstance)
                    ? null
                    : $"{id} is not a live asset of the type the property refers to.";
            case DataType.User:
                if (!references.Members.Contains(id))
                    return $"{id} is not a live member.";
                return property.RoleId is not { } roleId || references.RoleMembers.Contains((id, roleId))
                    ? null
                    : $"{id} is not a member of the role the property is limited to.";
            default:
                return null;
        }
    }

    private async Task<HashSet<Guid>> ExistingIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var existing = new HashSet<Guid>();
        foreach (var chunk in ids.Chunk(QueryChunk))
            existing.UnionWith(await db.Assets.AsNoTracking().Where(a => chunk.Contains(a.Id)).Select(a => a.Id).ToListAsync(cancellationToken));
        return existing;
    }

    private async Task<HashSet<Guid>> LiveDefinitionsAsync(Guid assetTypeId, IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
    {
        var live = new HashSet<Guid>();
        foreach (var chunk in ids.Chunk(QueryChunk))
            live.UnionWith(await db.Assets.AsNoTracking()
                .Where(a => chunk.Contains(a.Id) && a.Deleted == null && !a.IsInstance && a.AssetTypeId == assetTypeId)
                .Select(a => a.Id)
                .ToListAsync(cancellationToken));
        return live;
    }

    private async Task<References> ReferencesAsync(IReadOnlyList<Candidate> candidates, CancellationToken cancellationToken)
    {
        var references = new References();
        List<Candidate> Of(DataType type) => candidates.Where(c => c.Property.DataType == type).ToList();

        var listIds = Of(DataType.PickList).Select(c => c.Property.PickListId).OfType<Guid>().Distinct().ToList();
        foreach (var chunk in listIds.Chunk(QueryChunk))
            foreach (var value in await db.PickListValues.AsNoTracking()
                         .Where(v => chunk.Contains(v.PickListId) && v.Deleted == null)
                         .Select(v => new { v.PickListId, v.Id })
                         .ToListAsync(cancellationToken))
            {
                if (!references.PickListValues.TryGetValue(value.PickListId, out var values))
                    references.PickListValues[value.PickListId] = values = [];
                values.Add(value.Id);
            }

        var assetValues = Of(DataType.Asset);
        foreach (var targetId in assetValues.Select(c => c.Property.AssetTypeId).OfType<Guid>().Distinct())
            references.TypeAndDescendants[targetId] = await SelfAndDescendantsAsync(targetId, cancellationToken);
        foreach (var chunk in assetValues.Select(c => c.Reference).Distinct().Chunk(QueryChunk))
            foreach (var asset in await db.Assets.AsNoTracking()
                         .Where(a => chunk.Contains(a.Id) && a.Deleted == null)
                         .Select(a => new { a.Id, a.AssetTypeId, a.IsInstance })
                         .ToListAsync(cancellationToken))
                references.Assets[asset.Id] = (asset.AssetTypeId, asset.IsInstance);

        var userValues = Of(DataType.User);
        var roleIds = userValues.Select(c => c.Property.RoleId).OfType<Guid>().Distinct().ToList();
        foreach (var chunk in userValues.Select(c => c.Reference).Distinct().Chunk(QueryChunk))
        {
            references.Members.UnionWith(await db.Members.AsNoTracking()
                .Where(m => chunk.Contains(m.Id) && m.Deleted == null)
                .Select(m => m.Id)
                .ToListAsync(cancellationToken));
            if (roleIds.Count > 0)
                foreach (var member in await db.RoleMembers.AsNoTracking()
                             .Where(m => chunk.Contains(m.MemberId) && roleIds.Contains(m.RoleId) && m.Deleted == null)
                             .Select(m => new { m.MemberId, m.RoleId })
                             .ToListAsync(cancellationToken))
                    references.RoleMembers.Add((member.MemberId, member.RoleId));
        }

        return references;
    }

    /// <summary>The type and its live descendants, through AssetTypeRelations.</summary>
    private async Task<HashSet<Guid>> SelfAndDescendantsAsync(Guid assetTypeId, CancellationToken cancellationToken)
    {
        var all = new HashSet<Guid> { assetTypeId };
        var level = all.ToList();
        while (level.Count > 0)
        {
            level = (await db.AssetTypeRelations.AsNoTracking()
                    .Where(r => level.Contains(r.ParentAssetTypeId) && r.Deleted == null)
                    .Select(r => r.ChildAssetTypeId)
                    .ToListAsync(cancellationToken))
                .Where(all.Add)
                .ToList();
        }
        return all;
    }

    /// <summary>
    /// Writes the accepted assets, definitions first, <see cref="WriteChunk"/> per transaction. A chunk that fails is
    /// rolled back and it and the rest are rejected, since later chunks may hold its definitions' instances.
    /// </summary>
    /// <returns>The ids written, in order.</returns>
    private async Task<List<Guid>> WriteAsync(CreateAssetsCommand command, IReadOnlyList<Pending> accepted, CancellationToken cancellationToken)
    {
        var created = new List<Guid>(accepted.Count);
        var now = clock.UtcNow();
        foreach (var chunk in accepted.Chunk(WriteChunk))
        {
            try
            {
                await WriteChunkAsync(command, chunk, now, cancellationToken);
                created.AddRange(chunk.Select(p => p.Item.Id));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Writing assets of type {AssetTypeId} failed; {Count} not written", command.AssetTypeId, accepted.Count - created.Count);
                foreach (var asset in accepted.Skip(created.Count))
                {
                    asset.Status = ResultStatus.Error;
                    asset.Reject(ItemError, nameof(CreateAssetsItem.Id), $"Not written: {ex.Message}");
                }
                break;
            }
        }
        return created;
    }

    private async Task WriteChunkAsync(CreateAssetsCommand command, IReadOnlyList<Pending> chunk, DateTime now, CancellationToken cancellationToken)
    {
        var assets = new List<Asset>(chunk.Count);
        var submittals = new List<PropertyValueSubmittal>(chunk.Count);
        var values = new List<PropertyValue>();
        foreach (var pending in chunk)
        {
            var item = pending.Item;
            var submittalId = Guid.NewGuid();
            assets.Add(new Asset
            {
                Id = item.Id,
                AssetTypeId = command.AssetTypeId,
                Name = item.Name,
                // NULL, never "": readers fall back with ISNULL(DisplayValue, Name).
                DisplayValue = null,
                Description = item.Description,
                IsInstance = pending.IsInstance,
                InstanceOfId = item.InstanceOfId,
                Created = now,
                CreatedBy = command.ActorId,
                Approved = now,
                ApprovedBy = command.ActorId
            });
            submittals.Add(new PropertyValueSubmittal
            {
                Id = submittalId, AssetId = item.Id, Created = now, CreatedBy = command.ActorId, Reviewed = now, ReviewedBy = command.ActorId
            });
            // The name is also a value, so the asset's history starts with it, as the original's creations did.
            values.Add(Value(SystemPropertyIds.AssetName, item.Id, submittalId, item.Name, order: null, now, command.ActorId));
            foreach (var (propertyId, text, order) in pending.Values)
                values.Add(Value(propertyId, item.Id, submittalId, text, order, now, command.ActorId));
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await EntityBulkCopy.WriteAsync(db, assets, cancellationToken);
        await EntityBulkCopy.WriteAsync(db, submittals, cancellationToken);
        await EntityBulkCopy.WriteAsync(db, values, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<List<GenerationFailure>> RefreshGenTablesAsync(Guid assetTypeId, IReadOnlyList<Pending> created, CancellationToken cancellationToken)
    {
        var failures = new List<GenerationFailure>();
        if (created.Count == 0)
            return failures;

        if (created.Count <= PerAssetRefreshLimit)
        {
            foreach (var asset in created)
            {
                try
                {
                    await genTables.InsertAsync(asset.Item.Id, asset.IsInstance, assetTypeId, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Asset {Id} was created but its gen-table rows failed", asset.Item.Id);
                    failures.Add(new GenerationFailure("Asset", asset.Item.Id, ex.Message));
                }
            }
            return failures;
        }

        for (Guid? typeId = assetTypeId; typeId is { } id; typeId = await metadata.GetParentAssetTypeIdAsync(id, cancellationToken))
        {
            try
            {
                await genTableGenerator.GenerateAsync(id, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "{Count} assets were created but the gen tables of type {AssetTypeId} failed to rebuild", created.Count, id);
                failures.Add(new GenerationFailure("AssetType", id, ex.Message));
            }
        }
        return failures;
    }

    private static PropertyValue Value(Guid propertyId, Guid assetId, Guid submittalId, string text, int? order, DateTime now, Guid actorId) => new()
    {
        Id = Guid.NewGuid(),
        PropertyId = propertyId,
        AssetId = assetId,
        SubmittalGroupId = submittalId,
        Value = text,
        Order = order,
        Created = now,
        CreatedBy = actorId,
        // The gen-table decoders read approved values only.
        Approved = now,
        ApprovedBy = actorId
    };

    private static Failure<CreateAssetsResult> Failed(List<ValidationFailure> errors) => new(default!, ResultStatus.Validation, errors);
}
