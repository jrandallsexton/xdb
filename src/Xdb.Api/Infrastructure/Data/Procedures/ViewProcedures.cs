using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Dapper;

namespace Xdb.Api.Infrastructure.Data.Procedures;

public sealed class ViewProcedures(XdbDataContext db) : LegacyGatewayBase(db), IViewProcedures
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    // ---- Views ----

    public async Task<ViewRow?> GetViewAsync(Guid viewId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).QueryFirstOrDefaultAsync<ViewRow>(Procedure("spr_View_Get", new { Id = viewId }, cancellationToken));

    public async Task<IReadOnlyList<ViewPropertyGroupRelationRow>> GetViewPropertyGroupRelationsAsync(Guid viewId, CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<ViewPropertyGroupRelationRow>(
            Procedure("spr_ViewPropertyGroupRelationList_GetByViewId", new { ViewId = viewId }, cancellationToken))).ToList();

    public async Task<IReadOnlyList<PropertyGroupRow>> GetViewPropertyGroupsAsync(Guid viewId, CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<PropertyGroupRow>(
            Procedure("spr_PropertyGroupList_GetByViewId", new { ViewId = viewId }, cancellationToken))).ToList();

    public async Task<IReadOnlyList<PropertyGroupPropertyRelationRow>> GetPropertyGroupRelationsAsync(Guid propertyGroupId, CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<PropertyGroupPropertyRelationRow>(
            Procedure("spr_PropertyGroupPropertyRelationList_GetByPropertyGroupId", new { PropertyGroupId = propertyGroupId }, cancellationToken))).ToList();

    public async Task<IReadOnlyList<GroupPropertyRow>> GetPropertyGroupPropertiesAsync(Guid propertyGroupId, CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<GroupPropertyRow>(
            Procedure("spr_PropertyList_GetByPropertyGroupId", new { PropertyGroupId = propertyGroupId }, cancellationToken))).ToList();

    public async Task<IReadOnlyList<ViewFilterRow>> GetViewFiltersAsync(Guid viewId, CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<ViewFilterRow>(Inline(Lines(
            "SELECT VF.[Id], VF.[PropertyId], IsNull(P.[DisplayValue], P.[Name]) AS [Property],",
            "VF.[OperatorId], O.[Operator], VF.[Value], VF.[Order]",
            "FROM [ViewFilters] VF WITH (NoLock)",
            "INNER JOIN [Properties] P WITH (NoLock) ON P.[Id] = VF.[PropertyId]",
            "INNER JOIN [Operators] O WITH (NoLock) ON O.[Id] = VF.[OperatorId]",
            "WHERE VF.[ViewId] = @ViewId",
            "ORDER BY VF.[Order]"), new { ViewId = viewId }, cancellationToken))).ToList();

    public async Task<IReadOnlyList<Guid>> GetViewTriggerIdsAsync(Guid viewId, CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<Guid>(Inline(Lines(
            "SELECT VT.[TriggerId] FROM [ViewsTriggers] VT WITH (NoLock)",
            "INNER JOIN [Triggers] T WITH (NoLock) ON T.[Id] = VT.[TriggerId]",
            "WHERE (T.[Deleted] IS NULL)",
            "AND (T.[IsActive] = 1)",
            "AND (VT.[ViewId] = @ViewId)"), new { ViewId = viewId }, cancellationToken))).ToList();

    public async Task<TriggerRow?> GetTriggerAsync(Guid triggerId, CancellationToken cancellationToken)
    {
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(Lines(
            "SELECT", "[Name],", "[Description],", "[IsActive],", "[TypeId],", "[Created],", "[CreatedBy],", "[Deleted],", "[DeletedBy]",
            "FROM [Triggers] WITH (NoLock) WHERE [Id] = @Id"), new { Id = triggerId }, cancellationToken));
        if (!await reader.ReadAsync(cancellationToken))
            return null;
        // Read as the original did: GetString for the texts, ToString() of the bit and the type.
        return new TriggerRow
        {
            Name = reader.GetString(0),
            Description = reader.GetString(1),
            IsActive = reader.GetValue(2).ToString()!,
            TypeId = reader.GetValue(3).ToString()!
        };
    }

    public async Task<IReadOnlyList<TriggerRestrictionRow>> GetTriggerRestrictionsAsync(Guid triggerId, CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<TriggerRestrictionRow>(Inline(
            "SELECT [Id], [PropertyId], [RestrictedPropertyId], [SourcePropertyId] FROM [TriggersRestrictions] WHERE [TriggerId] = @TriggerId",
            new { TriggerId = triggerId }, cancellationToken))).ToList();

    public Task<int> CountCreatePermissionsAsync(Guid userId, Guid assetTypeId, CancellationToken cancellationToken) =>
        ScalarIntAsync(Lines(
            "select COUNT(RATP.[Id])",
            "FROM [RolesAssetTypesPermissions] RATP WITH (NoLock)",
            "INNER JOIN [Permissions] P WITH (NoLock) ON P.[Id] = RATP.[PermissionId]",
            "INNER JOIN [RolesMembers] RM WITH (NoLock) ON RM.[RoleId] = RATP.[RoleId]",
            "WHERE RM.[MemberId] = @UserId",
            "AND RM.[Deleted] IS NULL",
            "AND RATP.[Deleted] IS NULL",
            "AND P.[Value] = @PermissionId",
            "AND RATP.[AssetTypeId] = @AssetTypeId"), new { UserId = userId, PermissionId = 0, AssetTypeId = assetTypeId }, cancellationToken);

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetMemberFiltersAsync(Guid viewId, Guid memberId, CancellationToken cancellationToken) =>
        GuidPairsAsync(Inline("SELECT [Id], [Name] FROM [MemberViews] WHERE [MemberId] = @MemberId AND [ViewId] = @ViewId ORDER BY [Name]",
            new { MemberId = memberId, ViewId = viewId }, cancellationToken), skipNulls: false, cancellationToken, unique: true);

    public async Task<byte[]?> GetViewClientCacheAsync(Guid viewId, Guid userId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).QueryFirstOrDefaultAsync<byte[]>(Inline(
            "SELECT [Data] FROM [ViewClients] WHERE [ViewId] = @ViewId AND [UserId] = @UserId", new { ViewId = viewId, UserId = userId }, cancellationToken));

    public async Task InsertViewClientCacheAsync(Guid viewId, Guid userId, byte[] data, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Inline(
            "INSERT INTO [ViewClients] ([ViewId], [UserId], [Data]) VALUES (@ViewId, @UserId, @Data)",
            new { ViewId = viewId, UserId = userId, Data = data }, cancellationToken));

    public async Task<DateTime?> GetViewLastModifiedAsync(Guid viewId, CancellationToken cancellationToken)
    {
        // ExecuteScalarDateTime: ToString() then TryParse in the host culture (en-US); NULL gives "" which does not parse.
        var value = await (await OpenAsync(cancellationToken)).ExecuteScalarAsync(Inline("SELECT [LastModified] FROM [Views] WHERE [Id] = @Id", new { Id = viewId }, cancellationToken));
        if (value is null or DBNull)
            return null;
        var text = Convert.ToDateTime(value, CultureInfo.InvariantCulture).ToString(EnUsGeneralPattern, EnUs);
        return DateTime.TryParse(text, EnUs, DateTimeStyles.None, out var result) ? result : null;
    }

    /// <summary>en-US "G" with an ordinary space before AM/PM (ICU may use U+202F).</summary>
    public const string EnUsGeneralPattern = "M/d/yyyy h:mm:ss tt";

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetViewsByAssetTypeAsync(Guid assetTypeId, int requestType, bool includeNonStandAlone, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        Line(sb, "SELECT [Id], [DisplayValue] FROM [Views] WITH (NoLock)");
        Line(sb, "WHERE [AssetTypeId] = @AssetTypeId");
        Line(sb, "AND [Deleted] IS NULL");
        if (requestType == 0)
            Line(sb, "AND [IsInstance] = 0");
        else if (requestType == 1)
            Line(sb, "AND [IsInstance] = 1");
        if (!includeNonStandAlone)
            Line(sb, "AND [IsStandAlone] = 1");
        Line(sb, "ORDER BY [DisplayValue]");
        return GuidPairsAsync(Inline(sb.ToString(), new { AssetTypeId = assetTypeId }, cancellationToken), skipNulls: false, cancellationToken, unique: true);
    }

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetViewsByPropertyGroupAsync(Guid propertyGroupId, CancellationToken cancellationToken) =>
        GuidPairsAsync(Inline(Lines(
            "SELECT V.[Id], V.[DisplayValue]",
            "FROM [Views] V WITH (NoLock)",
            "INNER JOIN [ViewsPropertyGroups] VPG WITH (NoLock) ON VPG.[ViewId] = V.[Id]",
            "INNER JOIN [PropertyGroups] PG WITH (NoLock) ON PG.[Id] = VPG.[PropertyGroupId]",
            "WHERE VPG.[PropertyGroupId] = @PropertyGroupId",
            "AND VPG.[Deleted] IS NULL",
            "AND V.[Deleted] IS NULL",
            "AND PG.[Deleted] IS NULL",
            "ORDER BY V.[DisplayValue]"), new { PropertyGroupId = propertyGroupId }, cancellationToken), skipNulls: false, cancellationToken, unique: true);

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetViewsDictionaryAsync(bool includeDeleted, bool includeNonStandAlone, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        Line(sb, "SELECT [Id], ");
        Line(sb, "[Name] + ' (' + [DisplayValue] + ')' FROM [Views] WITH (NoLock)");
        var where = false;
        if (!includeDeleted)
        {
            where = true;
            Line(sb, "WHERE [Deleted] IS NULL");
        }
        if (!includeNonStandAlone)
            Line(sb, where ? "AND [IsStandAlone] = 1" : "WHERE [IsStandAlone] = 1");
        Line(sb, "ORDER BY [Name]");
        return LegacyDictionaryAsync(Inline(sb.ToString(), null, cancellationToken), firstWins: true, cancellationToken);
    }

    public async Task<MemberViewRow?> GetMemberViewAsync(Guid memberViewId, CancellationToken cancellationToken)
    {
        await using var results = await (await OpenAsync(cancellationToken)).QueryMultipleAsync(Procedure("spr_MemberView_Get", new { Id = memberViewId }, cancellationToken));
        return (await results.ReadAsync<MemberViewRow>()).FirstOrDefault();
    }

    public Task<bool> IsLiveViewAsync(Guid viewId, CancellationToken cancellationToken) =>
        CountIsOneAsync(Inline("SELECT COUNT(*) FROM [Views] WITH (NoLock) WHERE [Id] = @Id AND [Deleted] IS NULL", new { Id = viewId }, cancellationToken));

    public Task<Guid> GetViewAssetTypeIdAsync(Guid viewId, CancellationToken cancellationToken) =>
        ScalarGuidAsync("SELECT [AssetTypeId] FROM [Views] WITH (NoLock) WHERE [Id] = @ViewId", new { ViewId = viewId }, cancellationToken);

    public Task<bool> IsLiveInstanceViewAsync(Guid viewId, CancellationToken cancellationToken) =>
        CountIsOneAsync(Inline("SELECT COUNT(*) FROM [Views] WITH (NoLock) WHERE [Id] = @ViewId AND [IsInstance] = 1 AND [Deleted] IS NULL",
            new { ViewId = viewId }, cancellationToken));

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetUserViewsAsync(Guid userId, CancellationToken cancellationToken) =>
        LegacyDictionaryAsync(Procedure("spr_ViewDictionary_GetByUserId", new { UserId = userId }, cancellationToken), firstWins: false, cancellationToken);

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetUserViewGroupsAsync(Guid userId, CancellationToken cancellationToken) =>
        LegacyDictionaryAsync(Procedure("spr_ViewGroups_GetByUserId", new { UserId = userId }, cancellationToken), firstWins: false, cancellationToken);

    // ---- Processes ----

    public async Task<ProcessRow?> GetProcessAsync(Guid processId, CancellationToken cancellationToken)
    {
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(
            "select P.[Id], P.[Name], P.[DisplayValue], P.[AssetTypeId], P.[Created], P.[CreatedBy] from Processes P WHERE [Id] = @Id", new { Id = processId }, cancellationToken));
        if (!await reader.ReadAsync(cancellationToken))
            return null;
        // Unguarded reads, as in the original: a NULL DisplayValue or AssetTypeId fails.
        return new ProcessRow { Id = reader.GetGuid(0), Name = reader.GetString(1), DisplayValue = reader.GetString(2), AssetTypeId = reader.GetGuid(3) };
    }

    public async Task<IReadOnlyList<ProcessStepRow>> GetProcessStepsAsync(Guid processId, CancellationToken cancellationToken)
    {
        var steps = new List<ProcessStepRow>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(
            "SELECT [Id], [Order], [Name], [ViewId], [IsInstance], [IsCreation], [IsEdit], [ShowAssetTypeSelector], [AssetNameLabel] FROm [ProcessSteps] WHERE [ProcessId] = @Id",
            new { Id = processId }, cancellationToken));
        while (await reader.ReadAsync(cancellationToken))
        {
            steps.Add(new ProcessStepRow
            {
                Id = reader.GetGuid(0), Order = reader.GetInt32(1), Name = reader.GetString(2), ViewId = reader.GetGuid(3),
                IsInstance = (bool)reader.GetValue(4), IsCreation = (bool)reader.GetValue(5), IsEdit = (bool)reader.GetValue(6),
                ShowAssetTypeSelector = (bool)reader.GetValue(7), AssetNameLabel = reader.GetString(8)
            });
        }
        return steps;
    }

    public async Task<IReadOnlyList<ProcessStepFilterRow>> GetProcessStepFiltersAsync(Guid processStepId, CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<ProcessStepFilterRow>(Inline(
            "SELECT [Id], [PropertyId], [Order] FROM [ProcessStepFilters] WHERE [ProcessStepId] = @Id ORDER BY [Order]", new { Id = processStepId }, cancellationToken))).ToList();

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetProcessesDictionaryAsync(CancellationToken cancellationToken) =>
        GuidPairsAsync(Inline("SELECT [Id], IsNull([DisplayValue], [Name]) AS [Name] FROM [Processes] WITH (NoLock) WHERE [Deleted] IS NULL ORDER BY [Name]", null, cancellationToken),
            skipNulls: false, cancellationToken, unique: true);

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetProcessesByAssetTypeAsync(Guid assetTypeId, bool isInstance, CancellationToken cancellationToken) =>
        // @AssetTypeIds went as a string parameter (nvarchar).
        GuidPairsAsync(Procedure("spr_Processes_GetBy_AssetTypeIds", new { AssetTypeIds = assetTypeId.ToString("D"), IsInstance = isInstance }, cancellationToken),
            skipNulls: false, cancellationToken, unique: true);

    public async Task<string> GetProcessStepAssetNameLabelAsync(Guid processId, int stepIndex, CancellationToken cancellationToken)
    {
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(Lines(
            "SELECT [AssetNameLabel] FROM [ProcessSteps] WITH (NoLock) WHERE",
            "[ProcessId] = @ProcessId AND [Order] = @Order"), new { ProcessId = processId, Order = stepIndex }, cancellationToken));
        return await reader.ReadAsync(cancellationToken) ? reader.GetString(0) : string.Empty;
    }

    public async Task<bool> GetProcessStepFlagAsync(Guid processId, int stepIndex, string column, CancellationToken cancellationToken)
    {
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(Lines(
            $"SELECT [{column}] FROM [ProcessSteps] WITH (NoLock) WHERE",
            "[ProcessId] = @ProcessId AND [Order] = @Order"), new { ProcessId = processId, Order = stepIndex }, cancellationToken));
        return await reader.ReadAsync(cancellationToken) && (bool)reader.GetValue(0);
    }

    public Task<Guid> GetProcessStepIdAsync(Guid processId, int stepIndex, CancellationToken cancellationToken) =>
        ScalarGuidAsync("SELECT [Id] FROM [ProcessSteps] WITH (NoLock) WHERE [ProcessId] = @ProcessId AND [Order] = @Order",
            new { ProcessId = processId, Order = stepIndex }, cancellationToken);

    public async Task<IReadOnlyList<KeyValuePair<int, Guid>>> GetProcessStepFilterPropertiesAsync(Guid processStepId, CancellationToken cancellationToken)
    {
        var result = new List<KeyValuePair<int, Guid>>();
        var seen = new HashSet<int>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(
            "SELECT [Order], [PropertyId] FROM [ProcessStepFilters] WITH (NoLock) WHERE [ProcessStepId] = @StepId ORDER BY [Order]",
            new { StepId = processStepId }, cancellationToken));
        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.IsDBNull(0) || reader.IsDBNull(1))
                continue;
            var order = reader.GetInt32(0);
            if (!seen.Add(order))
                throw new ArgumentException($"An item with the same key has already been added: {order}");
            result.Add(new KeyValuePair<int, Guid>(order, reader.GetGuid(1)));
        }
        return result;
    }

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetUsedPickListValuesAsync(Guid propertyId, CancellationToken cancellationToken) =>
        GuidPairsAsync(Inline(Lines(
            "SELECT DISTINCT PLV.[Id], IsNull(PLV.[DisplayValue], PLV.[Value]) AS [Value]",
            "FROM PickListValues PLV WITH (NoLock)",
            "INNER JOIN PropertyValues PV WITH (NoLock) ON PV.[Value] = PLV.[Id]",
            "WHERE PV.[PropertyId] = @PropertyId",
            "ORDER BY [Value]"), new { PropertyId = propertyId }, cancellationToken), skipNulls: true, cancellationToken, unique: true);

    public async Task<IReadOnlyList<Guid>> GetAssetsByPropertyValueAsync(Guid propertyId, string? value, CancellationToken cancellationToken)
    {
        // The original embedded the value as a literal. Recorded deviation: it is parameterized here, and a value
        // containing an apostrophe fails as the original's broken SQL did (see the handler).
        var rows = await (await OpenAsync(cancellationToken)).QueryAsync<(Guid Id, string Name)>(Inline(Lines(
            "SELECT DISTINCT A.[Id], A.[Name]",
            "FROM [Assets] A WITH (NoLock)",
            "INNER JOIN [PropertyValues] PV0 WITH (NoLock) ON PV0.[AssetId] = A.[Id]",
            "WHERE (",
            "(PV0.[PropertyId] = @PropertyId AND PV0.[Value] = @Value)",
            ")",
            "AND PV0.[Deleted] IS NULL",
            "AND PV0.[Approved] IS NOT NULL",
            "AND PV0.[Rejected] IS NULL",
            "AND A.[Deleted] IS NULL",
            "AND A.[Approved] IS NOT NULL",
            "ORDER BY A.[Name]"),
            new { PropertyId = propertyId.ToString("D"), Value = new DbString { Value = value ?? string.Empty, IsAnsi = true, Length = -1 } }, cancellationToken));
        var ids = new List<Guid>();
        var seen = new HashSet<Guid>();
        foreach (var row in rows)
        {
            if (!seen.Add(row.Id))
                throw new ArgumentException($"An item with the same key has already been added: {row.Id:D}");
            ids.Add(row.Id);
        }
        return ids;
    }

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetUsedPickListValuesForAssetsAsync(IReadOnlyList<Guid> assetIds, Guid propertyId, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        Line(sb, "SELECT DISTINCT PLV.[Id], IsNull(PLV.[DisplayValue], PLV.[Value]) AS [Value]");
        Line(sb, "FROM PickListValues PLV WITH (NoLock)");
        Line(sb, "INNER JOIN PropertyValues PV WITH (NoLock) ON PV.[Value] = PLV.[Id]");
        Line(sb, "WHERE PV.[PropertyId] = @PropertyId AND PV.[Deleted] IS NULL");
        Line(sb, "AND PV.[AssetId] IN (");
        for (var i = 0; i < assetIds.Count; i++)
            Line(sb, i == assetIds.Count - 1 ? $"'{assetIds[i]:D}')" : $"'{assetIds[i]:D}',");
        Line(sb, "ORDER BY [Value]");
        return GuidPairsAsync(Inline(sb.ToString(), new { PropertyId = propertyId }, cancellationToken), skipNulls: true, cancellationToken, unique: true);
    }

    // ---- Help ----

    public async Task<HelpSectionRow?> GetHelpSectionAsync(Guid helpSectionId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).QueryFirstOrDefaultAsync<HelpSectionRow>(Inline(
            "SELECT [Title], [ImageId], [Order], [Content] FROM [HelpContent] WITH (NoLock) WHERE [Id] = @Id", new { Id = helpSectionId }, cancellationToken));

    public async Task<IReadOnlyList<Guid>> GetHelpSectionIdsAsync(CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<Guid>(Inline("SELECT [Id] FROM [HelpContent] WITH (NoLock) ORDER BY [ORDER]", null, cancellationToken))).ToList();

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetHelpSectionsDictionaryAsync(CancellationToken cancellationToken) =>
        GuidPairsAsync(Inline("SELECT [Id], [Title] FROM [HelpContent] WITH (NoLock) ORDER BY [Order]", null, cancellationToken), skipNulls: false, cancellationToken, unique: true);

    // ---- Property groups ----

    public async Task<PropertyGroupRow?> GetPropertyGroupAsync(Guid propertyGroupId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).QueryFirstOrDefaultAsync<PropertyGroupRow>(Procedure("spr_PropertyGroup_Get", new { Id = propertyGroupId }, cancellationToken));

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetPropertyGroupsDictionaryAsync(bool includeDeleted, Guid? assetTypeId, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        Line(sb, "SELECT [Id], ");
        Line(sb, "[Name] + ' (' + [DisplayValue] + ')' FROM [PropertyGroups] WITH (NoLock)");
        var where = false;
        if (assetTypeId.HasValue)
        {
            where = true;
            Line(sb, "WHERE [AssetTypeId] = @AssetTypeId");
        }
        if (!includeDeleted)
            Line(sb, where ? "AND [Deleted] IS NULL" : "WHERE [Deleted] IS NULL");
        Line(sb, "ORDER BY [Name]");
        return GuidPairsAsync(Inline(sb.ToString(), assetTypeId.HasValue ? new { AssetTypeId = assetTypeId.Value } : null, cancellationToken),
            skipNulls: true, cancellationToken, unique: true);
    }

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetPropertyGroupsByPropertyAsync(Guid propertyId, CancellationToken cancellationToken) =>
        GuidPairsAsync(Inline(Lines(
            "SELECT PG.[Id], IsNull(PG.[DisplayValue], PG.[Name]) FROM [PropertyGroups] PG WITH (NoLock)",
            "INNER JOIN [PropertyGroupsProperties] PGP WITH (NoLock) ON PGP.[PropertyGroupId] = PG.[Id]",
            "INNER JOIN [Properties] P WITH (NoLock) ON P.Id = PGP.[PropertyId]",
            "WHERE PG.[Deleted] IS NULL AND",
            "PGP.[Deleted] IS NULL AND",
            "P.[Deleted] IS NULL AND",
            "P.[Id] = @PropertyId",
            "ORDER BY PG.[Name]"), new { PropertyId = propertyId }, cancellationToken), skipNulls: false, cancellationToken, unique: true);

    // ---- Roles ----

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetRolesByViewAsync(Guid viewId, CancellationToken cancellationToken) =>
        GuidPairsAsync(Procedure("spr_Roles_GetByViewId", new { ViewId = viewId }, cancellationToken), skipNulls: false, cancellationToken, unique: true);

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetMemberRolesAsync(Guid memberId, CancellationToken cancellationToken) =>
        GuidPairsAsync(Inline(Lines(
            "SELECT R.[Id], R.[Name]",
            "FROM [Roles] R WITH (NoLock)",
            "INNER JOIN [RoleMembers] RM WITH (NoLock) ON RM.[RoleId] = R.[Id]",
            "INNER JOIN [Members] M WITH (NoLock) ON M.[Id] = RM.[MemberId]",
            "WHERE",
            "R.[Deleted] IS NULL",
            "AND R.[IsActive] = 1",
            "AND RM.[Deleted] IS NULL",
            "AND M.[Deleted] IS NULL",
            "AND RM.[MemberId] = @MemberId",
            "order by r.[name]"), new { MemberId = memberId }, cancellationToken), skipNulls: true, cancellationToken, unique: true);

    public async Task<IReadOnlyList<RoleViewRelationRow>> GetRoleViewRelationsAsync(Guid roleId, CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<RoleViewRelationRow>(
            Procedure("spr_RoleViewRelationList_GetByRoleId", new { RoleId = roleId }, cancellationToken))).ToList();
}
