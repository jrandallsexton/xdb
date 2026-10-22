using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dapper;

using Microsoft.EntityFrameworkCore;

namespace Xdb.Api.Infrastructure.Data.Procedures;

public sealed class GeneratedObjectProcedures(XdbDataContext db) : IGeneratedObjectProcedures
{
    /// <summary>The original's timeout for metadata reads, in seconds.</summary>
    private const int CommandTimeout = 3000;

    public async Task<GenAssetTypeRow?> GetAssetTypeAsync(Guid assetTypeId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).QueryFirstOrDefaultAsync<GenAssetTypeRow>(Text(
            "SELECT [Id], [Name], [DefinitionLabel], [InstanceLabel], [AllowAssets] FROM [AssetTypes] WHERE [Id] = @Id",
            new { Id = assetTypeId }, cancellationToken));

    public async Task<IReadOnlyList<Guid>> GetAssetTypeIdsAsync(CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<(Guid Id, string Name)>(
            Procedure("spr_AssetTypes_GetDictionary", new { IncludeUnused = true }, cancellationToken))).Select(r => r.Id).ToList();

    public async Task<IReadOnlyList<Guid>> GetDescendantsAsync(Guid assetTypeId, CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<Guid>(
            Procedure("spr_AssetType_GetAllDescendants", new { Id = assetTypeId }, cancellationToken))).ToList();

    public async Task<string> GetReportingLabelAsync(Guid assetTypeId, bool forInstances, CancellationToken cancellationToken)
    {
        var sql = forInstances
            ? "SELECT [InstanceLabel] FROM [AssetTypes] WITH (NoLock) WHERE [Id] = @Id AND [Deleted] IS NULL"
            : "SELECT [DefinitionLabel] FROM [AssetTypes] WITH (NoLock) WHERE [Id] = @Id AND [Deleted] IS NULL";
        var value = await (await OpenAsync(cancellationToken)).ExecuteScalarAsync(Text(sql, new { Id = assetTypeId }, cancellationToken));
        return value is null or DBNull ? string.Empty : value.ToString()!.Trim();
    }

    /// <summary>For types with descendants the original added every live System_AssetType (data type 16) property.</summary>
    private const string SystemAssetTypePropertiesUnion = """
        UNION
        SELECT [P].[Id], ISNULL(p.[DisplayValue], p.[Name]) AS [PropertyName], CASE WHEN P.[AssetTypeIsInstance] IS NULL THEN 1 ELSE 0 END AS [TieBreak]
        FROM [Properties] P WITH (NoLock)
        WHERE (P.[DataTypeId] = 16)
        AND (P.[Deleted] IS NULL)
        """;

    public async Task<IReadOnlyList<GenPropertyNameRow>> GetGenPropertyNamesAsync(Guid assetTypeId, bool isInstance, bool hasChildAssetTypes, CancellationToken cancellationToken)
    {
        // The original's Properties_GetByAssetTypeId for a gen-table pass. Deviation (decided): among equal names the
        // original's order was undefined; system properties with AssetTypeIsInstance set now come first (then by id),
        // which is what most existing instance tables picked.
        var sql = $"""
            SELECT [P].[Id], ISNULL(p.[DisplayValue], p.[Name]) AS [PropertyName], CASE WHEN P.[AssetTypeIsInstance] IS NULL THEN 1 ELSE 0 END AS [TieBreak]
            FROM [Properties] P WITH (NoLock)
            INNER JOIN AssetTypesProperties ATP WITH (NoLock) ON ATP.[PropertyId] = P.[Id]
            WHERE ATP.[AssetTypeId] = @AssetTypeId
            AND ATP.[IsInstance] = {(isInstance ? 1 : 0)}
            AND ATP.[Deleted] IS NULL
            AND (P.[DataTypeId] != 16)
            AND (P.[Deleted] IS NULL)
            UNION
            SELECT [P].[Id], ISNULL(p.[DisplayValue], p.[Name]) AS [PropertyName], CASE WHEN P.[AssetTypeIsInstance] IS NULL THEN 1 ELSE 0 END AS [TieBreak]
            FROM [Properties] P WITH (NoLock)
            WHERE (P.[IsSystem] = 1)
            {(isInstance ? string.Empty : "AND (P.[AssetTypeIsInstance] = 0)")}
            AND (P.[Deleted] IS NULL)
            {(hasChildAssetTypes ? SystemAssetTypePropertiesUnion : string.Empty)}
            ORDER BY [PropertyName], [TieBreak], [Id]
            """;

        var rows = await (await OpenAsync(cancellationToken)).QueryAsync<(Guid Id, string? PropertyName, int TieBreak)>(
            Text(sql, new { AssetTypeId = assetTypeId }, cancellationToken));
        // The original skipped rows with a NULL name.
        return rows.Where(r => r.PropertyName is not null).Select(r => new GenPropertyNameRow(r.Id, r.PropertyName!)).ToList();
    }

    public async Task<IReadOnlyList<PropertyMetaRow>> GetPropertiesAsync(IReadOnlyCollection<Guid> propertyIds, CancellationToken cancellationToken)
    {
        if (propertyIds.Count == 0)
            return [];
        return (await (await OpenAsync(cancellationToken)).QueryAsync<PropertyMetaRow>(Procedure("spr_Properties_GetByListOfPropertyIds",
            new { PropertyIds = AnsiList(propertyIds) }, cancellationToken))).ToList();
    }

    public async Task<IReadOnlyList<PropertyRelationRow>> GetRelationsAsync(Guid assetTypeId, IReadOnlyCollection<Guid> propertyIds, CancellationToken cancellationToken)
    {
        if (propertyIds.Count == 0)
            return [];
        return (await (await OpenAsync(cancellationToken)).QueryAsync<PropertyRelationRow>(Procedure("spr_AssetTypePropertyRelationList_GetByAssetTypeIdAndPropertyIds",
            new { AssetTypeId = assetTypeId, PropertyIds = AnsiList(propertyIds) }, cancellationToken))).ToList();
    }

    public async Task<PropertyRelationRow?> GetRelationAsync(Guid assetTypeId, Guid propertyId, CancellationToken cancellationToken)
    {
        var connection = await OpenAsync(cancellationToken);
        var id = await connection.ExecuteScalarAsync(Text(
            "SELECT [Id] FROM [AssetTypesProperties] WHERE [AssetTypeId] = @AssetTypeId AND [PropertyId] = @PropertyId AND [Deleted] IS NULL",
            new { AssetTypeId = assetTypeId, PropertyId = propertyId }, cancellationToken));
        if (id is not Guid relationId)
            return null;
        return await connection.QueryFirstOrDefaultAsync<PropertyRelationRow>(Procedure("spr_AssetTypePropertyRelation_Get", new { Id = relationId }, cancellationToken));
    }

    public async Task<IReadOnlyList<Guid>> GetReportIdsAsync(CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<Guid>(Text(
            "SELECT [Id] FROM [Reports] WHERE [Deleted] IS NULL ORDER BY [ReportName], [Id]", null, cancellationToken))).ToList();

    public async Task<IReadOnlyList<Guid>> GetRoleIdsAsync(CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<Guid>(Text(
            "SELECT [Id] FROM [Roles] WHERE [Deleted] IS NULL AND [IsActive] = 1 ORDER BY [Name], [Id]", null, cancellationToken))).ToList();

    public async Task<IReadOnlyList<RoleAssetTypeRow>> GetRoleAssetTypesAsync(Guid roleId, CancellationToken cancellationToken)
    {
        var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "spr_Role_Get";
        command.CommandType = CommandType.StoredProcedure;
        command.CommandTimeout = CommandTimeout;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@Id";
        parameter.Value = roleId;
        command.Parameters.Add(parameter);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        // Role, members and reports come first; the fourth result set holds asset types, permissions and filters.
        for (var i = 0; i < 3; i++)
            await reader.NextResultAsync(cancellationToken);

        var assetTypes = new List<RoleAssetTypeRow>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetGuid(0);
            var assetType = assetTypes.Find(a => a.Id == id);
            if (assetType is null)
            {
                assetType = new RoleAssetTypeRow
                {
                    Id = id,
                    AssetTypeId = reader.GetGuid(1),
                    IsInstance = reader.GetBoolean(2),
                    IsInherited = reader.GetBoolean(3)
                };
                assetTypes.Add(assetType);
            }

            if (reader.IsDBNull(5))
                continue;
            var permissionId = reader.GetGuid(5);
            var permission = assetType.Permissions.Find(p => p.Id == permissionId);
            if (permission is null)
            {
                permission = new RolePermissionRow
                {
                    Id = permissionId,
                    PermissionId = reader.GetInt32(6),
                    FilterExpression = reader.IsDBNull(8) ? string.Empty : reader.GetString(8)
                };
                assetType.Permissions.Add(permission);
            }

            if (reader.IsDBNull(9))
                continue;
            permission.Filters.Add(new RoleFilterRow
            {
                PropertyId = reader.GetGuid(11),
                OperatorId = reader.GetInt32(13),
                Operator = reader.GetString(14),
                Value = reader.IsDBNull(15) ? string.Empty : reader.GetString(15)
            });
        }

        return assetTypes;
    }

    public async Task<IReadOnlyList<string>> GetTableColumnNamesAsync(string tableName, CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<string>(Text(
            "SELECT [name] FROM sys.columns WHERE [object_id] = OBJECT_ID(@Table) ORDER BY [column_id]",
            new { Table = "dbo.[" + tableName + "]" }, cancellationToken))).ToList();

    public async Task<bool> AnyGenTablesAsync(CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteScalarAsync<int>(Text(
            "SELECT COUNT(*) FROM sys.tables WHERE [name] LIKE 'gen%[_]D' OR [name] LIKE 'gen%[_]I'", null, cancellationToken)) > 0;

    public async Task ExecuteAsync(string sql, int commandTimeoutSeconds, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(new CommandDefinition(sql, commandType: CommandType.Text,
            commandTimeout: commandTimeoutSeconds, cancellationToken: cancellationToken));

    /// <summary>Ids joined as the original passed them: lowercase, comma-separated, varchar.</summary>
    private static DbString AnsiList(IEnumerable<Guid> ids) =>
        new() { Value = string.Join(",", ids.Select(id => id.ToString("D"))), IsAnsi = true, Length = -1 };

    private async Task<DbConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await db.Database.OpenConnectionAsync(cancellationToken);
        return connection;
    }

    private static CommandDefinition Procedure(string name, object parameters, CancellationToken cancellationToken) =>
        new(name, parameters, commandType: CommandType.StoredProcedure, commandTimeout: CommandTimeout, cancellationToken: cancellationToken);

    private static CommandDefinition Text(string sql, object? parameters, CancellationToken cancellationToken) =>
        new(sql, parameters, commandType: CommandType.Text, commandTimeout: CommandTimeout, cancellationToken: cancellationToken);
}
