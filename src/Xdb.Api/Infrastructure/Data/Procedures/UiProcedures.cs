using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Dapper;

namespace Xdb.Api.Infrastructure.Data.Procedures;

public sealed class UiProcedures(XdbDataContext db) : LegacyGatewayBase(db), IUiProcedures
{
    public async Task<List<RelatedItemRow>> GetRelatedItemsAsync(Guid assetId, Guid? userId, bool isInstance, bool includeDataTypeRelations,
        CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        Line(sb, "select distinct");
        Line(sb, "\tat.[name] AS [AssetType_Singular],");
        Line(sb, "\tat.[pluralization] AS [AssetType_Plural],");
        Line(sb, "\t\tcase a.IsInstance");
        Line(sb, "\t\t\twhen 1 then at.[defaultInstanceViewId]");
        Line(sb, "\t\t\telse at.[defaultViewId]");
        Line(sb, "\t\tend");
        Line(sb, "\tAS [ViewId],");
        Line(sb, "\ta.Id as [AssetId],");
        Line(sb, "\tISNULL(a.[displayvalue], a.[name]) AS [Asset],");
        Line(sb, "\tISNULL(p.[singular], p.[displayvalue]) AS [Property],");
        Line(sb, "\ta.InstanceOfId AS [InstanceOfId],");
        Line(sb, "\tdbo.GetAssetParentNameByInstanceId(a.Id) AS [InstanceOfName],");
        Line(sb, "\tdbo.GetAssetParentDescByInstanceId(a.Id) AS [Instance Desc]");
        Line(sb, "from propertyValues pv WITH (NoLock)");
        Line(sb, "\tinner join properties p WITH (NoLock) on p.id = pv.propertyId");
        Line(sb, "\tinner join assets a WITH (NoLock) on a.id = pv.assetId");
        Line(sb, "\tinner join assetTypes at WITH (NoLock) on at.id = a.assetTypeId");
        if (userId.HasValue)
        {
            Line(sb, "\tinner join RolesAssetTypes RAT WITH (NoLock) ON RAT.AssetTypeId = at.Id");
            Line(sb, "\tinner join RolesMembers RM WITH (NoLock) ON RM.RoleId = RAT.RoleId");
        }
        Line(sb, "where [value] = cast(@AssetId as varchar(100))");
        Line(sb, "and pv.[deleted] is null");
        Line(sb, "and a.[deleted] is null");
        if (userId.HasValue)
            Line(sb, "and RM.MemberId = @MemberId ");
        if (!isInstance)
        {
            Line(sb, "UNION");
            Line(sb, "select distinct");
            Line(sb, "at.[name] AS [AssetType_Singular],");
            Line(sb, "at.[pluralization] AS [AssetType_Plural],");
            Line(sb, "case a.IsInstance");
            Line(sb, "when 1 then at.[defaultInstanceViewId]");
            Line(sb, "else at.[defaultViewId]");
            Line(sb, "end");
            Line(sb, "AS [ViewId],");
            Line(sb, "a.Id as [AssetId],");
            Line(sb, "ISNULL(a.[displayvalue], a.[name]) AS [Asset],");
            Line(sb, "at.DefinitionLabel  AS [Property],");
            Line(sb, "\ta.InstanceOfId AS [InstanceOfId],");
            Line(sb, "\tdbo.GetAssetParentNameByInstanceId(a.Id) AS [InstanceOfName],");
            Line(sb, "\tdbo.GetAssetParentDescByInstanceId(a.Id) AS [Instance Desc]");
            Line(sb, "from assets a WITH (NoLock)");
            Line(sb, "inner join assetTypes at WITH (NoLock) on at.id = a.assetTypeId");
            Line(sb, "where a.InstanceOfId = @AssetId");
            Line(sb, "and a.[Deleted] IS NULL");
        }
        if (includeDataTypeRelations)
        {
            Line(sb, "UNION");
            Line(sb, "select distinct");
            Line(sb, "at.[name] AS [AssetType_Singular],");
            Line(sb, "at.[pluralization] AS [AssetType_Plural],");
            Line(sb, "case a.IsInstance");
            Line(sb, "when 1 then at.[defaultInstanceViewId]");
            Line(sb, "else at.[defaultViewId]");
            Line(sb, "end");
            Line(sb, "AS [ViewId],");
            Line(sb, "a.Id as [AssetId],");
            Line(sb, "ISNULL(a.[displayvalue], a.[name]) AS [Asset],");
            Line(sb, "ISNULL(p.[displayvalue], p.[name])  AS [Property],");
            Line(sb, "a.InstanceOfId AS [InstanceOfId],");
            Line(sb, "dbo.GetAssetParentNameByInstanceId(a.Id) AS [InstanceOfName],");
            Line(sb, "dbo.GetAssetParentDescByInstanceId(a.Id) AS [Instance Desc]");
            Line(sb, "from propertyValues pv WITH (NoLock)");
            Line(sb, "inner join properties p WITH (NoLock) on p.id = pv.propertyId");
            Line(sb, "inner join assets a WITH (NoLock) on a.id = cast(pv.Value as varchar(36))");
            Line(sb, "inner join assetTypes at WITH (NoLock) on at.id = a.assetTypeId");
            Line(sb, "where pv.AssetId = @AssetId ");
            Line(sb, "and pv.[deleted] is null");
            Line(sb, "and a.[deleted] is null");
            Line(sb, "and p.DataTypeId = 13");
        }
        Line(sb, "UNION");
        Line(sb, "SELECT");
        Line(sb, "'Role' AS [AssetType_Singular]");
        Line(sb, ",'Roles' AS [AssetType_Plural]");
        Line(sb, ", NULL AS [viewId]");
        Line(sb, ", NULL AS [AssetId]");
        Line(sb, ", R.Name AS [Asset]");
        Line(sb, ", 'Member' AS [Property]");
        Line(sb, ", NULL AS [InstanceOfId]");
        Line(sb, ", NULL AS [InstanceOfName]");
        Line(sb, ", NULL AS [Instance Desc]");
        Line(sb, "FROM [Roles] R");
        Line(sb, "INNER JOIN [RoleMembers] RM ON RM.RoleId = R.Id");
        Line(sb, "WHERE RM.MemberId = @AssetId");
        Line(sb, "AND R.Deleted is null");
        Line(sb, "and RM.Deleted is null");
        Line(sb, "order by [property], [assetType_Singular], [asset]");

        var parameters = new DynamicParameters();
        parameters.Add("AssetId", assetId);
        if (userId.HasValue)
            parameters.Add("MemberId", userId.Value.ToString("D"));
        return await ReadItemsAsync(Inline(sb.ToString(), parameters, cancellationToken), withInstanceOf: true, cancellationToken);
    }

    public Task<List<RelatedItemRow>> GetRelatedItemsByValueAsync(string? propertyValue, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        Line(sb, "select distinct");
        Line(sb, "at.[name] AS [AssetType_Singular],");
        Line(sb, "at.[pluralization] AS [AssetType_Plural],");
        Line(sb, "case when a.[InstanceOfId] is null");
        Line(sb, "then at.[defaultViewId]");
        Line(sb, "else at.[DefaultInstanceViewId]");
        Line(sb, "end as [ViewId],");
        Line(sb, "a.Id as [AssetId],");
        Line(sb, "ISNULL(a.[displayvalue], a.[name]) AS [Asset],");
        Line(sb, "ISNULL(p.[displayvalue], p.[name]) AS [Property]");
        Line(sb, "from propertyValues pv WITH (NoLock)");
        Line(sb, "inner join properties p WITH (NoLock) on p.id = pv.propertyId");
        Line(sb, "inner join assets a WITH (NoLock) on a.id = pv.assetId");
        Line(sb, "inner join assetTypes at WITH (NoLock) on at.id = a.assetTypeId");
        Line(sb, $"where pv.[value] = '{propertyValue}'");
        Line(sb, "and pv.[deleted] is null");
        Line(sb, "and a.[deleted] is null");
        Line(sb, "order by [property], [assetType_Singular], [asset]");
        return ReadItemsAsync(Inline(sb.ToString(), null, cancellationToken), withInstanceOf: false, cancellationToken);
    }

    public Task<Guid> GetAssetTypeIdByNameAsync(string name, CancellationToken cancellationToken) =>
        ScalarGuidAsync("SELECT [Id] FROM [AssetTypes] WITH (NoLock) WHERE [Name] = @AssetTypeName", new { AssetTypeName = name }, cancellationToken);

    public async Task<IReadOnlyList<AssetHitRow>> GetAssetHitsAsync(Guid assetTypeId, int requestType, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        Line(sb, "SELECT");
        Line(sb, "A.[Id], A.[Name], COUNT(*) AS [HitCount]");
        Line(sb, "FROM [Assets] A WITH (NoLock)");
        Line(sb, "INNER JOIN [AssetHits] AH WITH (NoLock) ON AH.[AssetId] = A.[Id]");
        Line(sb, "WHERE");
        Line(sb, "A.[AssetTypeId] = @AssetTypeId");
        Line(sb, "AND A.[Deleted] IS NULL");
        if (requestType == 0)
            Line(sb, "AND A.[IsInstance] = 0");
        else if (requestType == 1)
            Line(sb, "AND A.[IsInstance] = 1");
        Line(sb, "GROUP BY A.[Name], A.[Id]");
        Line(sb, "ORDER BY [HitCount] DESC");
        var hits = new List<AssetHitRow>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(sb.ToString(), new { AssetTypeId = assetTypeId }, cancellationToken));
        while (await reader.ReadAsync(cancellationToken))
            hits.Add(new AssetHitRow(reader.GetGuid(0), reader.GetString(1), reader.GetInt32(2)));
        return hits;
    }

    public async Task<IReadOnlyList<RecentAssetRow>> GetRecentAssetsAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var rows = new List<RecentAssetRow>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Procedure("spr_AssetHits_GetForUser", new { MemberId = memberId }, cancellationToken));
        while (await reader.ReadAsync(cancellationToken))
        {
            _ = reader.GetInt32(0);
            rows.Add(new RecentAssetRow(reader.GetGuid(1), reader.GetString(2), reader.GetGuid(3), reader.IsDBNull(4) ? null : reader.GetGuid(4)));
        }
        return rows;
    }

    public async Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetGenTableDictionaryAsync(string tableName, string assetTypeName, string? searchTerm,
        CancellationToken cancellationToken)
    {
        var sql = !string.IsNullOrEmpty(searchTerm)
            ? $"SELECT [AssetId], [Name] AS [{assetTypeName}] FROM [{tableName}] WITH (NoLock) WHERE [Name] LIKE '%{searchTerm}%' ORDER BY [{assetTypeName}]"
            : $"SELECT [AssetId], [Name] AS [{assetTypeName}] FROM [{tableName}] WITH (NoLock) ORDER BY [{assetTypeName}]";
        var result = new List<KeyValuePair<Guid, string>>();
        var seen = new HashSet<Guid>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(sql, null, cancellationToken));
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetGuid(0);
            var name = reader.GetString(1);
            if (!seen.Add(id))
                throw new ArgumentException($"An item with the same key has already been added: {id:D}");
            result.Add(new KeyValuePair<Guid, string>(id, name));
        }
        return result;
    }

    public async Task<bool> IsValidViewIdAsync(Guid viewId, CancellationToken cancellationToken) =>
        await ScalarIntAsync("SELECT COUNT(*) FROM [Views] WITH (NoLock) WHERE [Id] = @Id AND [Deleted] IS NULL", new { Id = viewId }, cancellationToken) == 1;

    public Task<Guid> GetDefaultViewIdAsync(Guid assetTypeId, bool isInstance, CancellationToken cancellationToken) =>
        ScalarGuidAsync(isInstance
                ? "SELECT [DefaultInstanceViewId] FROM [AssetTypes] WITH (NoLock) WHERE [Id] = @Id"
                : "SELECT [DefaultViewId] FROM [AssetTypes] WITH (NoLock) WHERE [Id] = @Id",
            new { Id = assetTypeId }, cancellationToken);

    private async Task<List<RelatedItemRow>> ReadItemsAsync(CommandDefinition command, bool withInstanceOf, CancellationToken cancellationToken)
    {
        var items = new List<RelatedItemRow>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(command);
        while (await reader.ReadAsync(cancellationToken))
        {
            var item = new RelatedItemRow
            {
                AssetTypeSingular = reader.IsDBNull(0) ? null : reader.GetString(0),
                AssetTypePlural = reader.IsDBNull(1) ? null : reader.GetString(1),
                ViewId = reader.IsDBNull(2) ? Guid.Empty : reader.GetGuid(2),
                AssetId = reader.IsDBNull(3) ? Guid.Empty : reader.GetGuid(3),
                AssetName = reader.IsDBNull(4) ? null : reader.GetString(4),
                PropertyName = reader.IsDBNull(5) ? null : reader.GetString(5)
            };
            if (withInstanceOf)
            {
                item.InstanceOfId = reader.IsDBNull(6) ? Guid.Empty : reader.GetGuid(6);
                item.InstanceOfName = reader.IsDBNull(7) ? null : reader.GetString(7);
                item.InstanceOfDesc = reader.IsDBNull(8) ? null : reader.GetString(8);
            }
            items.Add(item);
        }
        return items;
    }
}
