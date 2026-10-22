using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Dapper;

namespace Xdb.Api.Infrastructure.Data.Procedures;

public sealed class AssetProcedures(XdbDataContext db) : LegacyGatewayBase(db), IAssetProcedures
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    public async Task<AssetRow?> GetAssetAsync(Guid assetId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).QueryFirstOrDefaultAsync<AssetRow>(Procedure("spr_Asset_Get", new { Id = assetId }, cancellationToken));

    public async Task<IReadOnlyList<PropertyValueRow>> GetAssetValuesAsync(Guid assetId, IReadOnlyList<Guid> propertyIds, bool includeDeleted, CancellationToken cancellationToken)
    {
        if (propertyIds.Count == 0)
            return [];
        var procedure = includeDeleted ? "spr_PropertyValueList_GetForAnAssetIncludeDeleted" : "spr_PropertyValueList_GetForAnAsset";
        return (await (await OpenAsync(cancellationToken)).QueryAsync<PropertyValueRow>(Procedure(procedure,
            new { AssetId = assetId, PropertyIds = AnsiList(propertyIds) }, cancellationToken))).ToList();
    }

    public Task<string> GetAssetDisplayValueTextAsync(Guid assetId, CancellationToken cancellationToken) =>
        ProcedureStringAsync("spr_Asset_Get_DisplayValue", assetId, cancellationToken);

    public Task<string> GetAssetNameTextAsync(Guid assetId, CancellationToken cancellationToken) =>
        ProcedureStringAsync("spr_Asset_Get_Name", assetId, cancellationToken);

    public Task<Guid?> GetInstanceOfIdAsync(Guid assetId, CancellationToken cancellationToken) =>
        ProcedureGuidAsync("spr_Asset_Get_InstanceOfId", assetId, cancellationToken);

    public async Task<bool> IsInstanceAsync(Guid assetId, CancellationToken cancellationToken)
    {
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Procedure("spr_Asset_IsInstance", new { Id = assetId }, cancellationToken));
        return await reader.ReadAsync(cancellationToken) && (bool)reader.GetValue(0);
    }

    public async Task<Guid?> GetParentAssetIdAsync(Guid assetId, CancellationToken cancellationToken)
    {
        var parent = await ScalarGuidAsync(Lines(
            "SELECT [FromAssetId] FROM [AssetRelations] WITH (NoLock)",
            $"WHERE [ToAssetId] = '{assetId:D}' AND [AssetRelationTypeId] = 0",
            "AND [Deleted] IS NULL"), null, cancellationToken);
        return parent == Guid.Empty ? null : parent;
    }

    public Task<Guid?> GetAssetCreatedByAsync(Guid assetId, CancellationToken cancellationToken) =>
        ProcedureGuidAsync("spr_Asset_Get_CreatedBy", assetId, cancellationToken);

    public Task<Guid> GetApprovedValueIdAsync(Guid propertyId, Guid assetId, CancellationToken cancellationToken) =>
        ScalarGuidAsync("SELECT [ID] FROM [PropertyValues] WITH (NoLock) WHERE [PropertyId] = @Id AND [AssetId] = @AssetId AND [Approved] IS NOT NULL AND [Deleted] IS NULL",
            new { Id = propertyId, AssetId = assetId }, cancellationToken);

    public async Task<PropertyValueRow?> GetPropertyValueAsync(Guid propertyValueId, CancellationToken cancellationToken)
    {
        var row = await (await OpenAsync(cancellationToken)).QueryFirstOrDefaultAsync<PropertyValueRow>(
            Procedure("spr_PropertyValue_Get", new { Id = propertyValueId }, cancellationToken));
        return row is null ? null : new PropertyValueRow
        {
            Id = propertyValueId, PropertyId = row.PropertyId, AssetId = row.AssetId, Value = row.Value?.Trim(), Order = row.Order,
            Created = row.Created, CreatedBy = row.CreatedBy
        };
    }

    public async Task<MemberRow?> GetMemberAsync(Guid memberId, CancellationToken cancellationToken)
    {
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Procedure("spr_Member_Get", new { Id = memberId }, cancellationToken));
        if (!await reader.ReadAsync(cancellationToken))
            return null;
        return new MemberRow(reader.GetString(0), reader.IsDBNull(1) ? string.Empty : reader.GetString(1), reader.IsDBNull(2) ? string.Empty : reader.GetString(2));
    }

    public async Task<DocumentRow?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Procedure("spr_Document_Get", new { Id = documentId }, cancellationToken));
        if (!await reader.ReadAsync(cancellationToken))
            return null;
        string? Text(string column) => reader.IsDBNull(reader.GetOrdinal(column)) ? null : reader.GetString(reader.GetOrdinal(column));
        return new DocumentRow(Text("Name"), Text("Title"));
    }

    public Task<string> GetPickListValueTextAsync(Guid pickListId, string valueId, CancellationToken cancellationToken) =>
        // @Id goes as a string (nvarchar), compared with a uniqueidentifier: a non-Guid value fails (SQL 8169).
        ScalarStringAsync(Lines(
            "SELECT IsNull([DisplayValue], [Value]) AS [DisplayValue]",
            "FROM [PickListValues] WITH (NoLock) WHERE",
            "[PickListId] = @PickListId",
            "AND",
            "[Id] = @Id"), new { PickListId = pickListId, Id = valueId }, cancellationToken);

    public async Task<UrlRow?> GetUrlAsync(Guid urlId, CancellationToken cancellationToken)
    {
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(Lines(
            "SELECT [Name], [Description], [URL], [Created], [CreatedBy], [Deleted], [DeletedBy]",
            "FROM [URLs] WITH (NoLock) WHERE [Id] = @Id"), new { Id = urlId }, cancellationToken));
        if (!await reader.ReadAsync(cancellationToken))
            return null;
        return new UrlRow(reader.IsDBNull(0) ? string.Empty : reader.GetString(0), reader.IsDBNull(2) ? string.Empty : reader.GetString(2));
    }

    public Task<string> GetAssetDescriptionAsync(Guid assetId, CancellationToken cancellationToken) =>
        ProcedureStringAsync("spr_Asset_GetDescription", assetId, cancellationToken);

    public Task<string> GetCurrencySymbolTextAsync(Guid currencySymbolId, CancellationToken cancellationToken) =>
        ScalarStringAsync("SELECT [Value] FROM [CurrencySymbols] WITH (NoLock) WHERE [Id] = @Id", new { Id = currencySymbolId }, cancellationToken);

    public async Task<DateTime?> GetAssetLastModifiedAsync(Guid assetId, CancellationToken cancellationToken)
    {
        var value = await (await OpenAsync(cancellationToken)).ExecuteScalarAsync(Inline("SELECT [LastModified] FROM [Assets] WITH (NoLock) WHERE [Id] = @Id",
            new { Id = assetId }, cancellationToken));
        if (value is null or DBNull)
            return null;
        // ExecuteScalarDateTime: ToString() and TryParse in en-US (lossless for smalldatetime).
        var text = Convert.ToDateTime(value, CultureInfo.InvariantCulture).ToString("M/d/yyyy h:mm:ss tt", EnUs);
        return DateTime.TryParse(text, EnUs, DateTimeStyles.None, out var result) ? result : null;
    }

    // ---- Cache, hits, row security ----

    public async Task<byte[]?> GetAssetClientCacheAsync(Guid assetId, Guid viewId, Guid userId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).QueryFirstOrDefaultAsync<byte[]>(Inline(
            "SELECT [Data] FROM [AssetClients] WHERE [AssetId] = @AssetId AND [ViewId] = @ViewId AND [UserId] = @UserId",
            new { AssetId = assetId, ViewId = viewId, UserId = userId }, cancellationToken));

    public async Task InsertAssetClientCacheAsync(Guid assetId, Guid viewId, Guid userId, byte[] data, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Inline(
            "INSERT INTO [AssetClients] ([AssetId], [ViewId], [UserId], [Data]) VALUES (@AssetId, @ViewId, @UserId, @Data)",
            new { AssetId = assetId, ViewId = viewId, UserId = userId, Data = data }, cancellationToken));

    public async Task InsertAssetHitAsync(Guid assetId, Guid memberId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Inline(
            "INSERT INTO [AssetHits] ([AssetId], [MemberId]) VALUES (@AssetId, @MemberId)", new { AssetId = assetId, MemberId = memberId }, cancellationToken));

    public async Task<IReadOnlyList<Guid>> GetRolesForUserAndViewAsync(Guid memberId, Guid viewId, CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<Guid>(Procedure("spr_Roles_GetByUserAndView",
            new { MemberId = memberId, ViewId = viewId }, cancellationToken))).ToList();

    public async Task<IReadOnlyList<Guid>> GetEditableRoleIdsAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var ids = new List<Guid>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Procedure("spr_Roles_GetEditable", new { MemberId = memberId }, cancellationToken));
        while (await reader.ReadAsync(cancellationToken))
        {
            // The original mapped Id, Name (GetString: a NULL name fails), AssetTypeId and DefIns.
            _ = reader.GetString(1);
            ids.Add(reader.GetGuid(0));
        }
        return ids;
    }

    public Task<int> CountInRoleViewAsync(string roleViewName, Guid assetId, CancellationToken cancellationToken) =>
        ScalarIntAsync($"SELECT COUNT(*) FROM [{roleViewName}] WHERE [AssetId] = '{assetId:D}'", null, cancellationToken);

    // ---- Lookups by name ----

    public async Task<Guid?> FindAssetIdAsync(string? assetName, Guid assetTypeId, CancellationToken cancellationToken)
    {
        // A null name was a parameter "not supplied" in the original (SQL 201).
        if (assetName is null)
            throw new InvalidOperationException("Procedure or function 'spr_Asset_GetId' expects parameter '@Name', which was not supplied.");
        var value = await (await OpenAsync(cancellationToken)).ExecuteScalarAsync(Procedure("spr_Asset_GetId",
            new { Name = new DbString { Value = assetName, IsAnsi = true, Length = -1 }, AssetTypeId = assetTypeId }, cancellationToken));
        return value is not null && Guid.TryParse(value.ToString(), out var id) ? id : null;
    }

    public async Task<IReadOnlyList<Guid>> GetAssetIdsByNameAsync(string? assetName, IReadOnlyList<Guid>? assetTypeIds, CancellationToken cancellationToken)
    {
        if (assetName is null)
            throw new InvalidOperationException("The parameterized query expects a name parameter, which was not supplied.");
        string sql;
        object parameters;
        if (assetTypeIds is null)
        {
            sql = "SELECT [Id] FROM [Assets] WITH (NoLock) WHERE [Name] = @Name";
            parameters = new { Name = assetName };
        }
        else
        {
            var sb = new StringBuilder();
            Line(sb, "SELECT [Id] FROM [Assets] WITH (NoLock)");
            Line(sb, "WHERE [Name] = @AssetName");
            Line(sb, "AND [AssetTypeId] IN (");
            for (var i = 0; i < assetTypeIds.Count; i++)
                Line(sb, i == assetTypeIds.Count - 1 ? $"'{assetTypeIds[i]:D}'" : $"'{assetTypeIds[i]:D}',");
            Line(sb, ") AND [Deleted] IS NULL");
            sql = sb.ToString();
            parameters = new { AssetName = assetName };
        }
        return (await (await OpenAsync(cancellationToken)).QueryAsync<Guid>(Inline(sql, parameters, cancellationToken))).ToList();
    }

    // ---- Type-level dictionaries and counts ----

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetDefinitionDictionaryAsync(Guid assetTypeId, bool includeDescriptions, CancellationToken cancellationToken) =>
        DefinitionsAsync(Inline(Lines(
            "SELECT [Id], IsNull([DisplayValue], [Name]) AS [Name], [Description] FROM [Assets] WITH (NoLock) WHERE [AssetTypeId] = @AssetTypeId",
            "AND [Deleted] IS NULL",
            "AND [Approved] IS NOT NULL",
            "AND [IsInstance] = 0",
            includeDescriptions ? "ORDER BY [Description], [Name]" : "ORDER BY [Name]"), new { AssetTypeId = assetTypeId }, cancellationToken), includeDescriptions, cancellationToken);

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetDefinitionDictionaryAsync(IReadOnlyList<Guid> assetTypeIds, bool includeDescriptions, CancellationToken cancellationToken)
    {
        if (assetTypeIds.Count == 0)
            return Task.FromResult<IReadOnlyList<KeyValuePair<Guid, string>>>([]);
        var sb = new StringBuilder();
        Line(sb, "SELECT [Id], IsNull([DisplayValue], [Name]) AS [Name], [Description] FROM [Assets] WITH (NoLock)");
        Line(sb, "WHERE [AssetTypeId] IN (");
        for (var i = 0; i < assetTypeIds.Count; i++)
            Line(sb, i == assetTypeIds.Count - 1 ? $"'{assetTypeIds[i]:D}'" : $"'{assetTypeIds[i]:D}',");
        Line(sb, ") AND ([Deleted] IS NULL) AND ([Approved] IS NOT NULL)");
        Line(sb, "AND [IsInstance] = 0");
        Line(sb, includeDescriptions ? "ORDER BY [Description], [Name]" : "ORDER BY [Name]");
        return DefinitionsAsync(Inline(sb.ToString(), null, cancellationToken), includeDescriptions, cancellationToken);
    }

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetInstanceDictionaryAsync(Guid assetTypeId, CancellationToken cancellationToken) =>
        GuidPairsAsync(Procedure("spr_AssetInstances_Get", new { AssetTypeId = assetTypeId }, cancellationToken), skipNulls: true, cancellationToken, unique: true);

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetInstanceDictionaryAsync(IReadOnlyList<Guid> assetTypeIds, CancellationToken cancellationToken) =>
        GuidPairsAsync(Procedure("spr_AssetInstances_GetByAssetTypeIds", new { AssetTypeIds = string.Join(",", assetTypeIds.Select(id => id.ToString("D"))) }, cancellationToken),
            skipNulls: true, cancellationToken, unique: true);

    public async Task<int> CountRowsAsync(string tableName, CancellationToken cancellationToken) =>
        (int)(await (await OpenAsync(cancellationToken)).ExecuteScalarAsync(Inline($"SELECT COUNT(*) FROM [{tableName}]", null, cancellationToken)))!;

    public async Task<IReadOnlyList<KeyValuePair<Guid, string>>> SearchByNameAsync(IReadOnlyList<Guid> assetTypeIds, int requestType, string? searchValue, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        Line(sb, "SELECT [Id], [Name],");
        Line(sb, requestType == 1 ? "[dbo].GetAssetParentDescByInstanceId ([Id]) AS [Description]" : "[Description]");
        Line(sb, "FROM [Assets] WITH (NoLock)");
        Line(sb, "WHERE [Deleted] IS NULL");
        Line(sb, "AND [Approved] IS NOT NULL");
        if (requestType == 0)
            Line(sb, "AND [IsInstance] = 0");
        else if (requestType == 1)
            Line(sb, "AND [IsInstance] = 1");
        Line(sb, "AND [Name] LIKE @SearchValue");
        Line(sb, "AND [AssetTypeId] IN (");
        for (var i = 0; i < assetTypeIds.Count; i++)
            Line(sb, i == assetTypeIds.Count - 1 ? $"'{assetTypeIds[i]:D}'" : $"'{assetTypeIds[i]:D}',");
        Line(sb, ")");
        Line(sb, "ORDER BY [Name]");

        var result = new List<KeyValuePair<Guid, string>>();
        var seen = new HashSet<Guid>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(sb.ToString(), new { SearchValue = "%" + searchValue + "%" }, cancellationToken));
        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.IsDBNull(0) || reader.IsDBNull(1))
                continue;
            var id = reader.GetGuid(0);
            var text = reader.GetString(1);
            if (!reader.IsDBNull(2) && reader.GetString(2) is { Length: > 0 } description)
                text = text + " (" + description + ")";
            if (!seen.Add(id))
                throw new ArgumentException($"An item with the same key has already been added: {id:D}");
            result.Add(new KeyValuePair<Guid, string>(id, text));
        }
        return result;
    }

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetAssetsReferencedByPropertyAsync(Guid propertyId, CancellationToken cancellationToken) =>
        GuidPairsAsync(Procedure("spr_Assets_GetFromPropertyValue", new { PropertyId = propertyId }, cancellationToken), skipNulls: false, cancellationToken, unique: true);

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetAssetsReferencedByPropertyOfTypeAsync(Guid propertyId, Guid assetTypeId, CancellationToken cancellationToken) =>
        GuidPairsAsync(Procedure("spr_Assets_GetInPropertyValue", new { PropertyId = propertyId, AssetTypeId = assetTypeId }, cancellationToken), skipNulls: true, cancellationToken, unique: true);

    public async Task<IReadOnlyList<Guid>> GetAssetIdsAsync(Guid assetTypeId, bool? isInstance, CancellationToken cancellationToken)
    {
        if (isInstance is null)
            throw new InvalidOperationException("Procedure or function 'spr_AssetIds_Get' expects parameter '@IsInstance', which was not supplied.");
        return (await (await OpenAsync(cancellationToken)).QueryAsync<Guid>(Procedure("spr_AssetIds_Get",
            new { AssetTypeId = assetTypeId, IsInstance = isInstance.Value }, cancellationToken))).ToList();
    }

    public async Task<IReadOnlyList<(string Group, Guid AssetId, string Name)>> GetPickListGroupsAsync(IReadOnlyList<Guid> assetIds, Guid propertyId, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        Line(sb, "SELECT A.[Id], IsNull(A.[DisplayValue], A.[Name]) AS [Name], IsNull(PLV.[Value], '[No Value]')");
        Line(sb, "FROM [Assets] A WITH (NoLock)");
        Line(sb, "LEFT JOIN [PropertyValues] PV WITH (NoLock) ON");
        Line(sb, "PV.[AssetId] = A.[Id] AND");
        Line(sb, "PV.[PropertyId] = @PropertyId AND");
        Line(sb, "PV.[Deleted] IS NULL AND");
        Line(sb, "PV.[Approved] IS NOT NULL AND");
        Line(sb, "PV.[Value] IS NOT NULL AND");
        Line(sb, "PV.[Value] <> ''");
        Line(sb, "LEFT JOIN [PickListValues] PLV WITH (NoLock) ON");
        Line(sb, "PLV.[Id] = PV.[Value]");
        Line(sb, "WHERE");
        Line(sb, "A.[Id] IN (");
        for (var i = 0; i < assetIds.Count; i++)
            Line(sb, i == assetIds.Count - 1 ? $"'{assetIds[i]:D}'" : $"'{assetIds[i]:D}',");
        Line(sb, ") ORDER BY PLV.[Value], A.[Name]");
        // Guarded as the original was: a plan-dependent 8169 (PLV.Id = PV.Value) after the metadata leaves no rows.
        var rows = new List<(string Group, Guid AssetId, string Name)>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(sb.ToString(), new { PropertyId = propertyId }, cancellationToken));
        if (!reader.HasRows)
            return rows;
        while (await reader.ReadAsync(cancellationToken))
            rows.Add((reader.GetString(2), reader.GetGuid(0), reader.GetString(1)));
        return rows;
    }

    /// <summary>The original's single/list definition dictionary mapping: trim, optional " (description)", unique ids.</summary>
    private async Task<IReadOnlyList<KeyValuePair<Guid, string>>> DefinitionsAsync(CommandDefinition command, bool includeDescriptions, CancellationToken cancellationToken)
    {
        var result = new List<KeyValuePair<Guid, string>>();
        var seen = new HashSet<Guid>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(command);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.IsDBNull(0) || reader.IsDBNull(1))
                continue;
            var id = reader.GetGuid(0);
            var text = reader.GetString(1).Trim();
            if (includeDescriptions && !reader.IsDBNull(2) && reader.GetString(2).Trim() is { Length: > 0 } description)
                text = text + " (" + description + ")";
            if (!seen.Add(id))
                throw new ArgumentException($"An item with the same key has already been added: {id:D}");
            result.Add(new KeyValuePair<Guid, string>(id, text));
        }
        return result;
    }

    /// <summary>ExecuteScalarString on a procedure: trimmed; "" for no row or NULL.</summary>
    private async Task<string> ProcedureStringAsync(string procedure, Guid id, CancellationToken cancellationToken)
    {
        var value = await (await OpenAsync(cancellationToken)).ExecuteScalarAsync(Procedure(procedure, new { Id = id }, cancellationToken));
        return value is null or DBNull ? string.Empty : value.ToString()!.Trim();
    }

    /// <summary>ExecuteScalarGuid: null for no row, NULL or a non-Guid.</summary>
    private async Task<Guid?> ProcedureGuidAsync(string procedure, Guid id, CancellationToken cancellationToken)
    {
        var value = await (await OpenAsync(cancellationToken)).ExecuteScalarAsync(Procedure(procedure, new { Id = id }, cancellationToken));
        return value is not null && Guid.TryParse(value.ToString(), out var result) ? result : null;
    }
}
