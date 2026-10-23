using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Dapper;

namespace Xdb.Api.Infrastructure.Data.Procedures;

public sealed class ActivityProcedures(XdbDataContext db) : LegacyGatewayBase(db), IActivityProcedures
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    /// <summary>en-US "G" with an ordinary space before AM/PM (ICU may use U+202F).</summary>
    private const string EnUsGeneralPattern = "M/d/yyyy h:mm:ss tt";

    /// <summary>OpenDataReader(proc, params, valueParameters): each value parameter is "%" + value + "%", timeout 30000.</summary>
    private static CommandDefinition LikeProcedure(string name, string phrase, CancellationToken cancellationToken) =>
        new(name, new { Phrase = "%" + phrase + "%" }, commandType: CommandType.StoredProcedure, commandTimeout: InlineTimeout, cancellationToken: cancellationToken);

    private async Task<DbDataReader> ReaderAsync(CommandDefinition command) =>
        await (await OpenAsync(command.CancellationToken)).ExecuteReaderAsync(command);

    private static T? Nullable<T>(DbDataReader reader, string column) where T : struct =>
        reader.IsDBNull(reader.GetOrdinal(column)) ? null : reader.GetFieldValue<T>(reader.GetOrdinal(column));

    private static string? Text(DbDataReader reader, string column) =>
        reader.IsDBNull(reader.GetOrdinal(column)) ? null : reader.GetString(reader.GetOrdinal(column));

    // ---- Submittals ----

    public async Task<IReadOnlyList<SubmittalListRow>> GetUnreviewedSubmittalsAsync(CancellationToken cancellationToken)
    {
        var rows = new List<SubmittalListRow>();
        await using var reader = await ReaderAsync(Procedure("spr_PropertyValueSubmittalList_GetDisplay", null, cancellationToken));
        if (!reader.HasRows)
            return rows;
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new SubmittalListRow(Nullable<Guid>(reader, "Id") ?? Guid.Empty, Text(reader, "AssetName"),
                Nullable<DateTime>(reader, "Created") ?? default, Text(reader, "CreatedByDisplay")));
        }
        return rows;
    }

    public async Task<SubmittalRow?> GetSubmittalAsync(Guid submittalId, CancellationToken cancellationToken)
    {
        await using var reader = await ReaderAsync(Procedure("spr_PropertyValueSubmittal_Get", new { Id = submittalId }, cancellationToken));
        if (!reader.HasRows || !await reader.ReadAsync(cancellationToken))
            return null;
        return new SubmittalRow(Nullable<Guid>(reader, "Id"), Nullable<Guid>(reader, "AssetId"), Text(reader, "Notes"), Nullable<DateTime>(reader, "Created"),
            Nullable<DateTime>(reader, "Reviewed"), Nullable<DateTime>(reader, "Deleted"), Nullable<Guid>(reader, "CreatedBy"), Nullable<Guid>(reader, "ReviewedBy"),
            Nullable<Guid>(reader, "DeletedBy"));
    }

    public async Task<IReadOnlyList<SubmittalValueRow>> GetSubmittalValuesAsync(Guid submittalId, CancellationToken cancellationToken)
    {
        var rows = new List<SubmittalValueRow>();
        await using var reader = await ReaderAsync(Procedure("spr_PropertyValueList_GetBySubmittalGroupId", new { SubmittalGroupId = submittalId }, cancellationToken));
        if (!reader.HasRows)
            return rows;
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new SubmittalValueRow(reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.IsDBNull(3) ? null : reader.GetGuid(3),
                reader.IsDBNull(4) ? null : reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetInt32(5), reader.GetDateTime(6),
                reader.IsDBNull(7) ? null : reader.GetDateTime(7), reader.IsDBNull(8) ? null : reader.GetDateTime(8), reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                reader.GetGuid(10), reader.IsDBNull(11) ? null : reader.GetGuid(11), reader.IsDBNull(12) ? null : reader.GetGuid(12),
                reader.IsDBNull(13) ? null : reader.GetGuid(13)));
        }
        return rows;
    }

    public async Task<IReadOnlyList<SubmittalSummaryRow>> GetSubmittalSummaryAsync(Guid submittalId, CancellationToken cancellationToken)
    {
        var rows = new List<SubmittalSummaryRow>();
        await using var reader = await ReaderAsync(Procedure("spr_PropertyValueSubmittal_GetSummary", new { Id = submittalId }, cancellationToken));
        if (!reader.HasRows)
            return rows;
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(new SubmittalSummaryRow(reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        return rows;
    }

    public async Task<DateTime?> GetSubmittalCreatedAsync(Guid submittalId, CancellationToken cancellationToken)
    {
        var value = await (await OpenAsync(cancellationToken)).ExecuteScalarAsync(new CommandDefinition(
            "SELECT [Created] FROM [PropertyValueSubmittals] WHERE [Id] = @Id", new { Id = submittalId }, cancellationToken: cancellationToken));
        if (value is null or DBNull)
            return null;
        var text = Convert.ToDateTime(value, CultureInfo.InvariantCulture).ToString(EnUsGeneralPattern, EnUs);
        return DateTime.TryParse(text, EnUs, DateTimeStyles.None, out var result) ? result : null;
    }

    public async Task<(Guid AssetId, string? Asset)?> GetSubmittalAssetInfoAsync(Guid submittalId, CancellationToken cancellationToken)
    {
        await using var reader = await ReaderAsync(Procedure("spr_PropertyValueSubmittal_GetAssetInfo", new { Id = submittalId }, cancellationToken));
        if (!reader.HasRows || !await reader.ReadAsync(cancellationToken))
            return null;
        return (reader.GetGuid(0), reader.GetString(1));
    }

    // ---- Activity feeds ----

    public async Task<IReadOnlyList<SubmittalActivityRow>> GetActivityAsync(string sql, CancellationToken cancellationToken)
    {
        var rows = new List<SubmittalActivityRow>();
        await using var reader = await ReaderAsync(Inline(sql, null, cancellationToken));
        if (!reader.HasRows)
            return rows;
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new SubmittalActivityRow(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3), reader.GetGuid(4), reader.GetString(5),
                reader.GetDateTime(6)));
        }
        return rows;
    }

    public async Task<IReadOnlyList<EditableRoleRow>> GetEditableRolesAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var rows = new List<EditableRoleRow>();
        await using var reader = await ReaderAsync(Procedure("spr_Roles_GetEditable", new { MemberId = memberId }, cancellationToken));
        if (!reader.HasRows)
            return rows;
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new EditableRoleRow(reader.GetGuid(0), reader.GetString(1), reader.GetGuid(2));
            _ = Convert.ToInt32(reader.GetValue(3), CultureInfo.InvariantCulture);
            rows.Add(row);
        }
        return rows;
    }

    public async Task ReadAssetTypesForMemberAsync(Guid memberId, CancellationToken cancellationToken)
    {
        await using var reader = await ReaderAsync(Procedure("spr_AssetTypes_ByMember_Get", new { MemberId = memberId }, cancellationToken));
        if (!reader.HasRows)
            return;
        while (await reader.ReadAsync(cancellationToken))
        {
        }
    }

    public async Task RunQueryAsync(string sql, CancellationToken cancellationToken)
    {
        await using var reader = await ReaderAsync(Inline(sql, null, cancellationToken));
        while (await reader.ReadAsync(cancellationToken))
        {
        }
    }

    // ---- Triggers ----

    public async Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetTriggerDictionaryAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var result = new List<KeyValuePair<Guid, string>>();
        var seen = new HashSet<Guid>();
        await using var reader = await ReaderAsync(Inline("SELECT [Id], [Name] FROM [Triggers] WITH (NoLock) WHERE [CreatedBy] = @MemberId ORDER BY [Name]",
            new { MemberId = memberId }, cancellationToken));
        if (!reader.HasRows)
            return result;
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

    public async Task<TriggerDetailRow?> GetTriggerAsync(Guid triggerId, CancellationToken cancellationToken)
    {
        await using var reader = await ReaderAsync(Inline(Lines("SELECT", "[Name],", "[Description],", "[IsActive],", "[TypeId],", "[Created],", "[CreatedBy],",
            "[Deleted],", "[DeletedBy]", "FROM [Triggers] WITH (NoLock) WHERE [Id] = @Id"), new { Id = triggerId }, cancellationToken));
        if (!reader.HasRows || !await reader.ReadAsync(cancellationToken))
            return null;
        return new TriggerDetailRow(reader.GetString(0), reader.GetString(1), Convert.ToString(reader.GetValue(2), CultureInfo.InvariantCulture)!,
            Convert.ToString(reader.GetValue(3), CultureInfo.InvariantCulture)!, reader.GetDateTime(4), reader.GetGuid(5),
            reader.IsDBNull(6) ? null : reader.GetDateTime(6), reader.IsDBNull(7) ? null : reader.GetGuid(7));
    }

    public async Task<IReadOnlyList<TriggerRestrictionDetailRow>> GetTriggerRestrictionsAsync(Guid triggerId, CancellationToken cancellationToken)
    {
        var rows = new List<TriggerRestrictionDetailRow>();
        await using var reader = await ReaderAsync(Inline(
            "SELECT [Id], [PropertyId], [RestrictedPropertyId], [SourcePropertyId] FROM [TriggersRestrictions] WHERE [TriggerId] = @TriggerId",
            new { TriggerId = triggerId }, cancellationToken));
        if (!reader.HasRows)
            return rows;
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(new TriggerRestrictionDetailRow(reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.IsDBNull(3) ? null : reader.GetGuid(3)));
        return rows;
    }

    public async Task<IReadOnlyList<TriggerPropertyRow>> GetTriggerPropertiesAsync(Guid triggerId, CancellationToken cancellationToken)
    {
        var rows = new List<TriggerPropertyRow>();
        await using var reader = await ReaderAsync(Inline(Lines("SELECT [Id], [PropertyId], [Value], [OperatorId], [Threshold]", "FROM [TriggersProperties]",
            "WHERE [TriggerId] = @TriggerId"), new { TriggerId = triggerId }, cancellationToken));
        if (!reader.HasRows)
            return rows;
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new TriggerPropertyRow(reader.GetGuid(0), reader.IsDBNull(1) ? null : reader.GetGuid(1), reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetGuid(3), reader.IsDBNull(4) ? null : reader.GetString(4)));
        }
        return rows;
    }

    public async Task<int> CountTriggerAssetTypesAsync(Guid triggerId, CancellationToken cancellationToken)
    {
        var count = 0;
        await using var reader = await ReaderAsync(Inline(Lines("SELECT", "[Id], [AssetTypeId], [IncludesChildren]", "FROM [TriggersAssetTypes]",
            "WHERE [TriggerId] = @TriggerId"), new { TriggerId = triggerId }, cancellationToken));
        if (!reader.HasRows)
            return count;
        while (await reader.ReadAsync(cancellationToken))
        {
            _ = reader.GetGuid(0);
            _ = reader.GetGuid(1);
            _ = bool.Parse(Convert.ToString(reader.GetValue(2), CultureInfo.InvariantCulture)!);
            count++;
        }
        return count;
    }

    public async Task<int> CountTriggerAssetsAsync(Guid triggerId, CancellationToken cancellationToken)
    {
        var count = 0;
        await using var reader = await ReaderAsync(Inline(Lines("SELECT [Id], [AssetId], [IncludesInstances]", "FROM [TriggersAssets]",
            "WHERE [TriggerId] = @TriggerId"), new { TriggerId = triggerId }, cancellationToken));
        if (!reader.HasRows)
            return count;
        while (await reader.ReadAsync(cancellationToken))
        {
            _ = reader.GetGuid(0);
            _ = reader.GetGuid(1);
            _ = bool.Parse(Convert.ToString(reader.GetValue(2), CultureInfo.InvariantCulture)!);
            count++;
        }
        return count;
    }

    public Task<IReadOnlyList<Guid>> GetTriggerMembersAsync(Guid triggerId, CancellationToken cancellationToken) =>
        GuidsAsync("SELECT [MemberId] FROM [TriggersMembers] WHERE [TriggerId] = @TriggerId", triggerId, cancellationToken);

    public Task<IReadOnlyList<Guid>> GetTriggerRolesAsync(Guid triggerId, CancellationToken cancellationToken) =>
        GuidsAsync("SELECT [RoleId] FROM [TriggersRoles] WHERE [TriggerId] = @TriggerId", triggerId, cancellationToken);

    private async Task<IReadOnlyList<Guid>> GuidsAsync(string sql, Guid triggerId, CancellationToken cancellationToken)
    {
        var rows = new List<Guid>();
        await using var reader = await ReaderAsync(Inline(sql, new { TriggerId = triggerId }, cancellationToken));
        if (!reader.HasRows)
            return rows;
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(reader.GetGuid(0));
        return rows;
    }

    public async Task<IReadOnlyList<TriggerTargetPropertyRow>> GetTriggerTargetPropertiesAsync(Guid triggerId, CancellationToken cancellationToken)
    {
        var rows = new List<TriggerTargetPropertyRow>();
        await using var reader = await ReaderAsync(Inline(Lines("SELECT", "[Id], [PropertyId], [NewValue]", "FROM [TriggersTargetProperties] WHERE [TriggerId] = @TriggerId"),
            new { TriggerId = triggerId }, cancellationToken));
        if (!reader.HasRows)
            return rows;
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(new TriggerTargetPropertyRow(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2)));
        return rows;
    }

    public async Task SaveTriggerAsync(Guid id, string? name, string? description, bool isActive, int typeId, DateTime created, Guid createdBy,
        CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Procedure("spr_Trigger_Save", Supplied(
            ("Id", id), ("Name", name), ("Description", description), ("IsActive", isActive), ("TriggerTypeId", typeId), ("Created", created),
            ("CreatedBy", createdBy)), cancellationToken));

    // ---- Search ----

    private async Task<IReadOnlyList<SearchRow>> SearchAsync(string procedure, string phrase, CancellationToken cancellationToken)
    {
        var rows = new List<SearchRow>();
        await using var reader = await ReaderAsync(LikeProcedure(procedure, phrase, cancellationToken));
        if (!reader.HasRows)
            return rows;
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new SearchRow(reader.GetGuid(0), reader.GetString(1), reader.GetGuid(2), reader.IsDBNull(3) ? null : reader.GetString(3),
                (bool)reader.GetValue(4)));
        }
        return rows;
    }

    public Task<IReadOnlyList<SearchRow>> SearchAssetsAsync(string phrase, CancellationToken cancellationToken) => SearchAsync("spr_Search_Asset", phrase, cancellationToken);

    public async Task<IReadOnlyList<SearchRow>> SearchPropertyValueTextAsync(string phrase, CancellationToken cancellationToken)
    {
        var rows = new List<SearchRow>();
        await using var reader = await ReaderAsync(LikeProcedure("spr_Search_PropVal_Text", phrase, cancellationToken));
        if (!reader.HasRows)
            return rows;
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(new SearchRow(reader.GetGuid(0), reader.GetString(1), reader.GetGuid(2), reader.GetString(3), (bool)reader.GetValue(4)));
        return rows;
    }

    public async Task<IReadOnlyList<SearchValueAssetRow>> SearchPropertyValueAssetsAsync(Guid memberId, string assetIds, CancellationToken cancellationToken)
    {
        var rows = new List<SearchValueAssetRow>();
        await using var reader = await ReaderAsync(Procedure("spr_Search_PropertyValue_Asset", new { MemberId = memberId, AssetIds = assetIds }, cancellationToken));
        if (!reader.HasRows)
            return rows;
        while (await reader.ReadAsync(cancellationToken))
        {
            var instance = (bool)reader.GetValue(6);
            var description = reader.IsDBNull(5) ? null : reader.GetString(5);
            // GetGuid(7) is read only for an instance with a description (a NULL InstanceOfId then fails).
            Guid? instanceOf = instance && description is not null ? reader.GetGuid(7) : null;
            rows.Add(new SearchValueAssetRow(reader.GetGuid(0), reader.IsDBNull(1) ? string.Empty : reader.GetString(1), reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                reader.IsDBNull(3) ? string.Empty : reader.GetString(3), reader.GetGuid(4), description, instance, instanceOf));
        }
        return rows;
    }

    public Task<IReadOnlyList<SearchRow>> SearchUsersAsync(string phrase, CancellationToken cancellationToken) => SearchAsync("spr_Search_User", phrase, cancellationToken);

    public async Task<IReadOnlyList<string>> SearchTagAsync(string procedure, string phrase, CancellationToken cancellationToken)
    {
        var names = new List<string>();
        await using var reader = await ReaderAsync(LikeProcedure(procedure, phrase, cancellationToken));
        if (!reader.HasRows)
            return names;
        while (await reader.ReadAsync(cancellationToken))
        {
            _ = reader.GetGuid(0);
            names.Add(reader.GetString(1));
            _ = reader.GetGuid(2);
        }
        return names;
    }

    public async Task InsertSearchAsync(string phrase, Guid userId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(new CommandDefinition("INSERT INTO [Searches] ([SearchPhrase], [CreatedBy]) VALUES (@SearchPhrase, @UserId)",
            new { SearchPhrase = phrase, UserId = userId }, commandTimeout: ProcedureTimeout, cancellationToken: cancellationToken));
}
