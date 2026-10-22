using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Dapper;

namespace Xdb.Api.Infrastructure.Data.Procedures;

public sealed class DashboardProcedures(XdbDataContext db) : LegacyGatewayBase(db), IDashboardProcedures
{
    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetDashboardsAsync(Guid memberId, CancellationToken cancellationToken) =>
        LegacyDictionaryAsync(Inline($"SELECT [Id], [Caption] FROM [Dashboards] WHERE [MemberId] = '{memberId:D}' AND [Deleted] IS NULL ORDER BY [Caption]",
            null, cancellationToken), firstWins: true, cancellationToken);

    public async Task<DashboardRow?> GetDashboardAsync(Guid dashboardId, CancellationToken cancellationToken)
    {
        string? caption;
        int index;
        await using (var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(
            "SELECT [Caption], [Index], [Created] FROM [Dashboards] WHERE [Id] = @Id", new { Id = dashboardId }, cancellationToken)))
        {
            if (!reader.HasRows)
                return null;
            await reader.ReadAsync(cancellationToken);
            caption = reader.IsDBNull(0) ? null : reader.GetString(0);
            index = reader.GetInt32(1);
        }
        var items = new List<DashboardItemRow>();
        await using (var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(
            "SELECT [Id], [Index], [ItemTypeId], [ChartId], [Title], [Subtitle], [Created] FROM [DashboardItems] WHERE [DashboardId] = @DashId AND [Deleted] IS NULL ORDER BY [Index]",
            new { DashId = dashboardId }, cancellationToken)))
        {
            while (await reader.ReadAsync(cancellationToken))
                items.Add(new DashboardItemRow(reader.GetGuid(0), reader.GetInt32(1), Convert.ToInt32(reader.GetValue(2)), reader.GetGuid(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetString(5)));
        }
        return new DashboardRow(dashboardId, caption, index, items);
    }

    public async Task SaveDashboardAsync(DashboardRow dashboard, Guid memberId, CancellationToken cancellationToken)
    {
        var connection = await OpenAsync(cancellationToken);
        if (await CountAsync($"SELECT COUNT(*) FROM [Dashboards] WHERE [Id] = '{dashboard.Id:D}'", cancellationToken) > 0)
            await connection.ExecuteAsync(Inline("UPDATE [Dashboards] SET [Caption] = @Caption, [Index] = @Index WHERE [Id] = @Id",
                Required(new { Id = dashboard.Id, Caption = dashboard.Caption, Index = dashboard.Index }, dashboard.Caption), cancellationToken));
        else
            await connection.ExecuteAsync(Inline("INSERT INTO [Dashboards] ([Id], [Caption], [Index], [MemberId]) VALUES (@Id, @Caption, @Index, @MemberId)",
                Required(new { Id = dashboard.Id, Caption = dashboard.Caption, Index = dashboard.Index, MemberId = memberId }, dashboard.Caption), cancellationToken));

        foreach (var item in dashboard.Items)
        {
            if (await CountAsync($"SELECT COUNT(*) FROM [DashboardItems] WHERE [Id] = '{item.Id:D}'", cancellationToken) > 0)
            {
                await connection.ExecuteAsync(Inline("UPDATE [DashboardItems] SET [Index] = @Index WHERE [Id] = @Id", new { Id = item.Id, Index = item.Index }, cancellationToken));
                continue;
            }
            // Title and subtitle are never copied by Dashboard_Save, so only the plain insert form is reachable.
            await connection.ExecuteAsync(Inline(
                "INSERT INTO [DashboardItems] ([Id], [DashboardId], [Index], [ItemTypeId], [ChartId]) VALUES (@Id, @DashId, @Index, @ItemTypeId, @ChartId)",
                new { Id = item.Id, DashId = dashboard.Id, Index = item.Index, ItemTypeId = item.ItemTypeId, ChartId = item.ChartId }, cancellationToken));
        }
    }

    public async Task<MemberSaveRow?> GetMemberAsync(Guid memberId, CancellationToken cancellationToken)
    {
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Procedure("spr_Member_Get", new { Id = memberId }, cancellationToken));
        if (!await reader.ReadAsync(cancellationToken))
            return null;
        return new MemberSaveRow(memberId, reader.GetString(0), reader.IsDBNull(1) ? string.Empty : reader.GetString(1), reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            reader.GetDateTime(4), reader.GetGuid(5), reader.IsDBNull(8) ? null : reader.GetGuid(8));
    }

    public Task<bool> IsLiveMemberAsync(Guid memberId, CancellationToken cancellationToken) =>
        CountIsOneAsync(Procedure("spr_MemberId_IsValid", new { Id = memberId }, cancellationToken));

    public async Task SaveMemberAsync(MemberSaveRow member, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Procedure("spr_Member_Save", new
        {
            member.Id,
            UserId = new DbString { Value = member.UserId, IsAnsi = true, Length = 50 },
            LName = new DbString { Value = member.LName, IsAnsi = true, Length = 50 },
            FName = new DbString { Value = member.FName, IsAnsi = true, Length = 50 },
            MName = (string?)null,
            member.Created,
            member.CreatedBy,
            member.DeletedBy
        }, cancellationToken));

    public async Task MarkMemberUpdatedAsync(Guid memberId, Guid byMemberId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Inline(
            "UPDATE [Members] SET [LastModified] = GetDate(), [LastModifiedby] = @ById WHERE [Id] = @Id", new { Id = memberId, ById = byMemberId }, cancellationToken));

    public async Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetUnmappedAsync(CancellationToken cancellationToken)
    {
        var result = new List<KeyValuePair<Guid, string>>();
        var seen = new HashSet<Guid>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(
            "SELECT [Id], [Key] FROM [Mappings] WHERE [Value] IS NULL ORDER BY [Key]", null, cancellationToken));
        if (!reader.HasRows)
            return result;
        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.IsDBNull(0) || reader.IsDBNull(1))
                continue;
            var id = reader.GetGuid(0);
            if (!seen.Add(id))
                throw new ArgumentException($"An item with the same key has already been added: {id:D}");
            result.Add(new KeyValuePair<Guid, string>(id, reader.GetString(1).Trim()));
        }
        return result;
    }

    public async Task SetMappingAsync(Guid id, Guid value, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Inline("UPDATE [Mappings] SET [Value] = @Val WHERE [Id] = @Id", new { Val = value, Id = id }, cancellationToken));

    /// <summary>SqlBaseDal.ExecuteScalar(sql): (int)ExecuteScalar().</summary>
    private async Task<int> CountAsync(string sql, CancellationToken cancellationToken) =>
        (int)(await (await OpenAsync(cancellationToken)).ExecuteScalarAsync(Inline(sql, null, cancellationToken)))!;

    /// <summary>A null caption was a parameter "not supplied" for the inline statement (SQL 8178).</summary>
    private static object Required(object parameters, string? caption) =>
        caption is null ? throw new InvalidOperationException("The parameterized query expects the parameter '@Caption', which was not supplied.") : parameters;
}
