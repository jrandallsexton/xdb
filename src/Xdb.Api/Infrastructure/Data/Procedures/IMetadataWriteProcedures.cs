using System;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Metadata.Legacy;
using Xdb.Api.Application.Views.Legacy;

namespace Xdb.Api.Infrastructure.Data.Procedures;

/// <summary>
/// The original's metadata and view write statements, one call each, with its parameter typing: a null value is not
/// sent (the procedure default applies, or the statement fails as it did), dates as datetime, enums as int. No
/// transactions: each call commits on its own.
/// </summary>
public interface IMetadataWriteProcedures
{
    // ---- Asset types ----
    Task SaveAssetTypeAsync(AssetTypeContract assetType, CancellationToken cancellationToken);
    Task DeleteAssetTypeRelationAsync(Guid parentId, Guid childId, DateTime deleted, Guid deletedBy, CancellationToken cancellationToken);
    Task InsertAssetTypeRelationAsync(Guid id, Guid parentId, Guid childId, DateTime created, Guid createdBy, CancellationToken cancellationToken);
    /// <summary>Soft-deletes the inherited row of a property in one type.</summary>
    Task DeleteInheritedMemberAsync(DateTime? deleted, Guid? deletedBy, Guid assetTypeId, Guid propertyId, CancellationToken cancellationToken);
    Task SaveAssetTypePropertyRelationAsync(AssetTypePropertyRelationContract relation, CancellationToken cancellationToken);

    // ---- Properties and picklists ----
    Task SavePropertyAsync(PropertyContract property, CancellationToken cancellationToken);
    Task<bool> IsLivePropertyAsync(Guid propertyId, CancellationToken cancellationToken);
    Task SavePickListAsync(PickListContract pickList, CancellationToken cancellationToken);
    Task<bool> IsLivePickListAsync(Guid pickListId, CancellationToken cancellationToken);
    Task SavePickListValueAsync(PickListValueContract value, CancellationToken cancellationToken);
    Task SavePickListDependencyAsync(PickListDependencyContract dependency, CancellationToken cancellationToken);

    // ---- Property groups ----
    Task SavePropertyGroupAsync(PropertyGroupContract group, CancellationToken cancellationToken);
    Task SavePropertyGroupRelationAsync(PropertyGroupPropertyRelationContract relation, CancellationToken cancellationToken);
    Task<bool> IsLivePropertyGroupAsync(Guid propertyGroupId, CancellationToken cancellationToken);
    Task KillPropertyGroupAsync(Guid propertyGroupId, CancellationToken cancellationToken);

    // ---- Views ----
    Task SaveViewAsync(ViewContract view, CancellationToken cancellationToken);
    Task SaveViewGroupRelationAsync(ViewPropertyGroupRelationContract relation, CancellationToken cancellationToken);
    Task DeleteViewFiltersAsync(Guid viewId, CancellationToken cancellationToken);
    /// <summary>The original insert: its int operator against the uniqueidentifier column always fails (Msg 206).</summary>
    Task InsertViewFilterAsync(Guid id, Guid viewId, FilterContract filter, CancellationToken cancellationToken);
    Task KillViewAsync(Guid viewId, CancellationToken cancellationToken);

    // ---- Quick filters ----
    Task SaveMemberViewAsync(Guid id, Guid memberId, Guid viewId, string? name, string? filterExpression, string? filterString, CancellationToken cancellationToken);
    Task DeleteMemberViewFiltersAsync(Guid memberViewId, CancellationToken cancellationToken);
    Task InsertMemberViewFilterAsync(Guid id, Guid memberViewId, FilterContract filter, CancellationToken cancellationToken);
    Task DeleteMemberFilterAsync(Guid memberViewId, CancellationToken cancellationToken);
    Task DeleteViewClientAsync(Guid memberId, Guid viewId, CancellationToken cancellationToken);
}
