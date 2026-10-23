using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Common.Identity;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Metadata.Legacy;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Metadata.Commands;

/// <summary>
/// AssetTypeLayer.Save of the deployed release (spec M6-M 3.1): validate, upsert the row, reconcile the parent
/// relation, copy new members into every direct child (recursively, by re-saving the child), soft-delete deleted
/// members' inherited rows in every descendant, then upsert the posted members. No transaction and no generated
/// objects: a failure leaves the earlier statements committed, and gen tables are not touched.
/// </summary>
public sealed class AssetTypeSaver(IMetadataWriteProcedures writes, IMetadataProcedures metadata, MetadataLoader loader, IDateTimeProvider clock)
{
    private DateTime Now() => clock.UtcNow().ToLocalTime();

    public async Task<bool> SaveAsync(AssetTypeContract assetType, Guid userId, CancellationToken cancellationToken)
    {
        await ValidateAsync(assetType, cancellationToken);
        if (!assetType.IsDirty)
            return true;

        await writes.SaveAssetTypeAsync(assetType, cancellationToken);

        var parent = await metadata.GetParentAssetTypeIdAsync(assetType.Id, cancellationToken);
        if (parent.HasValue && assetType.ParentId.HasValue)
        {
            if (parent.Value != assetType.ParentId.Value)
            {
                await writes.DeleteAssetTypeRelationAsync(parent.Value, assetType.Id, Now(), userId, cancellationToken);
                await writes.InsertAssetTypeRelationAsync(Guid.NewGuid(), assetType.ParentId.Value, assetType.Id, Now(), userId, cancellationToken);
            }
        }
        else if (parent.HasValue)
        {
            // The original compared with ParentId.Value here, which throws when the parent is being removed (bug kept).
            throw new InvalidOperationException("Nullable object must have a value.");
        }
        else if (assetType.ParentId.HasValue)
        {
            await writes.InsertAssetTypeRelationAsync(Guid.NewGuid(), assetType.ParentId.Value, assetType.Id, Now(), userId, cancellationToken);
        }

        var anyNew = false;
        foreach (var member in assetType.PropertyMembers)
        {
            if (member.IsNew)
            {
                anyNew = true;
                break;
            }
        }
        if (anyNew)
        {
            var children = await metadata.GetDirectChildAssetTypeIdsAsync(assetType.Id, cancellationToken);
            foreach (var member in assetType.PropertyMembers)
            {
                if (!member.IsNew)
                    continue;
                foreach (var childId in children)
                {
                    var child = await loader.AssetTypeAsync(childId, cancellationToken);
                    if (child is null || child.Id != childId)
                        continue;
                    child.PropertyMembers.Add(new AssetTypePropertyRelationContract
                    {
                        Id = Guid.NewGuid(), AssetTypeId = child.Id, Created = Now(), CreatedBy = userId, IsDirty = true, IsInherited = true,
                        IsInheritedValue = false, IsInstance = member.IsInstance, IsNew = true, PropertyId = member.PropertyId
                    });
                    child.IsDirty = true;
                    await SaveAsync(child, userId, cancellationToken);
                }
            }
        }

        var affected = new List<Guid>(await metadata.GetDescendantAssetTypeIdsAsync(assetType.Id, cancellationToken)) { assetType.Id };
        foreach (var member in assetType.PropertyMembers)
        {
            if (!member.Deleted.HasValue)
                continue;
            foreach (var typeId in affected)
                await writes.DeleteInheritedMemberAsync(member.Deleted, member.DeletedBy, typeId, member.PropertyId, cancellationToken);
        }

        foreach (var member in assetType.PropertyMembers)
            if (member.IsDirty)
                await writes.SaveAssetTypePropertyRelationAsync(member, cancellationToken);
        return true;
    }

    public async Task ValidateAsync(AssetTypeContract assetType, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(assetType.Name))
            throw new LegacyLogicalException("Name must be defined");
        if (string.IsNullOrEmpty(assetType.Pluralization))
            throw new LegacyLogicalException("Pluralization must be defined.");
        if (assetType.CreatedBy == Guid.Empty)
            throw new LegacyLogicalException("CreatedBy must be defined");
        if (!await metadata.IsLiveMemberIdAsync(assetType.CreatedBy, cancellationToken))
            throw new LegacyLogicalException("Invalid user id");
        if (assetType.Id == SystemAssetTypeIds.User && (assetType.Deleted.HasValue || assetType.DeletedBy.HasValue))
            throw new LegacyLogicalException("Cannot delete AssetType=User");
    }
}

/// <summary>PropertyLayer.Save (spec M6-M 3.2): validate, then upsert when dirty.</summary>
public sealed class PropertySaver(IMetadataWriteProcedures writes, IMetadataProcedures metadata)
{
    private const int UndefinedDataType = -1;

    public async Task<bool> SaveAsync(PropertyContract property, CancellationToken cancellationToken)
    {
        await ValidateAsync(property, cancellationToken);
        if (property.IsDirty)
            await writes.SavePropertyAsync(property, cancellationToken);
        return true;
    }

    public async Task ValidateAsync(PropertyContract property, CancellationToken cancellationToken)
    {
        if (property.Id == Guid.Empty)
            throw new LegacyLogicalException("Id cannot be null");
        if (property.DataType == UndefinedDataType)
            throw new LegacyLogicalException("DataType must be defined");
        if (property.DataType == (int)LegacyDataType.Asset && !property.AssetTypeId.HasValue)
            throw new LegacyLogicalException("DataType is Asset, but the Asset Type was not specified");
        if (property.AssetTypeId.HasValue && !property.AssetTypeIsInstance.HasValue)
            throw new LegacyLogicalException(
                "Must know whether the property is dealing with Asset Instances or Asset Definitions.  AssetTypeIsInstance not specified.");
        if (string.IsNullOrEmpty(property.Name))
            throw new LegacyLogicalException("Property name must be defined");
        if (property.CreatedBy == Guid.Empty)
            throw new LegacyLogicalException("'Created By' cannot be null");
        if (!await metadata.IsLiveMemberIdAsync(property.CreatedBy, cancellationToken))
            throw new LegacyLogicalException("Invalid user id");
        if (property.IsSystem && property.Deleted.HasValue)
            throw new LegacyLogicalException("System properties cannot be deleted");
        if (property.IsDirty && property.LastModifiedBy == Guid.Empty)
            throw new LegacyLogicalException("'Last Modified By' cannot be null");
    }
}

/// <summary>
/// PickListLayer.Save (spec M6-M 3.3): validate the list, upsert it when dirty, validate every value then upsert the
/// dirty ones, then upsert every dependency. The list row is written before the values are validated.
/// </summary>
public sealed class PickListSaver(IMetadataWriteProcedures writes, IMetadataProcedures metadata)
{
    public async Task<bool> SaveAsync(PickListContract pickList, CancellationToken cancellationToken)
    {
        await ValidateAsync(pickList, cancellationToken);
        if (pickList.IsDirty)
            await writes.SavePickListAsync(pickList, cancellationToken);

        foreach (var value in pickList.PickListValues)
            await ValidateValueAsync(value, cancellationToken);
        foreach (var value in pickList.PickListValues)
            if (value.IsDirty)
                await writes.SavePickListValueAsync(value, cancellationToken);

        foreach (var dependency in pickList.Dependencies)
            await writes.SavePickListDependencyAsync(dependency, cancellationToken);
        return true;
    }

    public async Task ValidateAsync(PickListContract pickList, CancellationToken cancellationToken)
    {
        if (pickList.Id == Guid.Empty)
            throw new LegacyLogicalException("Id cannot be null");
        if (string.IsNullOrEmpty(pickList.Name))
            throw new LegacyLogicalException("Name must be defined");
        if (string.IsNullOrEmpty(pickList.Description))
            throw new LegacyLogicalException("Description must be defined");
        if (pickList.CreatedBy == Guid.Empty)
            throw new LegacyLogicalException("'Created By' cannot be null");
        if (pickList.IsNew && !await metadata.IsLiveMemberIdAsync(pickList.CreatedBy, cancellationToken))
            throw new LegacyLogicalException("Invalid user id");
    }

    private async Task ValidateValueAsync(PickListValueContract value, CancellationToken cancellationToken)
    {
        if (value.Id == Guid.Empty)
            throw new LegacyLogicalException("Id cannot be null");
        if (value.PickListId == Guid.Empty)
            throw new LegacyLogicalException("PickListId must be defined");
        if (!await writes.IsLivePickListAsync(value.PickListId, cancellationToken))
            throw new LegacyLogicalException("Invalid picklist");
        if (string.IsNullOrEmpty(value.Value))
            throw new LegacyLogicalException("Value must be defined");
        if (value.CreatedBy == Guid.Empty)
            throw new LegacyLogicalException("'Created By' cannot be null");
        if (!await metadata.IsLiveMemberIdAsync(value.CreatedBy, cancellationToken))
            throw new LegacyLogicalException("Invalid user id");
        if (value.IsDirty && !value.IsNew && !value.LastModifiedBy.HasValue)
            throw new LegacyLogicalException("'Last Modified By' cannot be null");
    }
}
