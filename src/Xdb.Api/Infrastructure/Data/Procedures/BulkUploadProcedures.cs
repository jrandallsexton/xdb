using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Dapper;

namespace Xdb.Api.Infrastructure.Data.Procedures;

public sealed class BulkUploadProcedures(XdbDataContext db) : LegacyGatewayBase(db), IBulkUploadProcedures
{
    public async Task<BulkUploadRow?> GetUploadAsync(Guid uploadId, CancellationToken cancellationToken)
    {
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(
            "SELECT [FileId], [Created], [CreatedBy], [Completed], [RecordCount] FROM [BulkUploads] WITH (NoLock) WHERE [Id] = @Id",
            new { Id = uploadId }, cancellationToken));
        if (!reader.HasRows || !await reader.ReadAsync(cancellationToken))
            return null;
        return new BulkUploadRow(reader.IsDBNull(0) ? null : reader.GetGuid(0), reader.IsDBNull(1) ? null : reader.GetDateTime(1),
            reader.IsDBNull(2) ? null : reader.GetGuid(2), reader.IsDBNull(3) ? null : reader.GetDateTime(3), reader.IsDBNull(4) ? null : reader.GetInt32(4));
    }

    public async Task<IReadOnlyList<BulkUploadListRow>> GetUploadsAsync(Guid? memberId, CancellationToken cancellationToken)
    {
        var rows = new List<BulkUploadListRow>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Procedure("spr_BulkUploads_Get",
            Supplied(("MemberId", memberId)), cancellationToken));
        if (!reader.HasRows)
            return rows;
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new BulkUploadListRow(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4),
                reader.IsDBNull(6) ? null : reader.GetInt32(6), reader.GetDateTime(7)));
        }
        return rows;
    }

    public async Task<IReadOnlyList<BulkUploadLogRow>> GetLogsAsync(Guid uploadId, bool descending, CancellationToken cancellationToken)
    {
        var rows = new List<BulkUploadLogRow>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Procedure(
            descending ? "spr_BulkUploadLogs_GetDesc" : "spr_BulkUploadLogs_GetAsc", new { BulkUploadId = uploadId }, cancellationToken));
        if (!reader.HasRows)
            return rows;
        while (await reader.ReadAsync(cancellationToken))
        {
            var hasAsset = !reader.IsDBNull(2);
            rows.Add(new BulkUploadLogRow(reader.GetInt32(0), reader.GetString(1), hasAsset ? reader.GetGuid(2) : null,
                hasAsset && !reader.IsDBNull(3) ? reader.GetString(3) : null, hasAsset && !reader.IsDBNull(4) ? reader.GetGuid(4) : null,
                hasAsset && !reader.IsDBNull(5) ? (bool)reader.GetValue(5) : null, reader.GetString(6), reader.GetInt32(7)));
        }
        return rows;
    }
}
