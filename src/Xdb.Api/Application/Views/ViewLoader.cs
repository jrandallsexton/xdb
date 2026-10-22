using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Metadata;
using Xdb.Api.Application.Metadata.Legacy;
using Xdb.Api.Application.Views.Legacy;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Views;

/// <summary>
/// The original's ViewLayer.Get(viewId, userId) and the client model built from it. Property values changed after
/// loading go through <see cref="PropertyEdits"/>, which reproduces the original's dirty tracking: the first change
/// marks the property dirty and stamps LastModified with the current local time.
/// </summary>
public sealed class ViewLoader(IViewProcedures views, IMetadataProcedures metadata, MetadataLoader metadataLoader, IDateTimeProvider clock)
{
    private const int RestrictionTriggerType = 4;
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    /// <summary>Thrown as the original's LogicalException (the legacy wire returns the generic fault).</summary>
    public sealed class LogicalException(string message) : Exception(message);

    public async Task<ViewContract> ViewAsync(Guid viewId, Guid userId, CancellationToken cancellationToken)
    {
        var row = await views.GetViewAsync(viewId, cancellationToken) ?? throw new LogicalException("Invalid view id");
        var view = new ViewContract
        {
            Id = viewId,
            Name = row.Name ?? string.Empty,
            DisplayValue = row.DisplayValue ?? string.Empty,
            Description = row.Description ?? string.Empty,
            IsInstance = row.IsInstance ?? false,
            AssetTypeId = row.AssetTypeId,
            DriverCaption = row.DriverCaption ?? string.Empty,
            NewItemCaption = row.NewItemCaption ?? string.Empty,
            ConfirmationLabel = row.ConfirmationLabel ?? string.Empty,
            IsReadOnly = row.IsReadOnly ?? false,
            IsStandAlone = row.IsStandAlone ?? true,
            IsCreation = row.IsCreation ?? false,
            FilterLogic = row.FilterLogic,
            Created = row.Created,
            LastModified = row.LastModified,
            Deleted = row.Deleted,
            CreatedBy = row.CreatedBy,
            LastModifiedBy = row.LastModifiedBy,
            DeletedBy = row.DeletedBy
        };

        foreach (var relation in await views.GetViewPropertyGroupRelationsAsync(viewId, cancellationToken))
        {
            view.PropertyGroupMembers.Add(new ViewPropertyGroupRelationContract
            {
                Id = relation.Id, ViewId = relation.ViewId, PropertyGroupId = relation.PropertyGroupId, Order = relation.Order,
                Created = relation.Created, CreatedBy = relation.CreatedBy
            });
        }

        foreach (var group in await views.GetViewPropertyGroupsAsync(viewId, cancellationToken))
        {
            var contract = new PropertyGroupContract
            {
                Id = group.Id,
                Name = group.Name ?? string.Empty,
                DisplayValue = group.DisplayValue ?? string.Empty,
                AssetTypeId = group.AssetTypeId ?? Guid.Empty,
                IsInstance = group.IsInstance ?? false,
                Created = group.Created ?? default,
                LastModified = group.LastModified,
                CreatedBy = group.CreatedBy ?? Guid.Empty,
                LastModifiedBy = group.LastModifiedBy
            };
            foreach (var member in await views.GetPropertyGroupRelationsAsync(group.Id, cancellationToken))
                contract.PropertyMembers.Add(GroupRelation(member));
            view.PropertyGroups.Add(contract);
        }

        // Properties are read after all groups, as the original did.
        foreach (var group in view.PropertyGroups)
        {
            foreach (var property in await views.GetPropertyGroupPropertiesAsync(group.Id, cancellationToken))
                group.Properties.Add(GroupProperty(property));
        }

        foreach (var filter in await views.GetViewFiltersAsync(viewId, cancellationToken))
        {
            if (filter.OperatorId is < 0 or > 6)
                throw new InvalidOperationException("Invalid value for EFilterOperator");
            view.Filters.Add(new FilterContract
            {
                Id = filter.Id, PropertyId = filter.PropertyId, Property = filter.Property, OperatorId = filter.OperatorId,
                Operator = filter.Operator, Value = filter.Value, Order = filter.Order
            });
        }

        await AttachTriggersAsync(view, cancellationToken);

        var edits = new PropertyEdits(clock);
        var properties = view.PropertyGroups.SelectMany(g => g.Properties).ToList();
        foreach (var property in properties)
        {
            if (property.IsSystem)
            {
                if (property.SystemType == (int)SystemType.InstanceOf)
                    edits.DisplayValue(property, await metadata.GetReportingLabelAsync(view.AssetTypeId, forInstances: false, cancellationToken));
            }
            else if (property.DataType == (int)LegacyDataType.Relation_ChildParent)
            {
                if (await metadata.GetParentAssetTypeIdAsync(view.AssetTypeId, cancellationToken) is { } parent)
                    edits.AssetTypeId(property, parent);
            }
            else if (property.DataType == (int)LegacyDataType.System_InstanceOfDesc)
            {
                edits.DisplayValue(property, await metadata.GetReportingLabelAsync(view.AssetTypeId, forInstances: false, cancellationToken) + " Description");
            }
        }

        var assetType = await metadataLoader.AssetTypeAsync(view.AssetTypeId, cancellationToken)
            ?? throw new LogicalException("Invalid AssetType associated with view.");

        var isAdmin = await metadata.IsAdminAsync(userId, SystemRoleIds.Admin, cancellationToken);
        view.AllowNewValues = isAdmin || await CanAddAssetsAsync(userId, view.AssetTypeId, cancellationToken);
        foreach (var property in properties)
        {
            var relation = assetType.PropertyMembers.FirstOrDefault(r => r.PropertyId == property.Id);
            if (relation is not null)
            {
                edits.IsInherited(property, relation.IsInherited);
                edits.IsInheritedValue(property, relation.IsInheritedValue);
                edits.IsInstance(property, relation.IsInstance);
                if (property.IsInheritedValue)
                    edits.IsReadOnly(property, true);
                if (view.IsInstance && !property.IsInstance)
                    edits.IsReadOnly(property, true);
            }

            if (property.DataType == (int)LegacyDataType.Asset && property.AssetTypeId is { } target && property.AssetTypeIsInstance.HasValue)
                property.AllowNewValues = isAdmin || await CanAddAssetsAsync(userId, target, cancellationToken);
            else if (property.DataType == (int)LegacyDataType.User)
                property.AllowNewValues = isAdmin || await CanAddAssetsAsync(userId, SystemAssetTypeIds.User, cancellationToken);
            else if (property.IsSystem && property.SystemType == (int)SystemType.AssetName)
            {
                if (!string.IsNullOrEmpty(view.DriverCaption))
                    edits.DisplayValue(property, view.DriverCaption);
            }
            else
                property.AllowNewValues = isAdmin;
        }

        return view;
    }

    /// <summary>The client model: the view's display fields, its groups and properties, the member's quick filters.</summary>
    public async Task<ViewClientContract> ViewClientAsync(Guid viewId, Guid userId, CancellationToken cancellationToken)
    {
        var view = await ViewAsync(viewId, userId, cancellationToken);
        var client = new ViewClientContract
        {
            Id = view.Id,
            Name = string.IsNullOrEmpty(view.DisplayValue) ? view.Name : view.DisplayValue,
            AssetTypeId = view.AssetTypeId,
            IsInstance = view.IsInstance,
            IsReadOnly = view.IsReadOnly,
            IsCreation = view.IsCreation,
            AllowNewValues = view.AllowNewValues,
            DriverCaption = view.DriverCaption,
            NewItemCaption = view.NewItemCaption,
            LastModified = (view.LastModified ?? view.Created).ToString("M/d/yyyy h:mm:ss tt", EnUs),
            PrefetchAssets = !view.IsInstance
        };
        foreach (var group in view.PropertyGroups)
        {
            client.PropertyGroups.Add(new PropertyGroupClientContract
            {
                Id = group.Id,
                Name = string.IsNullOrEmpty(group.DisplayValue) ? group.Name : group.DisplayValue,
                Properties = group.Properties.Select(PropertyClient).ToList()
            });
        }
        foreach (var (id, name) in await views.GetMemberFiltersAsync(viewId, userId, cancellationToken))
            client.Filters.Add(id, name);
        return client;
    }

    public static PropertyClientContract PropertyClient(PropertyContract property) => new()
    {
        Id = property.Id,
        Name = string.IsNullOrEmpty(property.DisplayValue) ? property.Name : property.DisplayValue,
        AllowMultiValue = property.AllowMultiValue,
        AllowNewValues = property.AllowNewValues,
        AssetTypeId = property.AssetTypeId,
        AssetTypeIsInstance = property.AssetTypeIsInstance,
        DataType = property.DataType,
        DefaultValue = property.DefaultValue,
        Description = property.Description,
        IsInherited = property.IsInherited,
        IsInheritedValue = property.IsInheritedValue,
        IsInstance = property.IsInstance,
        IsOrdered = property.IsOrdered,
        IsReadOnly = property.IsReadOnly,
        IsRelationship = property.IsRelationship,
        IsRequired = property.IsRequired,
        IsSystem = property.IsSystem,
        PickListId = property.PickListId,
        Precision = property.Precision,
        RoleId = property.RoleId,
        SystemType = property.SystemType,
        Triggers = property.Triggers is null ? [] : [.. property.Triggers],
        UsesRole = property.UsesRole
    };

    /// <summary>MemberCanAddAssets after its admin check: the Create permission through RolesMembers.</summary>
    private async Task<bool> CanAddAssetsAsync(Guid userId, Guid assetTypeId, CancellationToken cancellationToken) =>
        await metadata.IsAdminAsync(userId, SystemRoleIds.Admin, cancellationToken)
        || await views.CountCreatePermissionsAsync(userId, assetTypeId, cancellationToken) > 0;

    private async Task AttachTriggersAsync(ViewContract view, CancellationToken cancellationToken)
    {
        foreach (var triggerId in await views.GetViewTriggerIdsAsync(view.Id, cancellationToken))
        {
            var trigger = await views.GetTriggerAsync(triggerId, cancellationToken);
            if (trigger is null)
                continue;
            if (!int.TryParse(trigger.TypeId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var type) || type != RestrictionTriggerType)
                continue;
            foreach (var restriction in await views.GetTriggerRestrictionsAsync(triggerId, cancellationToken))
            {
                var property = view.PropertyGroups.SelectMany(g => g.Properties).FirstOrDefault(p => p.Id == restriction.PropertyId);
                if (property is null)
                    continue;
                (property.Triggers ??= []).Add(new PropertyTriggerContract
                {
                    ParentPropertyId = restriction.PropertyId,
                    TargetPropertyId = restriction.RestrictedPropertyId,
                    SourcePropertyId = restriction.SourcePropertyId ?? Guid.Empty,
                    TriggerType = 0
                });
            }
        }
    }

    private static PropertyGroupPropertyRelationContract GroupRelation(PropertyGroupPropertyRelationRow row) => new()
    {
        Id = row.Id,
        PropertyGroupId = row.PropertyGroupId,
        PropertyId = row.PropertyId,
        Order = row.Order,
        IsRequired = row.IsRequired ?? false,
        IsReadOnly = row.IsReadOnly ?? false,
        DefaultValue = row.DefaultValue ?? string.Empty,
        Created = row.Created ?? default,
        CreatedBy = row.CreatedBy,
        DeletedBy = row.DeletedBy
    };

    private static PropertyContract GroupProperty(GroupPropertyRow row)
    {
        var property = MetadataLoader.Property(new PropertyDetailRow
        {
            Id = row.Id, DataTypeId = row.DataTypeId, Precision = row.Precision, PickListId = row.PickListId, RoleId = row.RoleId,
            IsOrdered = row.IsOrdered, AllowMultiValue = row.AllowMultiValue, Name = row.Name, DisplayValue = row.DisplayValue,
            Description = row.Description, AssetTypeId = row.AssetTypeId, AssetTypeIsInstance = row.AssetTypeIsInstance, Created = row.Created,
            Approved = row.Approved, LastModified = row.LastModified, CreatedBy = row.CreatedBy, ApprovedBy = row.ApprovedBy,
            LastModifiedBy = row.LastModifiedBy, Deleted = row.Deleted, DeletedBy = row.DeletedBy, IsSystem = row.IsSystem, SystemTypeId = row.SystemTypeId
        });
        // Read during the load (before IsNew is cleared), so these do not mark the property dirty.
        if (row.IsRequired is { } required)
            property.IsRequired = required;
        if (row.IsReadOnly is { } readOnly)
            property.IsReadOnly = readOnly;
        if (row.DefaultValue is { } defaultValue)
            property.DefaultValue = defaultValue;
        return property;
    }
}

/// <summary>
/// The original Property setters after loading: a value that changes marks the property dirty, and the first such
/// change sets LastModified to the current local time.
/// </summary>
public sealed class PropertyEdits(IDateTimeProvider clock)
{
    public void DisplayValue(PropertyContract property, string value)
    {
        if (property.DisplayValue != value)
        {
            property.DisplayValue = value;
            Dirty(property);
        }
    }

    public void AssetTypeId(PropertyContract property, Guid value)
    {
        if (!property.AssetTypeId.HasValue || property.AssetTypeId.Value != value)
        {
            property.AssetTypeId = value;
            Dirty(property);
        }
    }

    public void IsInherited(PropertyContract property, bool value) => Flag(property, property.IsInherited, value, v => property.IsInherited = v);

    public void IsInheritedValue(PropertyContract property, bool value) => Flag(property, property.IsInheritedValue, value, v => property.IsInheritedValue = v);

    public void IsInstance(PropertyContract property, bool value) => Flag(property, property.IsInstance, value, v => property.IsInstance = v);

    public void IsReadOnly(PropertyContract property, bool value) => Flag(property, property.IsReadOnly, value, v => property.IsReadOnly = v);

    public void IsRequired(PropertyContract property, bool value) => Flag(property, property.IsRequired, value, v => property.IsRequired = v);

    public void DefaultValue(PropertyContract property, string value)
    {
        if (property.DefaultValue != value)
        {
            property.DefaultValue = value;
            Dirty(property);
        }
    }

    private void Flag(PropertyContract property, bool current, bool value, Action<bool> set)
    {
        if (current != value)
        {
            set(value);
            Dirty(property);
        }
    }

    private void Dirty(PropertyContract property)
    {
        if (property.IsDirty)
            return;
        property.IsDirty = true;
        property.LastModified = clock.UtcNow().ToLocalTime();
    }
}
