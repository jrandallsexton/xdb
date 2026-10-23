using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Dapper;

using Xdb.Api.Application.Assets.Legacy;

namespace Xdb.Api.Infrastructure.Data.Procedures;

public sealed class AssetWriteProcedures(XdbDataContext db) : LegacyGatewayBase(db), IAssetWriteProcedures
{
    /// <summary>The original's <c>new SqlParameter(name, value)</c> list: a null value is not sent.</summary>
    private static DynamicParameters Supplied(params (string Name, object? Value)[] values)
    {
        var parameters = new DynamicParameters();
        foreach (var (name, value) in values)
            if (value is not null)
                parameters.Add(name, value);
        return parameters;
    }

    private static string? NullIfEmpty(string? text) => string.IsNullOrEmpty(text) ? null : text;

    private async Task ProcedureAsync(string name, DynamicParameters parameters, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Procedure(name, parameters, cancellationToken));

    private async Task InlineAsync(string sql, DynamicParameters parameters, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Inline(sql, parameters, cancellationToken));

    // ---- Assets ----

    public Task SaveAssetAsync(AssetContract a, CancellationToken cancellationToken) =>
        ProcedureAsync("spr_Asset_Save", Supplied(
            ("Id", a.Id), ("AssetTypeId", a.AssetTypeId), ("Name", a.Name), ("DisplayValue", NullIfEmpty(a.DisplayValue)), ("Description", NullIfEmpty(a.Description)),
            ("IsInstance", a.IsInstance ? true : null), ("InstanceOfId", a.InstanceOfId), ("Created", a.Created), ("Approved", a.Approved),
            ("LastModified", a.LastModified), ("CreatedBy", a.CreatedBy), ("ApprovedBy", a.ApprovedBy), ("LastModifiedBy", a.LastModifiedBy),
            ("DeletedBy", a.DeletedBy)), cancellationToken);

    public async Task<bool> IsLiveAssetAsync(Guid assetId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteScalarAsync<int>(Procedure("spr_AssetId_IsValid", new { Id = assetId }, cancellationToken)) == 1;

    public Task<bool> IsLiveAssetTypeAsync(Guid assetTypeId, CancellationToken cancellationToken) =>
        CountIsOneAsync(Inline("SELECT COUNT(*) FROM [AssetTypes] WITH (NoLock) WHERE [Id] = @Id AND [Deleted] IS NULL", new { Id = assetTypeId }, cancellationToken));

    public Task RenameAsync(Guid assetId, string? name, CancellationToken cancellationToken) =>
        InlineAsync("UPDATE [Assets] SET [Name] = @AssetName WHERE [Id] = @Id", Supplied(("AssetName", name), ("Id", assetId)), cancellationToken);

    public Task MarkAsUpdatedAsync(Guid assetId, Guid userId, CancellationToken cancellationToken) =>
        InlineAsync(Lines(
            "DELETE FROM [AssetClients] WHERE [AssetId] = @AssetId",
            "UPDATE [Assets] SET [LastModified] = GetDate(), [LastModifiedBy] = @UserId WHERE [Id] = @AssetId"),
            Supplied(("UserId", userId), ("AssetId", assetId)), cancellationToken);

    public Task ChangeInstancesTypeAsync(Guid newAssetTypeId, Guid instanceOfId, CancellationToken cancellationToken) =>
        ProcedureAsync("spr_Assets_ChangeAssetType", Supplied(("NewAssetTypeId", newAssetTypeId), ("InstanceOfId", instanceOfId)), cancellationToken);

    public Task DeleteAssetAsync(Guid assetId, Guid deletedBy, CancellationToken cancellationToken) =>
        ProcedureAsync("spr_Asset_Delete", Supplied(("Id", assetId), ("DeletedBy", deletedBy)), cancellationToken);

    public Task KillAssetAsync(Guid assetId, CancellationToken cancellationToken) =>
        ProcedureAsync("spr_Asset_Kill", Supplied(("AssetId", assetId)), cancellationToken);

    public Task DeleteAssetClientsAsync(Guid assetId, CancellationToken cancellationToken) =>
        InlineAsync("DELETE FROM [AssetClients] WHERE [AssetId] = @AssetId", Supplied(("AssetId", assetId)), cancellationToken);

    // ---- Relations ----

    public Task<bool> RelationExistsAsync(Guid fromAssetId, Guid toAssetId, byte relationTypeId, CancellationToken cancellationToken) =>
        CountIsOneAsync(Inline(Lines(
            "SELECT COUNT(*) FROM [AssetRelations] WITH (NoLock)",
            "WHERE [FromAssetId] = @From",
            "AND [ToAssetId] = @To",
            "AND [AssetRelationTypeId] = @TypeId",
            "AND [Deleted] IS NULL"), new { From = fromAssetId, To = toAssetId, TypeId = relationTypeId }, cancellationToken));

    public Task SaveRelationAsync(Guid id, Guid fromAssetId, Guid toAssetId, byte relationTypeId, DateTime created, DateTime? approved, Guid createdBy,
        Guid? approvedBy, CancellationToken cancellationToken) =>
        ProcedureAsync("spr_AssetAssetRelation_Save", Supplied(
            ("Id", id), ("FromAssetId", fromAssetId), ("ToAssetId", toAssetId), ("AssetRelationTypeId", relationTypeId), ("Created", created), ("Approved", approved),
            ("CreatedBy", createdBy), ("ApprovedBy", approvedBy)), cancellationToken);

    public Task RemoveParentRelationsAsync(Guid fromAssetId, byte relationTypeId, CancellationToken cancellationToken) =>
        InlineAsync(Lines(
            "UPDATE [AssetRelations]",
            "SET [Deleted] = GetDate()",
            "WHERE [FromAssetId] = @From",
            "AND [AssetRelationTypeId] = @TypeId",
            "AND [Deleted] IS NULL"), Supplied(("From", fromAssetId), ("TypeId", relationTypeId)), cancellationToken);

    // ---- Submittals and values ----

    public Task SaveSubmittalAsync(Guid id, Guid assetId, DateTime created, DateTime? reviewed, Guid createdBy, Guid? reviewedBy, CancellationToken cancellationToken) =>
        ProcedureAsync("spr_PropertyValueSubmittal_Save", Supplied(
            ("Id", id), ("AssetId", assetId), ("Created", created), ("Reviewed", reviewed), ("CreatedBy", createdBy), ("ReviewedBy", reviewedBy)), cancellationToken);

    public Task<bool> ValueExistsAsync(Guid propertyId, Guid assetId, string? value, bool caseSensitive, CancellationToken cancellationToken) =>
        CountIsOneAsync(Inline(Lines(
            "SELECT COUNT(*)",
            "FROM [PropertyValues]",
            "WHERE [PropertyId] = @PropertyId",
            "AND [AssetId] = @AssetId",
            caseSensitive ? "AND [Value]COLLATE Latin1_General_CS_AS = @Value" : "AND [Value] = @Value",
            "AND [Deleted] IS NULL",
            "AND [DeletedBy] IS NULL"),
            Supplied(("PropertyId", propertyId.ToString()), ("AssetId", assetId), ("Value", value)), cancellationToken));

    public Task DeleteValuesForAssetAsync(Guid propertyId, Guid deletedBy, Guid assetId, CancellationToken cancellationToken) =>
        ProcedureAsync("spr_PropertyValue_DeleteForAsset", Supplied(("PropertyId", propertyId), ("DeletedBy", deletedBy), ("AssetId", assetId)), cancellationToken);

    public Task SaveValueAsync(SubmittalValue v, CancellationToken cancellationToken) =>
        ProcedureAsync("spr_PropertyValue_Save", Supplied(
            ("Id", v.Id), ("PropertyId", v.PropertyId), ("AssetId", v.AssetId), ("SubmittalGroupId", v.SubmittalGroupId), ("Value", NullIfEmpty(v.Value)),
            ("Order", v.Order), ("Created", v.Created), ("Approved", v.Approved), ("Deleted", v.Deleted), ("CreatedBy", v.CreatedBy), ("ApprovedBy", v.ApprovedBy),
            ("DeletedBy", v.DeletedBy), ("Rejected", v.Rejected), ("RejectedBy", v.RejectedBy)), cancellationToken);

    public Task DeleteValueAsync(Guid valueId, Guid deletedBy, CancellationToken cancellationToken) =>
        ProcedureAsync("spr_PropertyValue_Delete", Supplied(("Id", valueId), ("DeletedBy", deletedBy)), cancellationToken);

    public Task SaveUrlAsync(Guid id, string? name, string url, DateTime created, Guid createdBy, CancellationToken cancellationToken) =>
        ProcedureAsync("spr_URL_Save", Supplied(("Id", id), ("Name", NullIfEmpty(name)), ("URL", url), ("Created", created), ("CreatedBy", createdBy)), cancellationToken);

    public Task SaveCurrencyValueAsync(Guid id, Guid symbolId, decimal amount, CancellationToken cancellationToken) =>
        InlineAsync(Lines(
            "INSERT INTO [CurrencyValues] ([Id], [CurrencySymbolId], [Value])",
            "VALUES (@Id, @SymbolId, @Value)"), Supplied(("Id", id), ("SymbolId", symbolId), ("Value", amount)), cancellationToken);

    public async Task<NarrativeFacts?> GetNarrativeFactsAsync(Guid submittalId, CancellationToken cancellationToken)
    {
        var sql = Lines(
            "SELECT",
            "P.[Id]",
            ",ISNULL(P.[DisplayValue], P.[Name]) AS [Property]",
            ",pv.[Value] AS [PVValue]",
            ",dbo.GetPropertyValueLookupByPropValId(PV.[Id], PV.[PropertyId]) AS [Value]",
            "FROM [PropertyValues] pv",
            "INNER JOIN [Properties] p on p.[Id] = pv.[PropertyId]",
            "where pv.[SubmittalGroupId] = @Id",
            "ORDER BY [Property]",
            "SELECT A.[Id], IsNull(A.[DisplayValue], A.[Name]) AS [Asset], A.[AssetTypeId], A.[Created], PVS.[Created] AS [Submitted], A.[IsInstance],",
            "isnull(dbo.GetAssetParentDescByInstanceId(A.[Id]), dbo.GetAssetParentNameByInstanceId(A.[Id])) AS [Product Code]",
            ",dbo.[GetMemberFriendlyValueById](PVS.[CreatedBy]) AS [SubmittedBy]",
            ",AT.[Name] AS [AssetType]",
            "FROM [PropertyValueSubmittals] PVS",
            "INNER JOIN [Assets] A ON A.[Id] = PVS.[AssetId]",
            "INNER JOIN [AssetTypes] AT ON AT.[Id] = A.[AssetTypeId]",
            "WHERE pvs.Id = @Id");
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(sql, new { Id = submittalId }, cancellationToken));
        if (!reader.HasRows)
            return null;
        var values = new List<(Guid, string?)>();
        var seen = new HashSet<Guid>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var propertyId = reader.GetGuid(0);
            if (seen.Add(propertyId))
            {
                _ = reader.GetString(1);
                values.Add((propertyId, reader.IsDBNull(2) ? string.Empty : reader.GetString(2)));
            }
        }
        await reader.NextResultAsync(cancellationToken);
        // The original read the asset row unconditionally: a submittal for a missing asset fails here.
        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException("Invalid attempt to read when no data is present.");
        return new NarrativeFacts(values,
            reader.GetString(1), reader.GetGuid(2), reader.GetDateTime(3), reader.GetDateTime(4), (bool)reader.GetValue(5),
            reader.IsDBNull(6) ? string.Empty : reader.GetString(6), reader.GetString(7), reader.GetString(8));
    }

    public Task SetNarrativeAsync(Guid submittalId, string narrative, CancellationToken cancellationToken) =>
        InlineAsync("UPDATE [PropertyValueSubmittals] SET [Narrative] = @Narr WHERE [Id] = @Id", Supplied(("Narr", narrative), ("Id", submittalId)), cancellationToken);

    public async Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetViewDefaultsAsync(Guid viewId, CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, string>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Procedure("spr_View_GetDefaultProperties", new { ViewId = viewId }, cancellationToken));
        if (!reader.HasRows)
            return [];
        while (await reader.ReadAsync(cancellationToken))
            result.Add(reader.GetGuid(0), reader.GetString(1));
        return [.. result];
    }

    // ---- Gen tables ----

    public async Task<IReadOnlyList<string>> GetGenTableNamesAsync(CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<string>(Inline(
            "SELECT [name] FROM sys.[objects] where (([type] = 'U') AND ([name] like 'gen%')) order by [name]", null, cancellationToken))).ToList();

    public async Task<IReadOnlyList<string>> GetGenTablesHoldingAsync(IReadOnlyList<string> tables, Guid assetId, CancellationToken cancellationToken)
    {
        if (tables.Count == 0)
            return [];
        var sql = new StringBuilder();
        for (var i = 0; i < tables.Count; i++)
        {
            Line(sql, $"SELECT COUNT(*) AS [Count], '{tables[i]}' AS [TableName] FROM [{tables[i]}] WHERE [AssetId] = @AssetId");
            if (i < tables.Count - 1)
                Line(sql, "UNION");
        }
        var holding = new List<string>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(sql.ToString(), new { AssetId = assetId }, cancellationToken));
        if (!reader.HasRows)
            return holding;
        while (await reader.ReadAsync(cancellationToken))
            if (reader.GetInt32(0) > 0)
                holding.Add(reader.GetString(1));
        return holding;
    }

    public Task ExecuteBatchAsync(string batch, CancellationToken cancellationToken) => InlineAsync(batch, new DynamicParameters(), cancellationToken);

    public Task ExecuteGenProcedureAsync(string procedure, Guid assetId, CancellationToken cancellationToken) =>
        ProcedureAsync(procedure, Supplied(("AssetId", assetId)), cancellationToken);

    // ---- Bookmarks ----

    public async Task<IReadOnlyList<BookmarkContract>> GetBookmarksAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var result = new List<BookmarkContract>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Procedure("spr_Bookmarks_Get", new { MemberId = memberId }, cancellationToken));
        if (!reader.HasRows)
            return result;
        while (await reader.ReadAsync(cancellationToken))
        {
            var bookmark = new BookmarkContract { Id = reader.GetGuid(0) };
            if (!reader.IsDBNull(1)) bookmark.AssetTypeId = reader.GetGuid(1);
            if (!reader.IsDBNull(2)) bookmark.AssetType = reader.GetString(2);
            if (!reader.IsDBNull(3)) bookmark.ViewId = reader.GetGuid(3);
            if (!reader.IsDBNull(4)) bookmark.AssetId = reader.GetGuid(4);
            if (!reader.IsDBNull(5)) bookmark.Asset = reader.GetString(5);
            if (!reader.IsDBNull(6)) bookmark.ReportId = reader.GetGuid(6);
            if (!reader.IsDBNull(7)) bookmark.ShowOnHome = (bool)reader.GetValue(7);
            if (!reader.IsDBNull(8)) bookmark.ReportName = reader.GetString(8);
            result.Add(bookmark);
        }
        return result;
    }

    public async Task<int> CountBookmarksAsync(Guid memberId, Guid viewId, Guid assetId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteScalarAsync<int>(Inline(
            $"SELECT COUNT(*) FROM [MemberBookmarks] WITH (NoLock) WHERE {NewLine}[MemberId] = '{memberId}' AND {NewLine}[ViewId] = '{viewId}' AND {NewLine}[AssetId] = '{assetId}'{NewLine}",
            null, cancellationToken));

    public async Task AddBookmarkAsync(Guid memberId, Guid viewId, Guid assetId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(new CommandDefinition(
            $"INSERT INTO [MemberBookmarks] ([Id], [MemberId], [ViewId], [AssetId]) VALUES ({NewLine}NEWID(), '{memberId}', '{viewId}', '{assetId}'){NewLine}",
            null, commandTimeout: 300, commandType: CommandType.Text, cancellationToken: cancellationToken));

    public Task RemoveBookmarkAsync(Guid userId, Guid assetId, Guid viewId, CancellationToken cancellationToken) =>
        ProcedureAsync("spr_Bookmark_Delete", Supplied(("UserId", userId), ("AssetId", assetId), ("ViewId", viewId)), cancellationToken);

    public Task ToggleBookmarkHomeAsync(Guid bookmarkId, CancellationToken cancellationToken) =>
        ProcedureAsync("spr_Bookmark_ToggleHome", Supplied(("Id", bookmarkId)), cancellationToken);

    public Task DeleteBookmarkAsync(Guid bookmarkId, CancellationToken cancellationToken) =>
        InlineAsync("DELETE FROM [MemberBookmarks] WHERE [Id] = @Id", Supplied(("Id", bookmarkId)), cancellationToken);

    // ---- Processes ----

    public async Task<Guid> GetAssetTypeIdByFiltersAsync(IReadOnlyList<string?> values, CancellationToken cancellationToken)
    {
        var sql = new StringBuilder();
        Line(sql, "SELECT DISTINCT AT.ID, AT.[Name]");
        Line(sql, "FROM [AssetTypes] AT WITH (NoLock)");
        Line(sql, "INNER JOIN [Assets] A WITH (NoLock) ON A.[AssetTypeId] = AT.[Id]");
        for (var i = 0; i < values.Count; i++)
            Line(sql, $"INNER JOIN [PropertyValues] PV{i} WITH (NoLock) ON PV{i}.[AssetId] = A.[Id]");
        Line(sql, "WHERE");
        var parameters = new DynamicParameters();
        for (var k = 0; k < values.Count; k++)
        {
            if (values[k] is not null)
                parameters.Add($"VALUE{k}", values[k]);
            if (k < values.Count - 1)
            {
                Line(sql, $"PV{k}.[Value] = @VALUE{k} AND");
                Line(sql, $"PV{k}.[Deleted] IS NULL");
                Line(sql, $"AND PV{k}.[Approved] IS NOT NULL");
                Line(sql, $"AND PV{k}.[Rejected] IS NULL AND");
            }
            else
            {
                Line(sql, $"PV{k}.[Value] = @VALUE{k}");
                Line(sql, $"AND PV{k}.[Deleted] IS NULL");
                Line(sql, $"AND PV{k}.[Approved] IS NOT NULL");
                Line(sql, $"AND PV{k}.[Rejected] IS NULL");
            }
        }
        Line(sql, "AND (A.[Deleted] IS NULL) AND (A.[Approved] IS NOT NULL)");

        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(sql.ToString(), parameters, cancellationToken));
        if (!reader.HasRows || !await reader.ReadAsync(cancellationToken) || reader.IsDBNull(0))
            return Guid.Empty;
        return reader.GetGuid(0);
    }
}
