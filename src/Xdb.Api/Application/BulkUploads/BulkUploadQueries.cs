using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.BulkUploads.Legacy;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.BulkUploads;

public interface IBulkUploadQueryHandler
{
    Task<Result<BulkUploadContract?>> GetUploadAsync(Guid uploadId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> GetUploadsAsync(string? userId, int filter, CancellationToken cancellationToken = default);
    Task<Result<List<BulkUploadLogContract>>> GetLogsAsync(Guid uploadId, bool descending, CancellationToken cancellationToken = default);
}

/// <summary>The bulk upload reads (spec M8-B section 1).</summary>
public sealed class BulkUploadQueryHandler(IBulkUploadProcedures uploads) : IBulkUploadQueryHandler
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");
    private const string EnUsGeneralPattern = "M/d/yyyy h:mm:ss tt";

    /// <summary>BulkUpload_Get: Id is the requested id; NULL columns keep the reader's defaults (RecordCount 0).</summary>
    public async Task<Result<BulkUploadContract?>> GetUploadAsync(Guid uploadId, CancellationToken cancellationToken = default)
    {
        var row = await uploads.GetUploadAsync(uploadId, cancellationToken);
        if (row is null)
            return new Success<BulkUploadContract?>(null);
        return new Success<BulkUploadContract?>(new BulkUploadContract
        {
            Id = uploadId, FileId = row.FileId ?? Guid.Empty, Created = row.Created ?? default, CreatedBy = row.CreatedBy ?? Guid.Empty, Completed = row.Completed,
            RecordCount = row.RecordCount ?? 0
        });
    }

    /// <summary>
    /// BulkUploads_Get: filter 1 keeps successful uploads, 0 all, anything else failed ones (a NULL count counts as -1).
    /// An unparseable userId lists every member's uploads, with the uploader named.
    /// </summary>
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> GetUploadsAsync(string? userId, int filter, CancellationToken cancellationToken = default)
    {
        Guid? memberId = Guid.TryParse(userId, out var parsed) ? parsed : null;
        var result = new List<KeyValuePair<Guid, string>>();
        var seen = new HashSet<Guid>();
        foreach (var row in await uploads.GetUploadsAsync(memberId, cancellationToken))
        {
            var count = row.RecordCount ?? -1;
            if ((filter == 1 && count <= 0) || (filter != 1 && filter != 0 && count >= 0))
                continue;
            var status = count > 0 ? "uploaded successfully" : "failed to upload";
            var created = row.Created.ToString(EnUsGeneralPattern, EnUs);
            var text = memberId is null
                ? $"{row.Document} {status} on {created} by {row.LastName}, {row.FirstName} ({row.Login}).  UploadId: {row.Id}"
                : $"{row.Document} {status} on {created}.  UploadId: {row.Id}";
            if (!seen.Add(row.Id))
                throw new ArgumentException($"An item with the same key has already been added: {row.Id:D}");
            result.Add(new KeyValuePair<Guid, string>(row.Id, text));
        }
        return new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(result);
    }

    /// <summary>BulkUploadLogs_Get: the whole log in Order (the start position was always discarded); apostrophes doubled on the way out.</summary>
    public async Task<Result<List<BulkUploadLogContract>>> GetLogsAsync(Guid uploadId, bool descending, CancellationToken cancellationToken = default)
    {
        var result = new List<BulkUploadLogContract>();
        foreach (var row in await uploads.GetLogsAsync(uploadId, descending, cancellationToken))
        {
            result.Add(new BulkUploadLogContract
            {
                Id = row.Id, Category = row.Category, AssetId = row.AssetId, AssetName = row.AssetName ?? string.Empty, AssetTypeId = row.AssetTypeId,
                IsInstance = row.IsInstance ?? false, Message = row.Message.Replace("'", "''", StringComparison.Ordinal), Order = row.Order
            });
        }
        return new Success<List<BulkUploadLogContract>>(result);
    }
}
