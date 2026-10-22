using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Dapper;

namespace Xdb.Api.Infrastructure.Data.Procedures;

public sealed class FilterProcedures(XdbDataContext db) : LegacyGatewayBase(db), IFilterProcedures
{
    public async Task<MemberFilterRow?> GetMemberFilterAsync(Guid memberViewId, CancellationToken cancellationToken)
    {
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Procedure("spr_MemberView_Get", new { Id = memberViewId }, cancellationToken));
        if (!await reader.ReadAsync(cancellationToken))
            return null;
        var assetTypeId = reader.GetGuid(2);
        var isInstance = (bool)reader.GetValue(3);
        _ = reader.GetString(4);
        var expression = reader.GetString(5);
        var filters = new List<MemberFilterItemRow>();
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                filters.Add(new MemberFilterItemRow(reader.GetGuid(0), reader.GetInt32(1), reader.IsDBNull(2) ? string.Empty : reader.GetString(2), reader.GetInt32(3)));
        }
        return new MemberFilterRow(assetTypeId, isInstance, expression, filters);
    }

    public async Task<IReadOnlyList<string>> GetPotentialValuesAsync(IReadOnlyList<Guid> assetIds, Guid propertyId, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        Line(sb, "SELECT DISTINCT PV.[Value]");
        Line(sb, "FROM [PropertyValues] PV WITH (NoLock)");
        Line(sb, "WHERE PV.[AssetId] IN (");
        InList(sb, assetIds);
        Line(sb, "AND (PV.[PropertyId] = @PropertyId)");
        Line(sb, "AND (PV.[Approved] IS NOT NULL)");
        Line(sb, "AND (PV.[Deleted] IS NULL)");
        Line(sb, "AND (PV.[Value] IS NOT NULL)");
        Line(sb, "AND (PV.[Value] != '')");
        Line(sb, "AND (PV.[Value] != ' ')");
        var values = new List<string>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(sb.ToString(), new { PropertyId = propertyId }, cancellationToken));
        while (await reader.ReadAsync(cancellationToken))
            values.Add(reader.GetString(0));
        return values;
    }

    public async Task<IReadOnlyList<string>> GetPotentialSystemValuesAsync(IReadOnlyList<Guid> assetIds, int systemTypeId, CancellationToken cancellationToken)
    {
        // SystemType: AssetType 1, CreatedBy 3, DeletedBy 5, InstanceOf 7, LastModifiedBy 10.
        var column = systemTypeId switch
        {
            1 => "A.[AssetTypeId]",
            3 => "A.[CreatedBy]",
            5 => "A.[DeletedBy]",
            7 => "A.[InstanceOfId]",
            10 => "A.[LastModifiedBy]",
            _ => null
        };
        var sb = new StringBuilder();
        if (column is not null)
            Line(sb, "SELECT DISTINCT " + column);
        Line(sb, "FROM [Assets] A WITH (NoLock)");
        Line(sb, "WHERE A.[Id] IN (");
        InList(sb, assetIds);
        if (systemTypeId is not (4 or 5))
            Line(sb, "AND A.[Deleted] IS NULL");
        var values = new List<string>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(sb.ToString(), null, cancellationToken));
        while (await reader.ReadAsync(cancellationToken))
        {
            if (!reader.IsDBNull(0))
                values.Add(reader.GetGuid(0).ToString("D"));
        }
        return values;
    }

    public async Task<IReadOnlyList<Guid>> GetInstanceOfIdsAsync(IReadOnlyList<Guid> assetIds, CancellationToken cancellationToken)
    {
        var ids = new List<Guid>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Procedure("spr_AssetIds_GetByInstanceOfIds",
            new { AssetIds = string.Join(",", assetIds.Select(id => id.ToString("D"))) }, cancellationToken));
        while (await reader.ReadAsync(cancellationToken))
            ids.Add(reader.GetGuid(0));
        return ids;
    }

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetAssetsAsync(IReadOnlyList<Guid> assetIds, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        Line(sb, "SELECT [Id], IsNull([DisplayValue], [Name]) AS [Name]");
        Line(sb, "FROM [Assets] WITH (NoLock)");
        sb.Append("WHERE [Id] IN (").Append(string.Join(",", assetIds.Select(id => $"'{id:D}'"))).Append(')');
        Line(sb, "AND ([Deleted] IS NULL)");
        Line(sb, "AND ([Approved] IS NOT NULL)");
        Line(sb, "ORDER BY [Name]");
        return AddPairsAsync(Inline(sb.ToString(), null, cancellationToken), cancellationToken);
    }

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetMatchingPickListValuesAsync(IReadOnlyList<Guid> valueIds, Guid pickListId, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        Line(sb, "SELECT PLV.[Id], IsNull(PLV.[DisplayValue], PLV.[Value]) AS [Value]");
        Line(sb, "FROM [PickListValues] PLV WITH (NoLock)");
        Line(sb, "WHERE PLV.[Id] IN (");
        InList(sb, valueIds);
        Line(sb, "AND (PLV.[PickListId] = @PickListId)");
        Line(sb, "AND (PLV.[Deleted] IS NULL)");
        Line(sb, "ORDER BY [Value]");
        return AddPairsAsync(Inline(sb.ToString(), new { PickListId = pickListId }, cancellationToken), cancellationToken);
    }

    // ---- Assets_GetMatching ----

    public async Task<IReadOnlyList<KeyValuePair<Guid, string>>> MatchByAssetTypeAsync(IReadOnlyList<Guid> assetIds, Guid assetTypeId, bool includeDescriptions,
        CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        Line(sb, "SELECT [Id], [Name], [Description]");
        Line(sb, "FROM [Assets] WITH (NoLock)");
        Line(sb, "WHERE [Id] IN (");
        InList(sb, assetIds);
        Line(sb, "AND [AssetTypeId] = @AssetTypeId");
        Line(sb, "AND [Deleted] IS NULL");
        Line(sb, "AND [Approved] IS NOT NULL");
        Line(sb, "ORDER BY [Name]");
        var result = new List<KeyValuePair<Guid, string>>();
        var seen = new HashSet<Guid>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(sb.ToString(), new { AssetTypeId = assetTypeId }, cancellationToken));
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetGuid(0);
            var name = reader.GetString(1);
            var text = includeDescriptions && !reader.IsDBNull(2) && reader.GetString(2) is { Length: > 0 } description ? $"{name} ({description})" : name;
            Add(result, seen, id, text);
        }
        return result;
    }

    public async Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetInstancesOfAsync(Guid instanceOfId, CancellationToken cancellationToken)
    {
        var result = new List<KeyValuePair<Guid, string>>();
        var seen = new HashSet<Guid>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Procedure("spr_Assets_GetByInstanceOfId",
            new { InstanceOfId = instanceOfId }, cancellationToken));
        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.IsDBNull(0) || reader.IsDBNull(1))
                continue;
            Add(result, seen, reader.GetGuid(0), reader.GetString(1));
        }
        return result;
    }

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> MatchBySystemUserAsync(IReadOnlyList<Guid> assetIds, int systemTypeId, string? userId,
        CancellationToken cancellationToken)
    {
        // A null value is a parameter "not supplied" (the original's new SqlParameter(name, null)).
        if (userId is null)
            throw new InvalidOperationException("The parameterized query expects the parameter '@UserId', which was not supplied.");
        var column = systemTypeId switch { 3 => "A.[CreatedBy]", 5 => "A.[DeletedBy]", _ => "A.[LastModifiedBy]" };
        var sb = new StringBuilder();
        Line(sb, "SELECT DISTINCT A.[Id], ISNULL(A.[DisplayValue], A.[Name]) AS [Name]");
        Line(sb, "FROM [Assets] A WITH (NoLock)");
        Line(sb, "WHERE A.[Id] IN (");
        InList(sb, assetIds);
        Line(sb, $"AND {column} = @UserId");
        if (systemTypeId != 5)
            Line(sb, "AND A.[Deleted] IS NULL AND A.[DeletedBy] IS NULL");
        Line(sb, "ORDER BY [Name]");
        return AddPairsAsync(Inline(sb.ToString(), new { UserId = userId }, cancellationToken), cancellationToken);
    }

    public async Task<IReadOnlyList<KeyValuePair<Guid, string>>> MatchByValueAsync(IReadOnlyList<Guid> assetIds, Guid propertyId, string? value,
        bool includeDescriptions, bool useInstanceOfValues, bool exactMatch, CancellationToken cancellationToken)
    {
        if (exactMatch && value is null)
            throw new InvalidOperationException("The parameterized query expects the parameter '@Value', which was not supplied.");
        var sb = new StringBuilder();
        Line(sb, "SELECT A.[Id], IsNull(A.[DisplayValue], A.[Name]) AS [Name], A.[Description]");
        Line(sb, "FROM [Assets] A WITH (NoLock)");
        Line(sb, useInstanceOfValues
            ? "INNER JOIN [PropertyValues] PV WITH (NoLock) ON PV.[AssetId] = A.[InstanceOfId]"
            : "INNER JOIN [PropertyValues] PV WITH (NoLock) ON PV.[AssetId] = A.[Id]");
        Line(sb, "WHERE A.[Id] IN (");
        InList(sb, assetIds);
        Line(sb, "AND PV.[PropertyId] = @PropertyId");
        Line(sb, exactMatch ? "AND PV.[Value] = @Value" : "AND PV.[Value] like @PropertyValue");
        Line(sb, "AND PV.[Deleted] IS NULL");
        Line(sb, "AND PV.[Approved] IS NOT NULL");
        Line(sb, "AND PV.[Rejected] IS NULL");
        Line(sb, "AND A.[Deleted] IS NULL");
        Line(sb, "AND A.[Approved] IS NOT NULL");
        Line(sb, "ORDER BY [Name]");
        var parameters = new DynamicParameters();
        parameters.Add("PropertyId", propertyId);
        if (exactMatch)
            parameters.Add("Value", value);
        parameters.Add("PropertyValue", "%" + value + "%");

        var result = new List<KeyValuePair<Guid, string>>();
        var seen = new HashSet<Guid>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(sb.ToString(), parameters, cancellationToken));
        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.IsDBNull(0) || reader.IsDBNull(1))
                continue;
            var id = reader.GetGuid(0);
            var name = reader.GetString(1);
            if (includeDescriptions && !reader.IsDBNull(2))
            {
                // An empty description drops the row, as in the original.
                if (reader.GetString(2) is { Length: > 0 } description)
                    Add(result, seen, id, $"{name} ({description})");
            }
            else
            {
                Add(result, seen, id, name);
            }
        }
        return result;
    }

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetByPropertyValuesAsync(Guid assetTypeId, IReadOnlyList<KeyValuePair<Guid, string>> propertyValues,
        CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        Line(sb, "SELECT DISTINCT PV.[AssetId], A.[Name]");
        Line(sb, "FROM [PropertyValues] PV WITH (NoLock)");
        Line(sb, "INNER JOIN [Assets] A WITH (NoLock) ON A.[Id] = [PV].[AssetId]");
        Line(sb, "WHERE (");
        for (var i = 0; i < propertyValues.Count; i++)
        {
            Line(sb, $"(PV.[PropertyId] = '{propertyValues[i].Key:D}' AND PV.[Value] = '{propertyValues[i].Value}')");
            if (i != propertyValues.Count - 1)
                Line(sb, "OR");
        }
        Line(sb, ") AND PV.[Deleted] IS NULL");
        Line(sb, "AND PV.[Approved] IS NOT NULL");
        Line(sb, "AND PV.[Rejected] IS NULL");
        Line(sb, "AND A.[Deleted] IS NULL");
        Line(sb, "AND A.[Approved] IS NOT NULL");
        Line(sb, "AND A.[AssetTypeId] = @AssetTypeId");
        Line(sb, "ORDER BY [Name]");
        return AddPairsAsync(Inline(sb.ToString(), new { AssetTypeId = assetTypeId }, cancellationToken), cancellationToken);
    }

    // ---- AssetFilterClient_Get ----

    public async Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetDependentPickListValuesAsync(Guid childPickListId, Guid parentPickListId, Guid parentValueId,
        CancellationToken cancellationToken)
    {
        var sql = Lines(
            "SELECT PLV.[Id], PLV.[Value], PLV.[DisplayValue] FROM [PickListValues] PLV WITH (NoLock)",
            "INNER JOIN [PickListDependencies] PLD WITH (NoLock) ON PLD.[ChildPickListValueId] = PLV.[Id]",
            "WHERE PLD.[ParentPickListId] = @ParentPickListId",
            "AND PLD.[ParentPickListValueId] = @ParentPickListValueId",
            "AND PLD.[ChildPickListId] = @ChildPickListId",
            "AND PLV.[Deleted] IS NULL",
            "ORDER BY [PLV].[Order]");
        var result = new List<KeyValuePair<Guid, string>>();
        var seen = new HashSet<Guid>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(sql,
            new { ParentPickListId = parentPickListId, ParentPickListValueId = parentValueId, ChildPickListId = childPickListId }, cancellationToken));
        while (await reader.ReadAsync(cancellationToken))
        {
            var value = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            var display = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            Add(result, seen, reader.GetGuid(0), display.Length > 0 ? display : value);
        }
        return result;
    }

    public async Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetNonFreeEntryPropertiesAsync(IReadOnlyList<Guid> assetTypeIds, int requestType,
        CancellationToken cancellationToken)
    {
        if (assetTypeIds.Count == 0)
            return [];
        var sb = new StringBuilder();
        Line(sb, "SELECT DISTINCT P.[Id],");
        Line(sb, "ISNULL(p.[DisplayValue], p.[Name]) as [Property Name]");
        Line(sb, "FROM [Properties] P WITH (NoLock)");
        Line(sb, "INNER JOIN [AssetTypesProperties] ATP WITH (NoLock) ON ATP.[PropertyId] = P.[Id]");
        Line(sb, "WHERE ATP.[AssetTypeId] IN (");
        for (var i = 0; i < assetTypeIds.Count; i++)
            Line(sb, i == assetTypeIds.Count - 1 ? $"'{assetTypeIds[i]:D}'" : $"'{assetTypeIds[i]:D}',");
        Line(sb, ")");
        Line(sb, "AND (P.[DataTypeId] NOT IN (2, 3, 4, 5, 7, 10, 12, 21, 22, 24, 25))");
        Line(sb, "AND (P.[Deleted] IS NULL)");
        Line(sb, "AND ([ATP].[Deleted] IS NULL)");
        if (requestType == 0)
            Line(sb, "AND ([ATP].[IsInstance] = 0)");
        Line(sb, "ORDER BY [Property Name]");
        return await AddPairsAsync(Inline(sb.ToString(), null, cancellationToken), cancellationToken);
    }

    public async Task<IReadOnlyList<IReadOnlyList<FilterItemRow>>> GetFilterItemsAsync(string sql, CancellationToken cancellationToken)
    {
        var sets = new List<IReadOnlyList<FilterItemRow>>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(sql, null, cancellationToken));
        if (!reader.HasRows)
            return sets;
        do
        {
            var rows = new List<FilterItemRow>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var raw = reader.IsDBNull(0) ? "NULL" : LegacyText(reader.GetValue(0));
                var display = reader.IsDBNull(1) ? "NULL" : LegacyText(reader.GetValue(1));
                rows.Add(new FilterItemRow(display, raw, reader.GetInt32(2) == 1));
            }
            sets.Add(rows);
        }
        while (await reader.NextResultAsync(cancellationToken));
        return sets;
    }

    private static readonly System.Globalization.CultureInfo EnUs = System.Globalization.CultureInfo.GetCultureInfo("en-US");

    /// <summary>object.ToString() as .NET Framework wrote it under en-US (dates without ICU's narrow space, 15-digit doubles).</summary>
    private static string LegacyText(object value) => value switch
    {
        string text => text,
        DateTime date => date.ToString(ViewProcedures.EnUsGeneralPattern, EnUs),
        double number => number.ToString("G15", EnUs),
        float number => number.ToString("G7", EnUs),
        Guid id => id.ToString("D"),
        _ => Convert.ToString(value, EnUs) ?? string.Empty
    };

    /// <summary>The ids one per line, the last closing the list: "'a',\r\n'b')". An empty list leaves "IN (" open (a syntax error).</summary>
    private static void InList(StringBuilder sb, IReadOnlyList<Guid> ids)
    {
        for (var i = 0; i < ids.Count; i++)
            Line(sb, i == ids.Count - 1 ? $"'{ids[i]:D}')" : $"'{ids[i]:D}',");
    }

    /// <summary>Rows mapped with Dictionary.Add(GetGuid(0), GetString(1)): NULLs and duplicate keys fail.</summary>
    private async Task<IReadOnlyList<KeyValuePair<Guid, string>>> AddPairsAsync(CommandDefinition command, CancellationToken cancellationToken)
    {
        var result = new List<KeyValuePair<Guid, string>>();
        var seen = new HashSet<Guid>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(command);
        while (await reader.ReadAsync(cancellationToken))
            Add(result, seen, reader.GetGuid(0), reader.GetString(1));
        return result;
    }

    private static void Add(List<KeyValuePair<Guid, string>> result, HashSet<Guid> seen, Guid id, string text)
    {
        if (!seen.Add(id))
            throw new ArgumentException($"An item with the same key has already been added: {id:D}");
        result.Add(new KeyValuePair<Guid, string>(id, text));
    }
}
