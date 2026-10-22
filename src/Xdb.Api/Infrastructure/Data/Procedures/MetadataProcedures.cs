using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Dapper;

using Microsoft.EntityFrameworkCore;

namespace Xdb.Api.Infrastructure.Data.Procedures;

/// <summary>
/// SQL text is the original's, line for line (it built statements with AppendLine, so lines end with CRLF).
/// </summary>
public sealed class MetadataProcedures(XdbDataContext db) : LegacyGatewayBase(db), IMetadataProcedures
{
    // ---- Asset types ----

    public Task<string> GetAssetTypeNameAsync(Guid assetTypeId, CancellationToken cancellationToken) =>
        ScalarStringAsync("SELECT [Name] FROM [AssetTypes] WITH (NoLock) WHERE [Id] = @Id", new { Id = assetTypeId }, cancellationToken);

    public Task<string> GetAssetTypePluralizationAsync(Guid assetTypeId, CancellationToken cancellationToken) =>
        ScalarStringAsync("SELECT ISNULL([Pluralization], [Name]) AS [Plural] FROM [AssetTypes] WITH (NoLock) WHERE [Id] = @Id", new { Id = assetTypeId }, cancellationToken);

    public async Task<Guid?> GetParentAssetTypeIdAsync(Guid childAssetTypeId, CancellationToken cancellationToken)
    {
        var parent = await ScalarGuidAsync(
            "select [ParentAssetTypeId] FROM AssetTypeRelations WITH (NoLock) WHERE [ChildAssetTypeId] = @ChildId AND [Deleted] IS NULL",
            new { ChildId = childAssetTypeId }, cancellationToken);
        return parent == Guid.Empty ? null : parent;
    }

    public async Task<bool> GetAllowAssetsAsync(Guid assetTypeId, CancellationToken cancellationToken)
    {
        // The original formatted the id into the text and cast the scalar to bool: no row or NULL fails.
        var value = await (await OpenAsync(cancellationToken)).ExecuteScalarAsync(
            Inline($"SELECT [AllowAssets] FROM [AssetTypes] WHERE [Id] = '{assetTypeId:D}'", null, cancellationToken));
        return value is bool allow ? allow : throw new InvalidOperationException($"Asset type {assetTypeId:D} has no AllowAssets value.");
    }

    public async Task<bool> HasAssetsAsync(Guid assetTypeId, bool isInstance, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteScalarAsync<int>(
            Procedure("spr_AssetType_HasAssets", new { AssetTypeId = assetTypeId, IsInstance = isInstance }, cancellationToken)) > 0;

    public async Task<IReadOnlyList<Guid>> GetDirectChildAssetTypeIdsAsync(Guid parentAssetTypeId, CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<Guid>(Inline(Lines(
            "SELECT ATR.[ChildAssetTypeId] FROM [AssetTypeRelations] ATR",
            "INNER JOIN [AssetTypes] AT ON AT.[Id] = ATR.[ChildAssetTypeId]",
            "WHERE [ParentAssetTypeId] = @ParentId",
            "AND AT.[Deleted] IS NULL",
            "AND ATR.[Deleted] IS NULL",
            "ORDER BY AT.[Name]"), new { ParentId = parentAssetTypeId }, cancellationToken))).ToList();

    public async Task<IReadOnlyList<Guid>> GetDescendantAssetTypeIdsAsync(Guid assetTypeId, CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<Guid>(
            Procedure("spr_AssetType_GetAllDescendants", new { Id = assetTypeId }, cancellationToken))).ToList();

    public Task<string> GetReportingLabelAsync(Guid assetTypeId, bool forInstances, CancellationToken cancellationToken) =>
        ScalarStringAsync(forInstances
                ? "SELECT [InstanceLabel] FROM [AssetTypes] WITH (NoLock) WHERE [Id] = @AssetTypeId AND [Deleted] IS NULL"
                : "SELECT [DefinitionLabel] FROM [AssetTypes] WITH (NoLock) WHERE [Id] = @AssetTypeId AND [Deleted] IS NULL",
            new { AssetTypeId = assetTypeId }, cancellationToken);

    public async Task<AssetTypeRow?> GetAssetTypeAsync(Guid assetTypeId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).QueryFirstOrDefaultAsync<AssetTypeRow>(
            Procedure("spr_AssetType_Get", new { Id = assetTypeId }, cancellationToken));

    public async Task<IReadOnlyList<AssetTypePropertyRelationRow>> GetAssetTypePropertyRelationsAsync(Guid assetTypeId, CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<AssetTypePropertyRelationRow>(
            Procedure("spr_AssetTypePropertyRelationList_GetByAssetTypeId", new { AssetTypeId = assetTypeId }, cancellationToken))).ToList();

    public async Task<IReadOnlyList<PropertyDetailRow>> GetAssetTypePropertiesAsync(Guid assetTypeId, CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<PropertyDetailRow>(
            Procedure("spr_PropertyList_GetByAssetTypeId", new { AssetTypeId = assetTypeId }, cancellationToken))).ToList();

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetAssetTypesDictionaryAsync(bool includeUnused, CancellationToken cancellationToken) =>
        GuidPairsAsync(Procedure("spr_AssetTypes_GetDictionary", new { IncludeUnused = includeUnused }, cancellationToken), skipNulls: false, cancellationToken);

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetRootAssetTypesAsync(CancellationToken cancellationToken) =>
        GuidPairsAsync(Inline(Lines(
            "SELECT AT.Id, AT.[Name]",
            "FROM AssetTypes AT WITH (NoLock)",
            "WHERE AT.ID NOT IN (SELECT DISTINCT ChildAssetTypeId FROM AssetTypeRelations WITH (NoLock))",
            "ORDER BY AT.[Name]"), null, cancellationToken), skipNulls: false, cancellationToken);

    public async Task<IReadOnlyList<KeyValuePair<Guid, Guid>>> GetDefaultViewsAsync(Guid? userId, string viewColumn, CancellationToken cancellationToken)
    {
        var sql = userId.HasValue
            ? Lines(
                $"SELECT DISTINCT AT.[Id], {viewColumn}",
                "FROM AssetTypes AT WITH (NoLock)",
                "INNER JOIN [RolesAssetTypes] RAT WITH (NoLock) ON RAT.[AssetTypeId] = AT.[Id]",
                "INNER JOIN [RolesMembers] RM WITH (NoLock) ON RM.[RoleId] = RAT.[RoleId]",
                "INNER JOIN [Roles] R WITH (NoLock) ON R.[Id] = RM.[RoleId]",
                "INNER JOIN [Members] M WITH (NoLock) ON M.[Id] = RM.[MemberId]",
                "WHERE",
                $"{viewColumn} IS NOT NULL",
                "AND AT.[Deleted] IS NULL",
                "AND RAT.[Deleted] IS NULL",
                "AND RM.[Deleted] IS NULL",
                "AND R.[Deleted] IS NULL",
                "AND R.[IsActive] = 1",
                "AND M.[Deleted] IS NULL",
                "AND M.[Id] = @UserId")
            : Lines(
                $"SELECT DISTINCT AT.[Id], {viewColumn}",
                "FROM AssetTypes AT WITH (NoLock)",
                $"WHERE {viewColumn} IS NOT NULL AND AT.[Deleted] IS NULL");
        // The original passed the user id as a string (nvarchar), converted implicitly by SQL Server.
        var parameters = userId.HasValue ? new { UserId = userId.Value.ToString("D") } : null;
        var rows = await (await OpenAsync(cancellationToken)).QueryAsync<(Guid AssetTypeId, Guid ViewId)>(Inline(sql, parameters, cancellationToken));
        return rows.Select(r => new KeyValuePair<Guid, Guid>(r.AssetTypeId, r.ViewId)).ToList();
    }

    public async Task<Guid?> GetAssetTypeIdOfAssetAsync(Guid assetId, CancellationToken cancellationToken)
    {
        var value = await (await OpenAsync(cancellationToken)).ExecuteScalarAsync(Procedure("spr_Asset_GetAssetTypeId", new { Id = assetId }, cancellationToken));
        return value is not null && Guid.TryParse(value.ToString(), out var id) ? id : null;
    }

    // ---- Processes and views ----

    public async Task<Guid?> GetProcessStepViewIdAsync(Guid processId, int stepIndex, CancellationToken cancellationToken)
    {
        var viewId = await ScalarGuidAsync("SELECT [ViewId] FROM [ProcessSteps] WITH (NoLock) WHERE [ProcessId] = @ProcessId AND [Order] = @Order",
            new { ProcessId = processId, Order = stepIndex }, cancellationToken);
        return viewId == Guid.Empty ? null : viewId;
    }

    public Task<Guid> GetViewAssetTypeIdAsync(Guid viewId, CancellationToken cancellationToken) =>
        ScalarGuidAsync("SELECT [AssetTypeId] FROM [Views] WITH (NoLock) WHERE [Id] = @ViewId", new { ViewId = viewId }, cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetViewPropertyIdsAsync(Guid viewId, CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<Guid>(Procedure("spr_View_GetPropertyIds", new { ViewId = viewId }, cancellationToken))).ToList();

    // ---- Members ----

    public async Task<bool> IsAdminAsync(Guid memberId, Guid adminRoleId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteScalarAsync<int>(
            Procedure("spr_Role_ContainsMember", new { RoleId = adminRoleId, MemberId = memberId }, cancellationToken)) == 1;

    public async Task<bool> IsLiveAssetIdAsync(Guid assetId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteScalarAsync<int>(Procedure("spr_AssetId_IsValid", new { Id = assetId }, cancellationToken)) == 1;

    public async Task<bool> IsLiveMemberIdAsync(Guid memberId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteScalarAsync<int>(Procedure("spr_MemberId_IsValid", new { Id = memberId }, cancellationToken)) == 1;

    /// <summary>SqlBaseDal.GetDictionary(proc): a repeated id or a NULL value fails.</summary>
    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetMembersDictionaryAsync(CancellationToken cancellationToken) =>
        GuidPairsAsync(Procedure("spr_Members_GetDictionary", null, cancellationToken), skipNulls: false, cancellationToken, unique: true);

    // ---- Properties ----

    public async Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetAssetTypePropertyNamesAsync(Guid assetTypeId, bool includeInherited,
        bool includeInheritedValue, bool includeDefinition, bool includeInstance, bool excludeUnused, bool hasChildAssetTypes,
        CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        Line(sb, "SELECT [P].[Id],");
        Line(sb, "ISNULL(p.[DisplayValue], p.[Name]) as [Property Name]");
        Line(sb, "FROM [Properties] P WITH (NoLock)");
        Line(sb, "INNER JOIN AssetTypesProperties ATP WITH (NoLock) on ATP.[PropertyId] = P.[id]");
        if (excludeUnused)
            Line(sb, "INNER JOIN [PropertyValues] PV WITH (NoLock) ON PV.[PropertyId] = P.[Id]");
        Line(sb, "WHERE ATP.[AssetTypeId] = @AssetTypeId");
        if (!includeInherited)
            Line(sb, "AND ATP.[IsInherited] = 0");
        if (!includeInheritedValue)
            Line(sb, "AND ATP.[IsInheritedValue] = 0");
        if (includeDefinition)
        {
            if (!includeInstance)
                Line(sb, "AND ATP.[IsInstance] = 0");
        }
        else
        {
            Line(sb, "AND ATP.[IsInstance] = 1");
        }
        Line(sb, "AND ATP.[Deleted] IS NULL");
        Line(sb, "AND (P.[DataTypeId] != 16)");
        Line(sb, "AND (P.[Deleted] IS NULL)");
        Line(sb, "UNION");
        Line(sb, "SELECT [P].[Id],");
        Line(sb, "ISNULL(p.[DisplayValue], p.[Name]) as [Property Name]");
        Line(sb, "FROM [Properties] P WITH (NoLock)");
        Line(sb, "WHERE (P.[IsSystem] = 1)");
        if (!includeInstance)
            Line(sb, "AND (P.[AssetTypeIsInstance] = 0)");
        Line(sb, "AND (P.[Deleted] IS NULL)");
        if (hasChildAssetTypes)
        {
            Line(sb, "UNION");
            Line(sb, "SELECT [P].[Id],");
            Line(sb, "ISNULL(p.[DisplayValue], p.[Name]) as [Property Name]");
            Line(sb, "FROM [Properties] P WITH (NoLock)");
            Line(sb, "WHERE (P.[DataTypeId] = 16)");
            Line(sb, "AND (P.[Deleted] IS NULL)");
        }
        Line(sb, "ORDER BY [Property Name]");

        // First row per id; rows with a NULL column skipped.
        var rows = await GuidPairsAsync(Inline(sb.ToString(), new { AssetTypeId = assetTypeId }, cancellationToken), skipNulls: true, cancellationToken);
        var seen = new HashSet<Guid>();
        return rows.Where(r => seen.Add(r.Key)).ToList();
    }

    public async Task<IReadOnlyList<PropertyDetailRow>> GetPropertiesAsync(IReadOnlyCollection<Guid> propertyIds, CancellationToken cancellationToken) =>
        // Guarded like PropertyDal.GetDictionary: an empty list makes dbo.Split yield '' and SQL raises 8169 after the
        // column metadata; the original's HasRows check swallowed it and returned nothing (measured: HistoricalValues_Get).
        await QueryGuardedAsync<PropertyDetailRow>(Procedure("spr_Properties_GetByListOfPropertyIds",
            new { PropertyIds = new DbString { Value = string.Join(",", propertyIds.Select(id => id.ToString("D"))), IsAnsi = true, Length = -1 } },
            cancellationToken));

    public async Task<PropertyDetailRow?> GetPropertyAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        var row = await (await OpenAsync(cancellationToken)).QueryFirstOrDefaultAsync<PropertyDetailRow>(
            Procedure("spr_Property_Get", new { Id = propertyId }, cancellationToken));
        // spr_Property_Get does not return the id; the original used the requested one.
        return row is null ? null : CopyWithId(row, propertyId);
    }

    public Task<int> GetPropertyDataTypeIdAsync(Guid propertyId, CancellationToken cancellationToken) =>
        ScalarIntAsync("SELECT [DataTypeId] FROM [Properties] WITH (NoLock) WHERE [Id] = @Id", new { Id = propertyId.ToString("D") }, cancellationToken);

    public Task<int> GetPropertySystemTypeIdAsync(Guid propertyId, CancellationToken cancellationToken) =>
        ScalarIntAsync("SELECT [SystemTypeId] FROM [Properties] WITH (NoLock) WHERE [Id] = @Id", new { Id = propertyId.ToString("D") }, cancellationToken);

    public Task<string> GetPropertyDisplayValueAsync(Guid propertyId, CancellationToken cancellationToken) =>
        ScalarStringAsync("select IsNull([DisplayValue], [Name]) FROM [Properties] WITH (NoLock) WHERE [Id] = @Id", new { Id = propertyId }, cancellationToken);

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetPropertiesByPickListAsync(Guid pickListId, CancellationToken cancellationToken) =>
        GuidPairsAsync(Inline(Lines(
            "SELECT [P].[Id], IsNull(P.[DisplayValue], P.[Name]) AS [Property]",
            "FROM [Properties] P WITH (NoLock)",
            "WHERE P.[PickListId] = @PickListId",
            "AND ([P].[Deleted] IS NULL)",
            "ORDER BY [Property]"), new { PickListId = pickListId }, cancellationToken), skipNulls: false, cancellationToken, skipNullKeysOnly: true);

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetPropertiesDictionaryAsync(bool includeDeleted, CancellationToken cancellationToken)
    {
        // "FROM ... WITH (NoLock)" is appended without a line break, as in the original.
        var sb = new StringBuilder();
        Line(sb, "SELECT [Id],");
        Line(sb, "ISNULL([DisplayValue], [Name]) as [Property Name]");
        sb.Append("FROM [Properties] WITH (NoLock)");
        if (!includeDeleted)
            Line(sb, "WHERE [Deleted] IS NULL");
        Line(sb, "ORDER BY [Property Name]");
        return GuidPairsAsync(Inline(sb.ToString(), null, cancellationToken), skipNulls: false, cancellationToken);
    }

    // ---- Picklists ----

    public async Task<PickListRow?> GetPickListAsync(Guid pickListId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).QueryFirstOrDefaultAsync<PickListRow>(Procedure("spr_PickList_Get", new { Id = pickListId }, cancellationToken));

    public async Task<IReadOnlyList<PickListValueDetailRow>> GetPickListValuesAsync(Guid pickListId, CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<PickListValueDetailRow>(
            Procedure("spr_PickListValueList_GetByPickListId", new { PickListId = pickListId }, cancellationToken))).ToList();

    public async Task<IReadOnlyList<PickListDependencyRow>> GetPickListDependenciesAsync(Guid childPickListId, CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<PickListDependencyRow>(Inline(Lines(
            "SELECT [Id],",
            "[ParentPickListId],",
            "[ParentPickListValueId],",
            "[ChildPickListId],",
            "[ChildPickListValueId],",
            "[Created],",
            "[CreatedBy]",
            "FROM [PickListDependencies] WITH (NoLock)",
            "WHERE [Deleted] IS NULL",
            "AND [ChildPickListId] = @ChildPickListId"), new { ChildPickListId = childPickListId }, cancellationToken))).ToList();

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetPickListValueDictionaryAsync(Guid pickListId, CancellationToken cancellationToken) =>
        GuidPairsAsync(Inline(Lines(
            "SELECT [Id], IsNull([DisplayValue], [Value]) AS [Value] FROM [PickListValues] WITH (NoLock)",
            "WHERE ([PickListId] = @PickListId) AND ([Deleted] IS NULL)",
            "ORDER BY [Order]"), new { PickListId = pickListId }, cancellationToken), skipNulls: true, cancellationToken);

    public Task<string> GetPickListNameAsync(Guid pickListId, CancellationToken cancellationToken) =>
        ScalarStringAsync("SELECT [Name] FROM [PickLists] WITH (NoLock) WHERE [Id] = @Id", new { Id = pickListId }, cancellationToken);

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetPickListsDictionaryAsync(bool includeDeleted, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        Line(sb, "SELECT [Id], IsNull([DisplayValue], [Name]) AS [Name] FROM [PickLists] WITH (NoLock)");
        if (!includeDeleted)
            Line(sb, "WHERE [Deleted] IS NULL");
        Line(sb, "ORDER BY [Name]");
        return GuidPairsAsync(Inline(sb.ToString(), null, cancellationToken), skipNulls: true, cancellationToken);
    }

    // ---- Lookups ----

    public Task<IReadOnlyList<KeyValuePair<int, string>>> GetOperatorsAsync(CancellationToken cancellationToken) =>
        IntPairsAsync("SELECT [Id], [Operator] FROM [lkOperators] ORDER BY [Id]", cancellationToken);

    public Task<IReadOnlyList<KeyValuePair<int, string>>> GetPermissionsAsync(CancellationToken cancellationToken) =>
        IntPairsAsync("SELECT [Id], [DisplayValue] FROM [lkPermissions] WITH (NoLock) ORDER BY [Id]", cancellationToken);

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetCurrencySymbolsAsync(CancellationToken cancellationToken) =>
        GuidPairsAsync(Inline("SELECT [Id], [Value] FROM [CurrencySymbols] WITH (NoLock) ORDER BY [Value]", null, cancellationToken), skipNulls: false, cancellationToken);

    public async Task<CurrencyValueRow?> GetCurrencyValueAsync(Guid currencyValueId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).QueryFirstOrDefaultAsync<CurrencyValueRow>(Inline(Lines(
            "select cv.[value], cv.[CurrencySymbolId], cs.value as [symbol], cast(cs.display as nvarchar) as [symbolDisplay]",
            "from currencyValues cv WITH (NoLock)",
            "inner join currencysymbols cs on cs.id = cv.currencySymbolId",
            "where cv.[Id] = @Id"), new { Id = currencyValueId }, cancellationToken));

    public async Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetSystemImageDictionaryAsync(CancellationToken cancellationToken)
    {
        // SqlBaseDal.GetDictionary(sql): first row per id wins.
        var rows = await GuidPairsAsync(Inline("SELECT [Id], [Name] from [Images] WITH (NoLock) WHERE [IsSystem] = 1 ORDER BY [Name]", null, cancellationToken),
            skipNulls: false, cancellationToken);
        var seen = new HashSet<Guid>();
        return rows.Where(r => seen.Add(r.Key)).ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetSystemImageIdsAsync(CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<Guid>(Inline("SELECT [Id] from [Images] WITH (NoLock) WHERE [IsSystem] = 1 ORDER BY [Name]", null, cancellationToken))).ToList();

    public async Task<ImageRow?> GetImageAsync(Guid imageId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).QueryFirstOrDefaultAsync<ImageRow>(Procedure("spr_Image_Get", new { Id = imageId }, cancellationToken));

    // ---- Asset dictionaries and generic lookups ----

    public async Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetAssetDictionaryAsync(Guid assetTypeId, bool useDisplayValues, CancellationToken cancellationToken)
    {
        var sql = Lines(
            useDisplayValues
                ? "SELECT [Id], IsNull([DisplayValue], [Name]) AS [Name], [Description] FROM [Assets] WITH (NoLock) WHERE [AssetTypeId] = @AssetTypeId"
                : "SELECT [Id], [Name], [Description] FROM [Assets] WITH (NoLock) WHERE [AssetTypeId] = @AssetTypeId",
            "AND [Deleted] IS NULL",
            "AND [Approved] IS NOT NULL",
            "AND [IsInstance] = 0",
            "ORDER BY [Name]");
        return Trimmed(await GuidPairsAsync(Inline(sql, new { AssetTypeId = assetTypeId }, cancellationToken), skipNulls: true, cancellationToken));
    }

    public async Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetAssetDictionaryAsync(IReadOnlyList<Guid> assetTypeIds, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        Line(sb, "SELECT [Id], IsNull([DisplayValue], [Name]) AS [Name], [Description] FROM [Assets] WITH (NoLock)");
        Line(sb, "WHERE [AssetTypeId] IN (");
        for (var i = 0; i < assetTypeIds.Count; i++)
            Line(sb, i == assetTypeIds.Count - 1 ? $"'{assetTypeIds[i]:D}'" : $"'{assetTypeIds[i]:D}',");
        Line(sb, ") AND ([Deleted] IS NULL) AND ([Approved] IS NOT NULL)");
        Line(sb, "AND [IsInstance] = 0");
        Line(sb, "ORDER BY [Name]");
        return Trimmed(await GuidPairsAsync(Inline(sb.ToString(), null, cancellationToken), skipNulls: true, cancellationToken));
    }

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetDatePartsAsync(CancellationToken cancellationToken) =>
        GuidPairsAsync(Inline("SELECT [Id], [DisplayValue] FROM [DateParts] ORDER BY [Order]", null, cancellationToken), skipNulls: false, cancellationToken);

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetLegacyDictionaryAsync(string sql, CancellationToken cancellationToken) =>
        LegacyDictionaryAsync(Inline(sql, null, cancellationToken), firstWins: true, cancellationToken);

    private static List<KeyValuePair<Guid, string>> Trimmed(IReadOnlyList<KeyValuePair<Guid, string>> rows) =>
        rows.Select(r => new KeyValuePair<Guid, string>(r.Key, r.Value.Trim())).ToList();

    // ---- Client cache ----

    public async Task<string> GetClientCacheAsync(string key, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).QueryFirstOrDefaultAsync<string>(
            Inline("SELECT [Value] FROM [ClientCache] WITH (NoLock) WHERE [Key] = @Key", new { Key = key }, cancellationToken)) ?? string.Empty;

    public async Task SetClientCacheAsync(string key, string value, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(new CommandDefinition(
            "DELETE FROM [ClientCache] WHERE [Key] = @Key INSERT INTO [ClientCache] ([Key], [Value]) VALUES (@Key, @Value)",
            new { Key = key, Value = value }, commandTimeout: ProcedureTimeout, cancellationToken: cancellationToken));

    private async Task<IReadOnlyList<KeyValuePair<int, string>>> IntPairsAsync(string sql, CancellationToken cancellationToken)
    {
        var result = new List<KeyValuePair<int, string>>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(sql, null, cancellationToken));
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new KeyValuePair<int, string>(Convert.ToInt32(reader.GetValue(0)), reader.GetString(1)));
        return result;
    }

    private static PropertyDetailRow CopyWithId(PropertyDetailRow row, Guid id) => new()
    {
        Id = id, DataTypeId = row.DataTypeId, Precision = row.Precision, PickListId = row.PickListId, RoleId = row.RoleId,
        IsOrdered = row.IsOrdered, AllowMultiValue = row.AllowMultiValue, Name = row.Name, DisplayValue = row.DisplayValue,
        Description = row.Description, AssetTypeId = row.AssetTypeId, AssetTypeIsInstance = row.AssetTypeIsInstance, Created = row.Created,
        Approved = row.Approved, LastModified = row.LastModified, CreatedBy = row.CreatedBy, ApprovedBy = row.ApprovedBy,
        LastModifiedBy = row.LastModifiedBy, Deleted = row.Deleted, DeletedBy = row.DeletedBy, IsSystem = row.IsSystem,
        SystemTypeId = row.SystemTypeId
    };

}
