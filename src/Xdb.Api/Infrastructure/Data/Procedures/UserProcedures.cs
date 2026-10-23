using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Dapper;

namespace Xdb.Api.Infrastructure.Data.Procedures;

public sealed class UserProcedures(XdbDataContext db) : LegacyGatewayBase(db), IUserProcedures
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    /// <summary>en-US "G" with an ordinary space before AM/PM (ICU may use U+202F).</summary>
    private const string EnUsGeneralPattern = "M/d/yyyy h:mm:ss tt";

    // ---- Members ----

    public async Task<MemberDetailRow?> GetMemberAsync(Guid memberId, CancellationToken cancellationToken)
    {
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Procedure("spr_Member_Get", new { Id = memberId }, cancellationToken));
        if (!reader.HasRows || !await reader.ReadAsync(cancellationToken))
            return null;
        return new MemberDetailRow(
            memberId,
            reader.GetString(0),
            reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            reader.GetDateTime(4),
            reader.GetGuid(5),
            reader.IsDBNull(6) ? null : reader.GetDateTime(6),
            reader.IsDBNull(7) ? null : reader.GetDateTime(7),
            reader.IsDBNull(8) ? null : reader.GetGuid(8),
            (bool)reader.GetValue(9),
            !reader.IsDBNull(10) && (bool)reader.GetValue(10));
    }

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetActiveRolesAsync(Guid memberId, CancellationToken cancellationToken) =>
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

    public async Task<DateTime?> GetLastModifiedAsync(Guid memberId, CancellationToken cancellationToken)
    {
        // ExecuteScalarDateTime: ToString() then TryParse in the host culture (en-US); NULL gives "" which does not parse.
        var value = await (await OpenAsync(cancellationToken)).ExecuteScalarAsync(Inline(
            "SELECT [LastModified] FROM [Members] WITH (NoLock) WHERE [Id] = @Id", new { Id = memberId }, cancellationToken));
        if (value is null or DBNull)
            return null;
        var text = Convert.ToDateTime(value, CultureInfo.InvariantCulture).ToString(EnUsGeneralPattern, EnUs);
        return DateTime.TryParse(text, EnUs, DateTimeStyles.None, out var result) ? result : null;
    }

    public Task<Guid> GetMemberIdByLoginAsync(string? login, CancellationToken cancellationToken) =>
        ScalarGuidAsync("SELECT [Id] FROM [Members] WITH (NoLock) WHERE [UserId] = @UserId", Supplied(("UserId", login)), cancellationToken);

    public async Task<IReadOnlyList<MemberVisitRow>> GetVisitsAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var rows = new List<MemberVisitRow>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Procedure("spr_MemberVisits_Get", new { MemberId = memberId }, cancellationToken));
        if (!reader.HasRows)
            return rows;
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(new MemberVisitRow(reader.GetDateTime(0), reader.IsDBNull(1) ? null : reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2)));
        return rows;
    }

    public async Task SaveMemberAsync(Guid id, string userId, string? lastName, string? firstName, DateTime created, Guid createdBy, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Procedure("spr_Member_Save", Supplied(
            ("Id", id), ("UserId", userId), ("LName", lastName), ("FName", firstName), ("Created", created), ("CreatedBy", createdBy)), cancellationToken));

    // ---- Client model ----

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetProcessesAsync(Guid memberId, bool isAdmin, CancellationToken cancellationToken)
    {
        var sql = isAdmin
            ? Lines(
                "SELECT DISTINCT P.[Id], IsNull(P.[DisplayValue], P.[Name]) AS [Name]",
                "FROM [Processes] P WITH (NoLock)",
                "WHERE P.[Deleted] IS NULL",
                "ORDER BY [Name]")
            : Lines(
                "SELECT DISTINCT P.[Id], IsNull(P.[DisplayValue], P.[Name]) AS [Name]",
                "FROM [Processes] P WITH (NoLock)",
                "INNER JOIN [RolesAssetTypesProcesses] RP WITH (NoLock) ON RP.[ProcessId] = P.[Id]",
                "INNER JOIN [Roles] R WITH (NoLock) ON R.[Id] = RP.[RoleID]",
                "INNER JOIN [RolesMembers] RM WITH (NoLock) ON RM.[RoleId] = R.[Id]",
                "WHERE RM.[MemberId] = @UserId",
                "AND RM.[Deleted] IS NULL",
                "AND RP.[Deleted] IS NULL",
                "AND R.[Deleted] IS NULL",
                "AND P.[Deleted] IS NULL",
                "ORDER BY [Name]");
        return GuidPairsAsync(Inline(sql, new { UserId = memberId }, cancellationToken), skipNulls: true, cancellationToken, unique: true);
    }

    public async Task<IReadOnlyList<ShortcutRow>> GetShortcutsAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var rows = new List<ShortcutRow>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Procedure("spr_Bookmarks_GetShortcuts", new { MemberId = memberId }, cancellationToken));
        if (!reader.HasRows)
            return rows;
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(new ShortcutRow(reader.IsDBNull(5) ? Guid.Empty : Guid.Parse(reader.GetValue(5).ToString()!), reader.IsDBNull(7) ? null : reader.GetString(7)));
        return rows;
    }

    public async Task<IReadOnlyList<Guid>> GetDashboardIdsAsync(Guid memberId, CancellationToken cancellationToken) =>
        await QueryGuardedAsync<Guid>(Inline("SELECT [Id] FROM [Dashboards] WITH (NoLock) WHERE [MemberId] = @MemberId AND [Deleted] IS NULL ORDER BY [Index]",
            new { MemberId = memberId }, cancellationToken));

    // ---- Settings and preferences ----

    public Task<string> GetMemberSettingAsync(Guid memberId, string key, CancellationToken cancellationToken) =>
        ScalarStringAsync("SELECT [Value] FROM [MemberSettings] WHERE [MemberId] = @UserId AND [Key] = @Key", new { UserId = memberId, Key = key }, cancellationToken);

    public async Task InsertMemberSettingAsync(Guid memberId, string key, string value, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Inline("INSERT INTO [MemberSettings] ([MemberId], [Key], [Value]) VALUES (@UserId, @Key, @Val)",
            new { UserId = memberId, Key = key, Val = value }, cancellationToken));

    public async Task<bool> SaveUserPreferenceAsync(Guid memberId, Guid optionTypeId, string? optionValue, CancellationToken cancellationToken)
    {
        // Built by concatenation in the original (no parameters): the identical text keeps its quoting failures.
        var connection = await OpenAsync(cancellationToken);
        var count = "SELECT COUNT(*) FROM [MemberOptions]" + $"WHERE [MemberId] = '{memberId:D}'" + $" AND [OptionTypeId] = '{optionTypeId:D}'"
            + $" AND [OptionValue] = '{optionValue}'";
        // ExecuteScalar(string): Text, default timeout, (int) cast.
        if ((int)(await connection.ExecuteScalarAsync(new CommandDefinition(count, commandTimeout: 30, cancellationToken: cancellationToken)))! != 0)
            return true;
        var insert = "INSERT INTO [MemberOptions] ([Id], [MemberId], [OptionTypeId], [OptionValue]) VALUES (NEWID(), "
            + $"'{memberId:D}', '{optionTypeId:D}', '{optionValue}')";
        return await connection.ExecuteAsync(new CommandDefinition(insert, commandTimeout: 300, cancellationToken: cancellationToken)) >= -1;
    }

    // ---- Errors and feedback ----

    public async Task<(string Message, string? Exception)?> GetErrorAsync(Guid errorId, CancellationToken cancellationToken)
    {
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(
            "SELECT [Message], [Exception] FROM [Errors] WHERE [Id] = @Id", new { Id = errorId }, cancellationToken));
        if (!reader.HasRows || !await reader.ReadAsync(cancellationToken))
            return null;
        return (reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1));
    }

    public async Task InsertBugAsync(int area, int criticality, string? title, string? bug, Guid createdBy, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Inline(Lines(
                "INSERT INTO [Bugs] ([Area], [Criticality], [Title], [Bug], [CreatedBy])",
                " VALUES (@Area, @Criticality, @Title, @Bug, @CreatedBy)"),
            Supplied(("Area", area), ("Criticality", criticality), ("Title", title), ("Bug", bug), ("CreatedBy", createdBy)), cancellationToken));

    public async Task InsertSuggestionAsync(string? title, string? suggestion, Guid createdBy, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Inline(
            "INSERT INTO [Suggestions] ([Title], [Suggestion], [CreatedBy]) VALUES (@Title, @Suggestion, @CreatedBy)",
            Supplied(("Title", title), ("Suggestion", suggestion), ("CreatedBy", createdBy)), cancellationToken));

    // ---- Alerts ----

    public async Task<IReadOnlyList<AlertRow>> GetAlertsAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var rows = new List<AlertRow>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(Lines(
            "SELECT", "TLM.[Id],", "TLM.[TriggerLogId],", "TL.[TriggerId],", "TL.[AssetId],", "TL.[SubmittalId],", "TL.[PropertyValueId],", "TL.[PropertyId],",
            "TL.[OldValue],", "TL.[NewValue],", "TL.[TriggerText],", "TLM.[Viewed],", "TLM.[Read],", "TLM.[Pinned]",
            "FROM [TriggerLogsMembers] TLM",
            "INNER JOIN [TriggerLogs] TL ON TL.[Id] = TLM.[TriggerLogId]",
            "WHERE TLM.[MemberId] = @MemberId",
            "ORDER BY TLM.[Created] DESC"), new { MemberId = memberId }, cancellationToken));
        if (!reader.HasRows)
            return rows;
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new AlertRow(reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetGuid(3), reader.GetGuid(4), reader.GetGuid(5), reader.GetGuid(6),
                reader.IsDBNull(7) ? string.Empty : reader.GetString(7), reader.IsDBNull(8) ? string.Empty : reader.GetString(8), reader.GetString(9),
                reader.IsDBNull(10) ? default : reader.GetDateTime(10), reader.IsDBNull(12) ? default : reader.GetDateTime(12)));
        }
        return rows;
    }

    public Task<int> CountUnreadAlertsAsync(Guid memberId, CancellationToken cancellationToken) =>
        ScalarIntAsync("SELECT Count(*) FROM [TriggerLogsMembers] WITH (NoLock) WHERE ([MemberId] = @MemberId) AND ([Read] IS NULL)", new { MemberId = memberId },
            cancellationToken);

    public async Task PinAlertAsync(Guid alertId, bool pin, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Inline(
            pin ? "UPDATE [TriggerLogsMembers] SET [Pinned] = GetDate() WHERE [Id] = @Id" : "UPDATE [TriggerLogsMembers] SET [Pinned] = NULL WHERE [Id] = @Id",
            new { Id = alertId }, cancellationToken));

    // ---- Questions ----

    public async Task<IReadOnlyList<QuestionGroupRow>> GetOpenQuestionGroupsAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var rows = new List<QuestionGroupRow>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(
            "SELECT [Id], [SurveyId], [AssetId], [Caption], [Created] FROM [QuestionGroups] WHERE (([MemberId] = @MemberId) AND ([Closed] IS NULL)) ORDER BY [Caption]",
            new { MemberId = memberId }, cancellationToken));
        if (!reader.HasRows)
            return rows;
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(new QuestionGroupRow(reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetString(3), reader.GetDateTime(4)));
        return rows;
    }

    public async Task<IReadOnlyList<QuestionRow>> GetOpenQuestionsAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var rows = new List<QuestionRow>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(Lines(
            "SELECT [Id], [Index], [AllowUnknown], [DisableNext], [DisableCondition], [PropertyValueId], [Message], [Created]",
            "FROM [Questions] WHERE [QuestionGroupId] = @GroupId",
            "AND [Responded] IS NULL",
            "ORDER BY [Index]"), new { GroupId = groupId }, cancellationToken));
        if (!reader.HasRows)
            return rows;
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new QuestionRow(reader.GetGuid(0), reader.GetInt32(1), (bool)reader.GetValue(2), (bool)reader.GetValue(3), reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.GetGuid(5), reader.GetString(6), reader.GetDateTime(7)));
        }
        return rows;
    }

    public async Task<QuestionRefRow?> GetQuestionAsync(Guid questionId, CancellationToken cancellationToken)
    {
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(Lines(
            "SELECT [QuestionGroupId], [PropertyValueId], [Message], [MemberId], [Created]",
            "FROM [Questions] WHERE [Id] = @Id"), new { Id = questionId }, cancellationToken));
        if (!reader.HasRows || !await reader.ReadAsync(cancellationToken))
            return null;
        var row = new QuestionRefRow(reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(3));
        _ = reader.GetString(2);
        _ = reader.GetDateTime(4);
        return row;
    }

    public async Task SaveResponseAsync(Guid questionId, short response, string responseText, DateTime responded, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Inline(
            "UPDATE [Questions] SET [Response] = @Response, [ResponseText] = @ResponseText, [Responded] = @Responded WHERE [Id] = @Id",
            new { Id = questionId, Response = response, ResponseText = responseText, Responded = responded }, cancellationToken));

    public Task<int> CountOpenQuestionsAsync(Guid groupId, CancellationToken cancellationToken) =>
        ScalarIntAsync("SELECT COUNT(*) FROM [Questions] WHERE [QuestionGroupId] = @Id AND [Responded] IS NULL", new { Id = groupId }, cancellationToken);

    public async Task CloseQuestionGroupAsync(Guid groupId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Inline("UPDATE [QuestionGroups] SET [Closed] = GetDate() WHERE [Id] = @Id", new { Id = groupId },
            cancellationToken));
}
