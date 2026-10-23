using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Reports.Legacy;
using Xdb.Api.Application.Views.Legacy;

namespace Xdb.Api.Infrastructure.Data.Procedures;

/// <summary>A ReportCache view row: the report, its cache time (if cached) and hit count text.</summary>
public sealed record ReportCacheRow(Guid ReportId, string ReportName, DateTime? CachedAt, string? Hits);

/// <summary>The original's report reads and writes beyond the M2 viewer (ReportDal, BookmarkDal), with its SQL text and order.</summary>
public interface IReportStore
{
    /// <summary>
    /// ReportDal.Get: the report row, its properties (as the reader built them) and its filters (operator ids above 6
    /// fail). An unknown report fails, as the original dereferenced the missing object.
    /// </summary>
    Task<ReportContract> LoadAsync(Guid reportId, CancellationToken cancellationToken);
    /// <summary>The report's AssetTypeId, or the empty Guid.</summary>
    Task<Guid> GetAssetTypeIdAsync(Guid reportId, CancellationToken cancellationToken);
    /// <summary>The report's CreatedBy, or the empty Guid.</summary>
    Task<Guid> GetCreatedByAsync(Guid reportId, CancellationToken cancellationToken);
    /// <summary>spr_Reports_GetDictionary (all live reports) or spr_ReportList_GetByMemberId (the member's role reports).</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetReportsDictionaryAsync(Guid? memberId, CancellationToken cancellationToken);
    /// <summary>CurrencySymbolLayer.GetValuesDisplayValues: (Value, Display) ordered by Value (a repeated Value fails).</summary>
    Task<IReadOnlyList<KeyValuePair<string, string>>> GetCurrencyDisplaysAsync(CancellationToken cancellationToken);
    /// <summary>spr_ReportCache_View.</summary>
    Task<IReadOnlyList<ReportCacheRow>> GetCacheAsync(CancellationToken cancellationToken);

    Task ClearCacheAsync(Guid reportId, CancellationToken cancellationToken);
    Task<int> CountBookmarksAsync(Guid memberId, Guid reportId, CancellationToken cancellationToken);
    Task AddBookmarkAsync(Guid memberId, Guid reportId, bool showOnHome, CancellationToken cancellationToken);
    /// <summary>spr_Bookmark_Delete for a report (also marks the member updated).</summary>
    Task RemoveBookmarkAsync(Guid memberId, Guid reportId, CancellationToken cancellationToken);

    /// <summary>spr_Report_Save (insert, or an update of name, expression, procedure, deletion and filter string).</summary>
    Task SaveReportAsync(ReportContract report, CancellationToken cancellationToken);
    Task DeletePropertiesAsync(Guid reportId, CancellationToken cancellationToken);
    Task InsertPropertyAsync(Guid reportId, ReportPropertyContract property, bool isGrouping, CancellationToken cancellationToken);
    /// <summary>The custom-field sub-property insert into a table that no longer exists (fails with SQL 208, as in the original).</summary>
    Task InsertPropertyPropertyAsync(Guid reportPropertyId, Guid propertyId, int order, CancellationToken cancellationToken);
    Task DeleteFiltersAsync(Guid reportId, CancellationToken cancellationToken);
    /// <summary>One filter with a new id (the posted id is ignored); a null value fails.</summary>
    Task InsertFilterAsync(Guid reportId, FilterContract filter, CancellationToken cancellationToken);
    /// <summary>spr_Report_Kill.</summary>
    Task KillAsync(Guid reportId, CancellationToken cancellationToken);
    /// <summary>spr_Report_Delete (soft delete, now).</summary>
    Task DeleteAsync(Guid reportId, Guid deletedBy, CancellationToken cancellationToken);
}
