using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Xdb.Api.Application.Common.Identity;
using Xdb.Api.Application.Common.Legacy;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Metadata.Legacy;
using Xdb.Api.Application.Metadata.Queries.Lookups;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Metadata.Commands;

/// <summary>The target-instance failure of a Migrate stub: the original reached another environment's database here.</summary>
public sealed class MigrationTargetUnavailableException() : InvalidOperationException("The target instance is not available.");

public interface IMetadataSaveCommandHandler
{
    /// <summary>AssetType_Save: a null body fails; the caller identity is read first and stamps relation and inherited rows.</summary>
    Task<Result<bool>> SaveAssetTypeAsync(AssetTypeContract? assetType, CancellationToken cancellationToken = default);
    Task<Result<bool>> SavePropertyAsync(PropertyContract? property, CancellationToken cancellationToken = default);
    Task<Result<bool>> SavePickListAsync(PickListContract? pickList, CancellationToken cancellationToken = default);
}

public sealed class MetadataSaveCommandHandler(AssetTypeSaver assetTypes, PropertySaver properties, PickListSaver pickLists, CurrentMember currentMember)
    : IMetadataSaveCommandHandler
{
    public async Task<Result<bool>> SaveAssetTypeAsync(AssetTypeContract? assetType, CancellationToken cancellationToken = default)
    {
        var userId = await currentMember.GetIdAsync(cancellationToken);
        return new Success<bool>(await assetTypes.SaveAsync(assetType ?? throw new NullReferenceException("assetType is null."), userId, cancellationToken));
    }

    public async Task<Result<bool>> SavePropertyAsync(PropertyContract? property, CancellationToken cancellationToken = default) =>
        new Success<bool>(await properties.SaveAsync(property ?? throw new NullReferenceException("property is null."), cancellationToken));

    public async Task<Result<bool>> SavePickListAsync(PickListContract? pickList, CancellationToken cancellationToken = default) =>
        new Success<bool>(await pickLists.SaveAsync(pickList ?? throw new NullReferenceException("pickList is null."), cancellationToken));
}

public interface IMetadataMigrateCommandHandler
{
    Task<Result<bool>> MigrateAssetTypeAsync(Guid assetTypeId, int target, CancellationToken cancellationToken = default);
    Task<Result<bool>> MigratePickListAsync(Guid pickListId, int target, CancellationToken cancellationToken = default);
    Task<Result<bool>> MigratePropertyAsync(Guid propertyId, int target, CancellationToken cancellationToken = default);
}

/// <summary>
/// The Migrate stubs (spec M6-M 5.1-5.3): to the configured own instance the whole original algorithm runs locally;
/// to any other target only the local steps before the first target query run, then the call faults without writing.
/// </summary>
public sealed class MetadataMigrateCommandHandler(
    MetadataLoader loader, IMetadataProcedures metadata, IMetadataWriteProcedures writes, AssetTypeSaver assetTypes, PropertySaver properties,
    PickListSaver pickLists, IOptions<LegacyEnvironmentOptions> environment, IDateTimeProvider clock) : IMetadataMigrateCommandHandler
{
    private bool Own(int target) => MigrateBody.IsOwnInstance(target, environment.Value.ApplicationInstance);

    private DateTime Now() => clock.UtcNow().ToLocalTime();

    public async Task<Result<bool>> MigrateAssetTypeAsync(Guid assetTypeId, int target, CancellationToken cancellationToken = default)
    {
        var assetType = await loader.AssetTypeAsync(assetTypeId, cancellationToken);
        if (assetType is null)
            return new Success<bool>(false);
        if (!Own(target))
        {
            // The first target query is the property check; only a type without properties validates locally first.
            if (assetType.Properties.Count == 0)
                await assetTypes.ValidateAsync(assetType, cancellationToken);
            throw new MigrationTargetUnavailableException();
        }

        var missing = new List<string>();
        foreach (var property in assetType.Properties)
            if (!await writes.IsLivePropertyAsync(property.Id, cancellationToken))
                missing.Add(string.IsNullOrEmpty(property.DisplayValue) ? property.Name : property.DisplayValue);
        ThrowMissing(missing, "property", "properties");

        foreach (var member in assetType.PropertyMembers)
        {
            member.IsDirty = true;
            member.IsNew = true;
        }
        assetType.IsDirty = true;
        assetType.IsNew = true;
        return new Success<bool>(await assetTypes.SaveAsync(assetType, SystemMemberIds.Admin, cancellationToken));
    }

    public async Task<Result<bool>> MigratePickListAsync(Guid pickListId, int target, CancellationToken cancellationToken = default)
    {
        var pickList = await loader.PickListAsync(pickListId, cancellationToken);
        if (pickList is null)
            return new Success<bool>(false);

        // IsDirty set while IsNew is still false stamps LastModified on the list and on every value.
        pickList.IsDirty = true;
        pickList.LastModified = Now();
        pickList.IsNew = true;
        foreach (var value in pickList.PickListValues)
        {
            value.IsDirty = true;
            value.LastModified = Now();
            value.IsNew = true;
        }

        if (!Own(target))
        {
            // PickListLayer(target).Save validates locally, then its first statement runs on the target.
            await pickLists.ValidateAsync(pickList, cancellationToken);
            throw new MigrationTargetUnavailableException();
        }
        return new Success<bool>(await pickLists.SaveAsync(pickList, cancellationToken));
    }

    public async Task<Result<bool>> MigratePropertyAsync(Guid propertyId, int target, CancellationToken cancellationToken = default)
    {
        var row = await metadata.GetPropertyAsync(propertyId, cancellationToken);
        if (row is null)
            return new Success<bool>(false);
        var property = MetadataLoader.Property(row);

        if (property.DataType == (int)LegacyDataType.PickList)
        {
            var pickListId = property.PickListId ?? throw new InvalidOperationException("Nullable object must have a value.");
            if (!Own(target))
                throw new MigrationTargetUnavailableException();
            if (!await writes.IsLivePickListAsync(pickListId, cancellationToken))
            {
                var pickList = await loader.PickListAsync(pickListId, cancellationToken) ?? throw new NullReferenceException("Pick list not found.");
                var name = string.IsNullOrEmpty(pickList.DisplayValue) ? pickList.Name : pickList.DisplayValue;
                throw new LegacyLogicalException($"The PickList in-use by this property ({name}) does not exist on the target instance.");
            }
        }

        property.IsDirty = true;
        property.LastModified = Now();
        property.IsNew = true;
        if (!Own(target))
        {
            await properties.ValidateAsync(property, cancellationToken);
            throw new MigrationTargetUnavailableException();
        }
        return new Success<bool>(await properties.SaveAsync(property, cancellationToken));
    }

    /// <summary>"Target instance is missing a property: x" or the numbered list, as the original built it.</summary>
    internal static void ThrowMissing(List<string> missing, string singular, string plural)
    {
        if (missing.Count == 1)
            throw new LegacyLogicalException($"Target instance is missing a {singular}: {missing[0]}");
        if (missing.Count <= 1)
            return;
        var message = new StringBuilder();
        message.Append($"Target instance is missing {missing.Count} {plural}:<br/>").Append("\r\n");
        for (var i = 0; i < missing.Count; i++)
            message.Append($"{i + 1}) {missing[i]}<br/>").Append("\r\n");
        throw new LegacyLogicalException(message.ToString());
    }
}
