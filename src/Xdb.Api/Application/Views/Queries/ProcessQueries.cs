using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Metadata;
using Xdb.Api.Application.Views.Legacy;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Views.Queries;

public sealed record GetProcessQuery(Guid ProcessId);

public interface IGetProcessQueryHandler
{
    Task<Result<ProcessClientContract?>> ExecuteAsync(GetProcessQuery query, CancellationToken cancellationToken = default);
}

/// <summary>A process with its steps (no ORDER BY) and their filters; deleted processes included; null when absent.</summary>
public sealed class GetProcessQueryHandler(IViewProcedures views) : IGetProcessQueryHandler
{
    public async Task<Result<ProcessClientContract?>> ExecuteAsync(GetProcessQuery query, CancellationToken cancellationToken = default)
    {
        var process = await views.GetProcessAsync(query.ProcessId, cancellationToken);
        if (process is null)
            return new Success<ProcessClientContract?>(null);

        var client = new ProcessClientContract
        {
            Id = process.Id.ToString("D"),
            Name = string.IsNullOrEmpty(process.DisplayValue) ? process.Name : process.DisplayValue,
            AssetTypeId = process.AssetTypeId?.ToString("D")
        };
        foreach (var step in await views.GetProcessStepsAsync(process.Id, cancellationToken))
        {
            client.Steps.Add(new ProcessStepClientContract
            {
                Id = step.Id.ToString("D"), Name = step.Name, Order = step.Order, ViewId = step.ViewId?.ToString("D"),
                IsInstance = step.IsInstance, IsCreation = step.IsCreation, IsEdit = step.IsEdit,
                ShowAssetTypeSelector = step.ShowAssetTypeSelector, AssetNameLabel = step.AssetNameLabel,
                Filters = (await views.GetProcessStepFiltersAsync(step.Id, cancellationToken))
                    .Select(f => new ProcessStepFilterClientContract { Id = f.Id.ToString("D"), Order = f.Order, PropertyId = f.PropertyId.ToString("D") })
                    .ToList()
            });
        }
        return new Success<ProcessClientContract?>(client);
    }
}

public sealed record GetProcessStepLabelQuery(Guid ProcessId, int StepIndex);

public interface IGetProcessStepLabelQueryHandler
{
    Task<Result<string>> ExecuteAsync(GetProcessStepLabelQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetProcessStepLabelQueryHandler(IViewProcedures views) : IGetProcessStepLabelQueryHandler
{
    public async Task<Result<string>> ExecuteAsync(GetProcessStepLabelQuery query, CancellationToken cancellationToken = default) =>
        new Success<string>(await views.GetProcessStepAssetNameLabelAsync(query.ProcessId, query.StepIndex, cancellationToken));
}

/// <summary>The step flag columns the four ProcessStep_Is* operations read.</summary>
public enum ProcessStepFlag
{
    IsCreation,
    IsEdit,
    IsInstance,
    ShowAssetTypeSelector
}

public sealed record GetProcessStepFlagQuery(Guid ProcessId, int StepIndex, ProcessStepFlag Flag);

public interface IGetProcessStepFlagQueryHandler
{
    Task<Result<bool>> ExecuteAsync(GetProcessStepFlagQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetProcessStepFlagQueryHandler(IViewProcedures views) : IGetProcessStepFlagQueryHandler
{
    public async Task<Result<bool>> ExecuteAsync(GetProcessStepFlagQuery query, CancellationToken cancellationToken = default) =>
        new Success<bool>(await views.GetProcessStepFlagAsync(query.ProcessId, query.StepIndex, query.Flag.ToString(), cancellationToken));
}

public sealed record GetProcessStepFiltersQuery(Guid ProcessId, int StepIndex);

public interface IGetProcessStepFiltersQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<int, Guid>>>> ExecuteAsync(GetProcessStepFiltersQuery query, CancellationToken cancellationToken = default);
}

/// <summary>(filter order, property id) for the step; empty when there is no step.</summary>
public sealed class GetProcessStepFiltersQueryHandler(IViewProcedures views) : IGetProcessStepFiltersQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<int, Guid>>>> ExecuteAsync(GetProcessStepFiltersQuery query, CancellationToken cancellationToken = default)
    {
        var stepId = await views.GetProcessStepIdAsync(query.ProcessId, query.StepIndex, cancellationToken);
        return new Success<IReadOnlyList<KeyValuePair<int, Guid>>>(stepId == Guid.Empty ? [] : await views.GetProcessStepFilterPropertiesAsync(stepId, cancellationToken));
    }
}

public sealed record GetFilterValuesQuery(Guid PropertyId);

public interface IGetFilterValuesQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetFilterValuesQuery query, CancellationToken cancellationToken = default);
}

/// <summary>ProcessStep_GetFilterValues: the picklist values a picklist property has been given (deleted values included).</summary>
public sealed class GetFilterValuesQueryHandler(IViewProcedures views, IMetadataProcedures metadata) : IGetFilterValuesQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetFilterValuesQuery query, CancellationToken cancellationToken = default)
    {
        var property = await metadata.GetPropertyAsync(query.PropertyId, cancellationToken);
        if (property is null || MetadataLoader.Property(property).DataType != (int)LegacyDataType.PickList)
            return new Success<IReadOnlyList<KeyValuePair<Guid, string>>>([]);
        return new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await views.GetUsedPickListValuesAsync(query.PropertyId, cancellationToken));
    }
}

public sealed record GetNextFilterValuesQuery(Guid PropertyId, string? SelectedValue, Guid NextPropertyId);

public interface IGetNextFilterValuesQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetNextFilterValuesQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// ProcessStep_GetNextFilterValues: the next property's picklist values among the assets that have the selected value.
/// The original embedded the selected value in its SQL; it is parameterized here (recorded deviation), and a value
/// containing an apostrophe fails as the original's broken SQL did.
/// </summary>
public sealed class GetNextFilterValuesQueryHandler(IViewProcedures views) : IGetNextFilterValuesQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetNextFilterValuesQuery query, CancellationToken cancellationToken = default)
    {
        if (query.SelectedValue?.Contains('\'') == true)
            throw new InvalidOperationException("The selected value would have broken the original's SQL literal.");
        var assets = await views.GetAssetsByPropertyValueAsync(query.PropertyId, query.SelectedValue, cancellationToken);
        if (assets.Count == 0)
            return new Success<IReadOnlyList<KeyValuePair<Guid, string>>>([]);
        return new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await views.GetUsedPickListValuesForAssetsAsync(assets, query.NextPropertyId, cancellationToken));
    }
}

public sealed record GetProcessStepViewQuery(Guid ProcessId, int StepIndex);

public interface IGetProcessStepViewQueryHandler
{
    Task<Result<Guid>> ExecuteAsync(GetProcessStepViewQuery query, CancellationToken cancellationToken = default);
}

/// <summary>ViewId_GetByProcessId: the step's view id, or the empty Guid.</summary>
public sealed class GetProcessStepViewQueryHandler(IMetadataProcedures metadata) : IGetProcessStepViewQueryHandler
{
    public async Task<Result<Guid>> ExecuteAsync(GetProcessStepViewQuery query, CancellationToken cancellationToken = default) =>
        new Success<Guid>(await metadata.GetProcessStepViewIdAsync(query.ProcessId, query.StepIndex, cancellationToken) ?? Guid.Empty);
}

public sealed record GetHelpSectionQuery(Guid HelpSectionId);

public interface IGetHelpSectionQueryHandler
{
    Task<Result<HelpSectionContract?>> ExecuteAsync(GetHelpSectionQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetHelpSectionQueryHandler(IViewProcedures views) : IGetHelpSectionQueryHandler
{
    public async Task<Result<HelpSectionContract?>> ExecuteAsync(GetHelpSectionQuery query, CancellationToken cancellationToken = default) =>
        new Success<HelpSectionContract?>(await views.GetHelpSectionAsync(query.HelpSectionId, cancellationToken) is { } row
            ? new HelpSectionContract { Id = query.HelpSectionId, Title = row.Title, ImageId = row.ImageId, Order = row.Order, Content = row.Content }
            : null);
}

public sealed record GetHelpSectionsQuery;

public interface IGetHelpSectionsQueryHandler
{
    Task<Result<List<HelpSectionContract?>>> ExecuteAsync(GetHelpSectionsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Every help section by [Order], loaded one by one (a section gone in between is null).</summary>
public sealed class GetHelpSectionsQueryHandler(IViewProcedures views) : IGetHelpSectionsQueryHandler
{
    public async Task<Result<List<HelpSectionContract?>>> ExecuteAsync(GetHelpSectionsQuery query, CancellationToken cancellationToken = default)
    {
        var sections = new List<HelpSectionContract?>();
        foreach (var id in await views.GetHelpSectionIdsAsync(cancellationToken))
        {
            sections.Add(await views.GetHelpSectionAsync(id, cancellationToken) is { } row
                ? new HelpSectionContract { Id = id, Title = row.Title, ImageId = row.ImageId, Order = row.Order, Content = row.Content }
                : null);
        }
        return new Success<List<HelpSectionContract?>>(sections);
    }
}

public sealed record GetPropertyGroupQuery(Guid PropertyGroupId);

public interface IGetPropertyGroupQueryHandler
{
    Task<Result<PropertyGroupContract?>> ExecuteAsync(GetPropertyGroupQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// A property group (deleted ones included) with its live relations and their properties. The relation's IsRequired,
/// IsReadOnly and DefaultValue are applied after loading, so a property they change is dirty with LastModified = now.
/// </summary>
public sealed class GetPropertyGroupQueryHandler(IViewProcedures views, IMetadataProcedures metadata, IDateTimeProvider clock) : IGetPropertyGroupQueryHandler
{
    public async Task<Result<PropertyGroupContract?>> ExecuteAsync(GetPropertyGroupQuery query, CancellationToken cancellationToken = default)
    {
        var row = await views.GetPropertyGroupAsync(query.PropertyGroupId, cancellationToken);
        if (row is null)
            return new Success<PropertyGroupContract?>(null);

        var group = new PropertyGroupContract
        {
            Id = query.PropertyGroupId,
            Name = row.Name ?? string.Empty,
            DisplayValue = row.DisplayValue ?? string.Empty,
            AssetTypeId = row.AssetTypeId ?? Guid.Empty,
            IsInstance = row.IsInstance ?? false,
            Created = row.Created ?? default,
            LastModified = row.LastModified,
            CreatedBy = row.CreatedBy ?? Guid.Empty,
            LastModifiedBy = row.LastModifiedBy,
            Deleted = row.Deleted,
            DeletedBy = row.DeletedBy
        };

        var edits = new PropertyEdits(clock);
        foreach (var relation in await views.GetPropertyGroupRelationsAsync(query.PropertyGroupId, cancellationToken))
        {
            if (group.PropertyMembers.Any(m => m.PropertyId == relation.PropertyId))
                continue;
            group.PropertyMembers.Add(new PropertyGroupPropertyRelationContract
            {
                Id = relation.Id, PropertyGroupId = relation.PropertyGroupId, PropertyId = relation.PropertyId, Order = relation.Order,
                IsRequired = relation.IsRequired ?? false, IsReadOnly = relation.IsReadOnly ?? false, DefaultValue = relation.DefaultValue ?? string.Empty,
                Created = relation.Created ?? default, CreatedBy = relation.CreatedBy, DeletedBy = relation.DeletedBy
            });

            var propertyRow = await metadata.GetPropertyAsync(relation.PropertyId, cancellationToken)
                ?? throw new NullReferenceException("Property not found.");
            var property = MetadataLoader.Property(propertyRow);
            edits.IsRequired(property, relation.IsRequired ?? false);
            edits.IsReadOnly(property, relation.IsReadOnly ?? false);
            edits.DefaultValue(property, relation.DefaultValue ?? string.Empty);
            group.Properties.Add(property);
        }
        return new Success<PropertyGroupContract?>(group);
    }
}
