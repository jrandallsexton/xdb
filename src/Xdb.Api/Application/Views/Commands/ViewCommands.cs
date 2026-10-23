using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Xdb.Api.Application.Common.Identity;
using Xdb.Api.Application.Common.Legacy;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Metadata.Commands;
using Xdb.Api.Application.Metadata.Queries.Lookups;
using Xdb.Api.Application.Views.Legacy;
using Xdb.Api.Application.Views.Queries;
using Xdb.Api.Infrastructure.Background;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Views.Commands;

/// <summary>PropertyGroupLayer.Save (spec M6-M 3.4): validate; when dirty, upsert the group then its dirty members.</summary>
public sealed class PropertyGroupSaver(IMetadataWriteProcedures writes, IMetadataProcedures metadata)
{
    public async Task<bool> SaveAsync(PropertyGroupContract group, CancellationToken cancellationToken)
    {
        await ValidateAsync(group, cancellationToken);
        if (!group.IsDirty)
            return true;
        await writes.SavePropertyGroupAsync(group, cancellationToken);
        foreach (var member in group.PropertyMembers)
            if (member.IsDirty)
                await writes.SavePropertyGroupRelationAsync(member, cancellationToken);
        return true;
    }

    public async Task ValidateAsync(PropertyGroupContract group, CancellationToken cancellationToken)
    {
        if (group.Id == Guid.Empty)
            throw new LegacyLogicalException("Id cannot be null");
        if (group.CreatedBy == Guid.Empty)
            throw new LegacyLogicalException("CreatedBy cannot be null");
        if (!await metadata.IsLiveMemberIdAsync(group.CreatedBy, cancellationToken))
            throw new LegacyLogicalException("Invalid user id");
        if (group.AssetTypeId == Guid.Empty)
            throw new LegacyLogicalException("Property group must be associated with an asset type");
        if (string.IsNullOrEmpty(group.Name))
            throw new LegacyLogicalException("Name is required");
    }
}

/// <summary>
/// ViewLayer.Save (spec M6-M 3.5): validate; when dirty, upsert the view and its dirty relations, delete its filters
/// and insert the posted ones (which always fails: the original binds an int to a uniqueidentifier column); a view
/// posted as new then gets the Admin role relation.
/// </summary>
public sealed class ViewSaver(IMetadataWriteProcedures writes, IMetadataProcedures metadata, IRoleProcedures roles, IDateTimeProvider clock)
{
    public async Task<bool> SaveAsync(ViewContract view, CancellationToken cancellationToken)
    {
        await ValidateAsync(view, cancellationToken);
        var isNew = view.IsNew;
        if (view.IsDirty)
        {
            await writes.SaveViewAsync(view, cancellationToken);
            foreach (var member in view.PropertyGroupMembers)
                if (member.IsDirty)
                    await writes.SaveViewGroupRelationAsync(member, cancellationToken);
            await writes.DeleteViewFiltersAsync(view.Id, cancellationToken);
            foreach (var filter in view.Filters)
                await writes.InsertViewFilterAsync(Guid.NewGuid(), view.Id, filter ?? throw new NullReferenceException("filter is null."), cancellationToken);
        }

        if (isNew)
        {
            await roles.SaveViewRelationAsync(new RoleViewRelationContract
            {
                Id = Guid.NewGuid(), RoleId = SystemRoleIds.Admin, ViewId = view.Id, CreatedBy = SystemMemberIds.Admin, Created = clock.UtcNow().ToLocalTime()
            }, cancellationToken);
        }
        return true;
    }

    public async Task ValidateAsync(ViewContract view, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(view.Name))
            throw new LegacyLogicalException("Name must be defined");
        if (string.IsNullOrEmpty(view.DisplayValue))
            throw new LegacyLogicalException("Display Value must be defined");
        if (string.IsNullOrEmpty(view.Description))
            throw new LegacyLogicalException("Description must be provided");
        if (string.IsNullOrEmpty(view.DriverCaption))
            throw new LegacyLogicalException("Driver Caption must be provided");
        if (string.IsNullOrEmpty(view.NewItemCaption))
            throw new LegacyLogicalException("New Item Caption must be provided");
        if (view.AssetTypeId == Guid.Empty)
            throw new LegacyLogicalException("Asset Type must be provided");
        foreach (var member in view.PropertyGroupMembers)
            if (!await writes.IsLivePropertyGroupAsync(member.PropertyGroupId, cancellationToken))
                throw new LegacyLogicalException($"Invalid property group: {member.PropertyGroupId:D}");
        if (!await metadata.IsLiveMemberIdAsync(view.CreatedBy, cancellationToken))
            throw new LegacyLogicalException("Invalid user id");
    }
}

/// <summary>One posted quick-filter item, as read from the search client.</summary>
public sealed record QuickFilterItem(string? PropertyId, int OperatorId, string? Value, int Order);

public sealed record SaveQuickFilterCommand(string? Id, string? UserId, string? ViewId, string? FilterName, string? FilterExpression, IReadOnlyList<QuickFilterItem>? Filters);

public interface IViewCommandHandler
{
    Task<Result<bool>> SavePropertyGroupAsync(PropertyGroupContract? group, CancellationToken cancellationToken = default);
    Task<Result<bool>> KillPropertyGroupAsync(Guid propertyGroupId, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> SaveViewAsync(ViewContract? view, CancellationToken cancellationToken = default);
    Task<Result<bool>> KillViewAsync(Guid viewId, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> SaveQuickFilterAsync(SaveQuickFilterCommand command, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteQuickFilterAsync(string? viewFilterId, CancellationToken cancellationToken = default);
}

public sealed class ViewCommandHandler(
    PropertyGroupSaver groups, ViewSaver views, IMetadataWriteProcedures writes, IMetadataProcedures metadata, IGetSearchFilterDisplayQueryHandler filterString,
    IBackgroundWorkQueue background) : IViewCommandHandler
{
    private const int MaxOperatorId = 6;

    public async Task<Result<bool>> SavePropertyGroupAsync(PropertyGroupContract? group, CancellationToken cancellationToken = default) =>
        new Success<bool>(await groups.SaveAsync(group ?? throw new NullReferenceException("propertyGroup is null."), cancellationToken));

    /// <summary>PropertyGroup_Kill: no handler around it, so a non-administrator is a plain fault.</summary>
    public async Task<Result<bool>> KillPropertyGroupAsync(Guid propertyGroupId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!await metadata.IsAdminAsync(userId, SystemRoleIds.Admin, cancellationToken))
            throw new InvalidOperationException("No permissions to kill objects");
        await writes.KillPropertyGroupAsync(propertyGroupId, cancellationToken);
        return new Success<bool>(true);
    }

    public async Task<Result<bool>> SaveViewAsync(ViewContract? view, CancellationToken cancellationToken = default) =>
        new Success<bool>(await views.SaveAsync(view ?? throw new NullReferenceException("view is null."), cancellationToken));

    /// <summary>View_Kill: inside the logical handler, so a non-administrator is a logical failure.</summary>
    public async Task<Result<bool>> KillViewAsync(Guid viewId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!await metadata.IsAdminAsync(userId, SystemRoleIds.Admin, cancellationToken))
            throw new LegacyLogicalException("No permissions to kill objects");
        await writes.KillViewAsync(viewId, cancellationToken);
        return new Success<bool>(true);
    }

    /// <summary>
    /// ViewFilter_Save (spec M6-M 3.6): build the filters (parse failures fault before any write), the filter text,
    /// upsert the member view, replace its filters; no filters answers false. Then, in the background, the member is
    /// marked updated and its cached client model of the view dropped.
    /// </summary>
    public async Task<Result<bool>> SaveQuickFilterAsync(SaveQuickFilterCommand command, CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterContract>();
        foreach (var item in command.Filters ?? throw new NullReferenceException("filters is null."))
        {
            var propertyId = Guid.Parse(item.PropertyId ?? throw new ArgumentNullException(nameof(command), "input is null."));
            if (item.OperatorId is < 0 or > MaxOperatorId)
                throw new InvalidOperationException("Invalid value for EFilterOperator");
            filters.Add(new FilterContract
            {
                Id = Guid.NewGuid(), PropertyId = propertyId, Property = string.Empty, OperatorId = item.OperatorId, Operator = "=", Value = item.Value!, Order = item.Order
            });
        }
        if (!Guid.TryParse(command.Id, out var id))
            id = Guid.NewGuid();
        var memberId = Guid.Parse(command.UserId ?? throw new ArgumentNullException(nameof(command), "input is null."));
        var viewId = Guid.Parse(command.ViewId ?? throw new ArgumentNullException(nameof(command), "input is null."));

        var text = (await filterString.ExecuteAsync(new GetSearchFilterDisplayQuery(command.FilterExpression,
            filters.Select(f => new SearchFilter(f.PropertyId, f.OperatorId, f.Value)).ToList()), cancellationToken)).Value;

        await writes.SaveMemberViewAsync(id, memberId, viewId, command.FilterName, command.FilterExpression, text, cancellationToken);
        await writes.DeleteMemberViewFiltersAsync(id, cancellationToken);
        if (filters.Count == 0)
            return new Success<bool>(false);
        foreach (var filter in filters)
            await writes.InsertMemberViewFilterAsync(Guid.NewGuid(), id, filter, cancellationToken);

        background.Enqueue($"member updated {memberId}", (services, ct) =>
            services.GetRequiredService<IDashboardProcedures>().MarkMemberUpdatedAsync(memberId, memberId, ct));
        background.Enqueue($"view client delete {viewId}/{memberId}", (services, ct) =>
            services.GetRequiredService<IMetadataWriteProcedures>().DeleteViewClientAsync(memberId, viewId, ct));
        return new Success<bool>(true);
    }

    /// <summary>ViewFilter_Delete: an unparseable id is a logical failure the operation does not catch (a plain fault).</summary>
    public async Task<Result<bool>> DeleteQuickFilterAsync(string? viewFilterId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(viewFilterId, out var id))
            throw new InvalidOperationException("Invalid format");
        await writes.DeleteMemberFilterAsync(id, cancellationToken);
        return new Success<bool>(true);
    }
}

public interface IViewCloneQueryHandler
{
    Task<Result<PropertyGroupContract?>> ClonePropertyGroupAsync(Guid propertyGroupId, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<ViewContract>> CloneViewAsync(Guid viewId, Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>PropertyGroup_Clone and View_Clone (spec M6-M 2): loads plus in-memory copies; nothing is written.</summary>
public sealed class ViewCloneQueryHandler(IGetPropertyGroupQueryHandler groups, ViewLoader loader, IDateTimeProvider clock) : IViewCloneQueryHandler
{
    private const int CaptionLimit = 100;

    private DateTime Now() => clock.UtcNow().ToLocalTime();

    public async Task<Result<PropertyGroupContract?>> ClonePropertyGroupAsync(Guid propertyGroupId, Guid userId, CancellationToken cancellationToken = default)
    {
        var group = (await groups.ExecuteAsync(new GetPropertyGroupQuery(propertyGroupId), cancellationToken)).Value;
        if (group is null)
            return new Success<PropertyGroupContract?>(null);

        group.Id = Guid.NewGuid();
        group.DisplayValue = string.Empty;
        group.Name = string.Empty;
        group.Created = Now();
        group.CreatedBy = userId;
        group.Deleted = null;
        group.DeletedBy = null;
        group.LastModified = null;
        group.LastModifiedBy = null;
        group.IsDirty = true;
        group.IsNew = true;
        foreach (var member in group.PropertyMembers)
        {
            member.Id = Guid.NewGuid();
            member.PropertyGroupId = group.Id;
            member.Created = group.Created;
            member.CreatedBy = userId;
            member.IsDirty = true;
            member.IsNew = true;
        }
        return new Success<PropertyGroupContract?>(group);
    }

    public async Task<Result<ViewContract>> CloneViewAsync(Guid viewId, Guid userId, CancellationToken cancellationToken = default)
    {
        var source = await loader.ViewAsync(viewId, userId, cancellationToken);
        var clone = new ViewContract
        {
            AllowCloning = false,
            Created = Now(),
            CreatedBy = userId,
            AssetTypeId = source.AssetTypeId,
            Description = source.Description,
            DisplayValue = Caption("Copy of " + source.DisplayValue),
            DriverCaption = source.DriverCaption,
            Id = Guid.NewGuid(),
            IsAutoApprove = source.IsAutoApprove,
            IsDirty = true,
            IsInstance = source.IsInstance,
            IsNew = true,
            IsReadOnly = source.IsReadOnly,
            Name = Caption("Copy of " + source.Name),
            NewItemCaption = source.NewItemCaption,
            FilterLogic = source.FilterLogic,
            Filters = [.. source.Filters.Select(f => new FilterContract
            {
                Id = Guid.NewGuid(), DisplayValue = f.DisplayValue, Operator = f.Operator, OperatorId = f.OperatorId, Order = f.Order, Property = f.Property,
                PropertyId = f.PropertyId, Value = f.Value
            })]
        };
        foreach (var member in source.PropertyGroupMembers)
        {
            clone.PropertyGroupMembers.Add(new ViewPropertyGroupRelationContract
            {
                Id = Guid.NewGuid(), Created = Now(), CreatedBy = userId, IsDirty = true, IsNew = true, Order = member.Order, PropertyGroupId = member.PropertyGroupId,
                ViewId = clone.Id
            });
            clone.PropertyGroups.Add((await groups.ExecuteAsync(new GetPropertyGroupQuery(member.PropertyGroupId), cancellationToken)).Value!);
        }
        return new Success<ViewContract>(clone);
    }

    /// <summary>The caption setter: longer than 100 characters keeps 99.</summary>
    private static string Caption(string text) => text.Length > CaptionLimit ? text[..(CaptionLimit - 1)] : text;
}

public interface IViewMigrateCommandHandler
{
    Task<Result<bool>> MigratePropertyGroupAsync(Guid propertyGroupId, int target, CancellationToken cancellationToken = default);
    Task<Result<bool>> MigrateViewAsync(Guid viewId, int target, CancellationToken cancellationToken = default);
}

/// <summary>MigratePropertyGroup and MigrateView stubs (spec M6-M 5.4, 5.5), as the metadata ones.</summary>
public sealed class ViewMigrateCommandHandler(
    IGetPropertyGroupQueryHandler groupLoader, ViewLoader viewLoader, IMetadataWriteProcedures writes, PropertyGroupSaver groups, ViewSaver views,
    IOptions<LegacyEnvironmentOptions> environment, IDateTimeProvider clock) : IViewMigrateCommandHandler
{
    private bool Own(int target) => MigrateBody.IsOwnInstance(target, environment.Value.ApplicationInstance);

    public async Task<Result<bool>> MigratePropertyGroupAsync(Guid propertyGroupId, int target, CancellationToken cancellationToken = default)
    {
        var group = (await groupLoader.ExecuteAsync(new GetPropertyGroupQuery(propertyGroupId), cancellationToken)).Value;
        if (group is null)
            return new Success<bool>(false);
        if (!Own(target))
        {
            if (group.Properties.Count == 0)
                await groups.ValidateAsync(group, cancellationToken);
            throw new MigrationTargetUnavailableException();
        }

        var missing = new List<string>();
        foreach (var property in group.Properties)
            if (!await writes.IsLivePropertyAsync(property.Id, cancellationToken))
                missing.Add(string.IsNullOrEmpty(property.DisplayValue) ? property.Name : property.DisplayValue);
        MetadataMigrateCommandHandler.ThrowMissing(missing, "property", "properties");

        foreach (var member in group.PropertyMembers)
        {
            member.IsDirty = true;
            member.IsNew = true;
        }
        group.IsDirty = true;
        group.IsNew = true;
        return new Success<bool>(await groups.SaveAsync(group, cancellationToken));
    }

    public async Task<Result<bool>> MigrateViewAsync(Guid viewId, int target, CancellationToken cancellationToken = default)
    {
        var view = await viewLoader.ViewRowsAsync(viewId, cancellationToken);
        if (view is null)
            return new Success<bool>(false);
        if (!Own(target))
        {
            if (view.PropertyGroups.Count == 0)
                await views.ValidateAsync(view, cancellationToken);
            throw new MigrationTargetUnavailableException();
        }

        var missing = new List<string>();
        foreach (var group in view.PropertyGroups)
            if (!await writes.IsLivePropertyGroupAsync(group.Id, cancellationToken))
                missing.Add(string.IsNullOrEmpty(group.DisplayValue) ? group.Name : group.DisplayValue);
        MetadataMigrateCommandHandler.ThrowMissing(missing, "property group", "property groups");

        foreach (var member in view.PropertyGroupMembers)
        {
            member.IsDirty = true;
            member.IsNew = true;
        }
        // IsDirty set while IsNew is still false stamps LastModified.
        view.IsDirty = true;
        view.LastModified = clock.UtcNow().ToLocalTime();
        view.IsNew = true;
        return new Success<bool>(await views.SaveAsync(view, cancellationToken));
    }
}
