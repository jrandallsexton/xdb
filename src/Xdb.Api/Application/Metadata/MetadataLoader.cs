using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Metadata.Legacy;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Metadata;

/// <summary>
/// Builds the original's metadata objects (AssetTypeLayer.Get, PickListLayer.Get, the property readers) from the
/// gateway rows, with the original's defaults: "" for NULL strings, the request id as the object id, IsNew and
/// IsDirty false.
/// </summary>
public sealed class MetadataLoader(IMetadataProcedures procedures, MetadataRules rules, IDateTimeProvider dateTimeProvider)
{
    /// <summary>AssetTypeLayer.Get: null for a missing or deleted type.</summary>
    public async Task<AssetTypeContract?> AssetTypeAsync(Guid assetTypeId, CancellationToken cancellationToken)
    {
        var row = await procedures.GetAssetTypeAsync(assetTypeId, cancellationToken);
        if (row is null)
            return null;

        var assetType = new AssetTypeContract
        {
            Id = assetTypeId,
            Name = row.Name,
            Pluralization = row.Pluralization ?? string.Empty,
            DefaultViewId = row.DefaultViewId,
            DefaultInstanceViewId = row.DefaultInstanceViewId,
            DefinitionLabel = row.DefinitionLabel,
            InstanceLabel = row.InstanceLabel,
            ImageId = row.ImageId,
            AllowAssets = row.AllowAssets ?? false,
            Created = row.Created,
            CreatedBy = row.CreatedBy,
            ParentId = await procedures.GetParentAssetTypeIdAsync(assetTypeId, cancellationToken)
        };

        foreach (var relation in await procedures.GetAssetTypePropertyRelationsAsync(assetTypeId, cancellationToken))
        {
            assetType.PropertyMembers.Add(new AssetTypePropertyRelationContract
            {
                Id = relation.Id,
                AssetTypeId = relation.AssetTypeId,
                PropertyId = relation.PropertyId,
                IsInstance = relation.IsInstance,
                IsInherited = relation.IsInherited,
                IsInheritedValue = relation.IsInheritedValue,
                Created = relation.Created,
                CreatedBy = relation.CreatedBy
            });
        }

        foreach (var property in await procedures.GetAssetTypePropertiesAsync(assetTypeId, cancellationToken))
        {
            var contract = Property(property);
            contract.IsInherited = property.IsInherited ?? false;
            contract.IsInheritedValue = property.IsInheritedValue ?? false;
            assetType.Properties.Add(contract);
        }

        return assetType;
    }

    /// <summary>The original's property mapping (PropertyDal readers).</summary>
    public static PropertyContract Property(PropertyDetailRow row)
    {
        // The original unboxed the tinyint Precision as a short, which fails for any non-NULL value.
        if (row.Precision is not null)
            throw new InvalidCastException("Specified cast is not valid.");

        // The original's EDataType: 0-17 and 20-25 (Geo is 25); any other number read as Undefined (-1).
        var dataType = row.DataTypeId is { } d && (d <= 17 || d is >= 20 and <= 25) ? d : -1;
        var systemType = row.SystemTypeId is { } s && Enum.IsDefined(typeof(SystemType), s) ? s : (int)SystemType.NotApplicable;
        return new PropertyContract
        {
            Id = row.Id,
            DataType = row.DataTypeId is null ? -1 : dataType,
            PickListId = row.PickListId,
            RoleId = row.RoleId,
            IsOrdered = row.IsOrdered ?? false,
            AllowMultiValue = row.AllowMultiValue ?? false,
            Name = row.Name ?? string.Empty,
            DisplayValue = row.DisplayValue ?? string.Empty,
            Description = row.Description ?? string.Empty,
            AssetTypeId = row.AssetTypeId,
            AssetTypeIsInstance = row.AssetTypeIsInstance,
            Created = row.Created,
            Approved = row.Approved,
            LastModified = row.LastModified,
            CreatedBy = row.CreatedBy,
            ApprovedBy = row.ApprovedBy,
            LastModifiedBy = row.LastModifiedBy,
            Deleted = row.Deleted,
            DeletedBy = row.DeletedBy,
            IsSystem = row.IsSystem ?? false,
            SystemType = row.SystemTypeId is null ? (int)SystemType.NotApplicable : systemType,
            UsesRole = row.RoleId.HasValue && row.RoleId != Guid.Empty
        };
    }

    /// <summary>PickListLayer.Get: the list, its live values and its dependencies; null when absent.</summary>
    public async Task<PickListContract?> PickListAsync(Guid pickListId, CancellationToken cancellationToken)
    {
        var row = await procedures.GetPickListAsync(pickListId, cancellationToken);
        if (row is null)
            return null;

        var pickList = new PickListContract
        {
            Id = pickListId,
            Name = row.Name ?? string.Empty,
            DisplayValue = row.DisplayValue ?? string.Empty,
            Description = row.Description ?? string.Empty,
            IsMemberList = row.IsMemberList ?? false,
            AllowNewValues = row.AllowNewValues ?? false,
            ServiceUrl = row.ServiceUrl ?? string.Empty,
            ServiceMethod = row.ServiceMethod ?? string.Empty,
            ServiceUsername = row.ServiceUsername ?? string.Empty,
            ServicePassword = row.ServicePassword ?? string.Empty,
            Created = row.Created,
            CreatedBy = row.CreatedBy,
            Approved = row.Approved,
            ApprovedBy = row.ApprovedBy,
            // The original's load left the object dirty, and clearing IsDirty at the end stamped LastModified with the
            // current local time: the stored value never reached the wire.
            LastModified = LocalNow(),
            LastModifiedBy = row.LastModifiedBy,
            Deleted = row.Deleted,
            DeletedBy = row.DeletedBy,
            PickListValues = await PickListValuesAsync(pickListId, cancellationToken)
        };

        foreach (var dependency in await procedures.GetPickListDependenciesAsync(pickListId, cancellationToken))
        {
            pickList.Dependencies.Add(new PickListDependencyContract
            {
                Id = dependency.Id,
                ParentPickListId = dependency.ParentPickListId,
                ParentPickListValueId = dependency.ParentPickListValueId,
                ChildPickListId = dependency.ChildPickListId,
                ChildPickListValueId = dependency.ChildPickListValueId,
                Created = dependency.Created,
                CreatedBy = dependency.CreatedBy
            });
        }

        return pickList;
    }

    /// <summary>PickListValueLayer.GetCollection: live values by [Order]; the list id is the requested one; Deleted is never mapped.</summary>
    public async Task<List<PickListValueContract>> PickListValuesAsync(Guid pickListId, CancellationToken cancellationToken) =>
        (await procedures.GetPickListValuesAsync(pickListId, cancellationToken)).Select(v => new PickListValueContract
        {
            Id = v.Id,
            PickListId = pickListId,
            Value = v.Value ?? string.Empty,
            DisplayValue = v.DisplayValue ?? string.Empty,
            BGColor = v.BGColor ?? string.Empty,
            Order = v.Order,
            Created = v.Created,
            Approved = v.Approved,
            LastModified = LocalNow(), // stamped when the load cleared IsDirty, as for the list itself
            CreatedBy = v.CreatedBy,
            ApprovedBy = v.ApprovedBy,
            LastModifiedBy = v.LastModifiedBy,
            DeletedBy = v.DeletedBy
        }).ToList();

    private DateTime LocalNow() => dateTimeProvider.UtcNow().ToLocalTime();

    /// <summary>
    /// AssetTypeDal.AssetTypes_GetDictionary(parent, includeAllDescendants: true): every live descendant (the parent
    /// removed), en-US sorted by name and de-duplicated by name.
    /// </summary>
    public async Task<List<KeyValuePair<Guid, string>>> DescendantDictionaryAsync(Guid parentAssetTypeId, CancellationToken cancellationToken)
    {
        var children = await rules.ChildrenAsync(parentAssetTypeId, includeAllDescendants: true, cancellationToken);
        children.Remove(parentAssetTypeId);
        var named = new List<KeyValuePair<Guid, string>>();
        foreach (var child in children)
            named.Add(new KeyValuePair<Guid, string>(child, await procedures.GetAssetTypeNameAsync(child, cancellationToken)));
        return MetadataRules.SortByName(named);
    }
}
