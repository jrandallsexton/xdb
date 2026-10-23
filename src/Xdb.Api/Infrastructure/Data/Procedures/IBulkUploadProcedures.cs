using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xdb.Api.Infrastructure.Data.Procedures;

/// <summary>A BulkUploads row as BulkUpload_Get reads it (NULL columns stay at the reader's defaults).</summary>
public sealed record BulkUploadRow(Guid? FileId, DateTime? Created, Guid? CreatedBy, DateTime? Completed, int? RecordCount);

/// <summary>A spr_BulkUploads_Get row (Completed is read and discarded by the original).</summary>
public sealed record BulkUploadListRow(Guid Id, string Document, string LastName, string FirstName, string Login, int? RecordCount, DateTime Created);

/// <summary>A BulkUploadLogs row joined to its asset (AssetName is Assets.Name).</summary>
public sealed record BulkUploadLogRow(int Id, string Category, Guid? AssetId, string? AssetName, Guid? AssetTypeId, bool? IsInstance, string Message, int Order);

/// <summary>The original's bulk upload data access (spec M8-B), with its SQL text.</summary>
public interface IBulkUploadProcedures
{
    Task<BulkUploadRow?> GetUploadAsync(Guid uploadId, CancellationToken cancellationToken);
    /// <summary>spr_BulkUploads_Get: one member's uploads, or every member's when <paramref name="memberId"/> is null (not supplied).</summary>
    Task<IReadOnlyList<BulkUploadListRow>> GetUploadsAsync(Guid? memberId, CancellationToken cancellationToken);
    Task<IReadOnlyList<BulkUploadLogRow>> GetLogsAsync(Guid uploadId, bool descending, CancellationToken cancellationToken);
}
