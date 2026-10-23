using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Assets.Legacy;

namespace Xdb.Api.Infrastructure.Data.Procedures;

/// <summary>The submittal of a narrative: its values (property, raw value) and the asset facts the templates read.</summary>
public sealed record NarrativeFacts(
    IReadOnlyList<(Guid PropertyId, string? Value)> Values, string? AssetName, Guid AssetTypeId, DateTime AssetCreated, DateTime Submitted, bool IsInstance,
    string? InstanceOf, string? Submitter, string AssetType);

/// <summary>
/// The original's asset, submittal, value, relation and bookmark write statements (spec M6 sections 2-3), one call
/// each, with its parameter typing (a null value is not sent). No transactions.
/// </summary>
public interface IAssetWriteProcedures
{
    // ---- Assets ----
    /// <summary>spr_Asset_Save: @IsInstance is sent only for instances (definitions take the default 0).</summary>
    Task SaveAssetAsync(AssetContract asset, CancellationToken cancellationToken);
    Task<bool> IsLiveAssetAsync(Guid assetId, CancellationToken cancellationToken);
    Task<bool> IsLiveAssetTypeAsync(Guid assetTypeId, CancellationToken cancellationToken);
    Task RenameAsync(Guid assetId, string? name, CancellationToken cancellationToken);
    /// <summary>The two-line batch: the asset's AssetClients rows, then Assets.LastModified[By].</summary>
    Task MarkAsUpdatedAsync(Guid assetId, Guid userId, CancellationToken cancellationToken);
    Task ChangeInstancesTypeAsync(Guid newAssetTypeId, Guid instanceOfId, CancellationToken cancellationToken);
    Task DeleteAssetAsync(Guid assetId, Guid deletedBy, CancellationToken cancellationToken);
    Task KillAssetAsync(Guid assetId, CancellationToken cancellationToken);
    Task DeleteAssetClientsAsync(Guid assetId, CancellationToken cancellationToken);

    // ---- Relations ----
    Task<bool> RelationExistsAsync(Guid fromAssetId, Guid toAssetId, byte relationTypeId, CancellationToken cancellationToken);
    Task SaveRelationAsync(Guid id, Guid fromAssetId, Guid toAssetId, byte relationTypeId, DateTime created, DateTime? approved, Guid createdBy, Guid? approvedBy,
        CancellationToken cancellationToken);
    Task RemoveParentRelationsAsync(Guid fromAssetId, byte relationTypeId, CancellationToken cancellationToken);

    // ---- Submittals and values ----
    Task SaveSubmittalAsync(Guid id, Guid assetId, DateTime created, DateTime? reviewed, Guid createdBy, Guid? reviewedBy, CancellationToken cancellationToken);
    Task<bool> ValueExistsAsync(Guid propertyId, Guid assetId, string? value, bool caseSensitive, CancellationToken cancellationToken);
    Task DeleteValuesForAssetAsync(Guid propertyId, Guid deletedBy, Guid assetId, CancellationToken cancellationToken);
    Task SaveValueAsync(SubmittalValue value, CancellationToken cancellationToken);
    Task DeleteValueAsync(Guid valueId, Guid deletedBy, CancellationToken cancellationToken);
    Task SaveUrlAsync(Guid id, string? name, string url, DateTime created, Guid createdBy, CancellationToken cancellationToken);
    Task SaveCurrencyValueAsync(Guid id, Guid symbolId, decimal amount, CancellationToken cancellationToken);
    /// <summary>The narrative helper (one command, two result sets): null when the submittal has no values.</summary>
    Task<NarrativeFacts?> GetNarrativeFactsAsync(Guid submittalId, CancellationToken cancellationToken);
    Task SetNarrativeAsync(Guid submittalId, string narrative, CancellationToken cancellationToken);
    /// <summary>spr_View_GetDefaultProperties into a dictionary: a property twice fails as the original's Add did.</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetViewDefaultsAsync(Guid viewId, CancellationToken cancellationToken);

    // ---- Gen tables ----
    Task<IReadOnlyList<string>> GetGenTableNamesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetGenTablesHoldingAsync(IReadOnlyList<string> tables, Guid assetId, CancellationToken cancellationToken);
    Task ExecuteBatchAsync(string batch, CancellationToken cancellationToken);
    Task ExecuteGenProcedureAsync(string procedure, Guid assetId, CancellationToken cancellationToken);

    // ---- Bookmarks ----
    Task<IReadOnlyList<BookmarkContract>> GetBookmarksAsync(Guid memberId, CancellationToken cancellationToken);
    Task<int> CountBookmarksAsync(Guid memberId, Guid viewId, Guid assetId, CancellationToken cancellationToken);
    Task AddBookmarkAsync(Guid memberId, Guid viewId, Guid assetId, CancellationToken cancellationToken);
    Task RemoveBookmarkAsync(Guid userId, Guid assetId, Guid viewId, CancellationToken cancellationToken);
    Task ToggleBookmarkHomeAsync(Guid bookmarkId, CancellationToken cancellationToken);
    Task DeleteBookmarkAsync(Guid bookmarkId, CancellationToken cancellationToken);

    // ---- Processes ----
    Task<Guid> GetAssetTypeIdByFiltersAsync(IReadOnlyList<string?> values, CancellationToken cancellationToken);
}
