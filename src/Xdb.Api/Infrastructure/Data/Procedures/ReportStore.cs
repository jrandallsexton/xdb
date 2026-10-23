using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

using Dapper;

using Xdb.Api.Application.Reports.Legacy;
using Xdb.Api.Application.Views.Legacy;

namespace Xdb.Api.Infrastructure.Data.Procedures;

public sealed class ReportStore(XdbDataContext db) : LegacyGatewayBase(db), IReportStore
{
    private const int MaxOperatorId = 6;

    public async Task<ReportContract> LoadAsync(Guid reportId, CancellationToken cancellationToken)
    {
        ReportContract? report = null;
        await using (var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Procedure("spr_Report_Get", new { Id = reportId }, cancellationToken)))
        {
            if (reader.HasRows)
            {
                await reader.ReadAsync(cancellationToken);
                report = new ReportContract
                {
                    Id = reportId,
                    ReportName = reader.GetString(0),
                    AssetTypeId = reader.IsDBNull(1) ? Guid.Empty : reader.GetGuid(1),
                    FilterLogic = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    StoredProc = reader.IsDBNull(3) ? null : reader.GetString(3),
                    IsInstance = (bool)reader.GetValue(4),
                    ReportingLabel = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Created = reader.GetDateTime(6),
                    CreatedBy = reader.GetGuid(7),
                    IsNew = false
                };
                await reader.NextResultAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var property = new ReportPropertyContract { Id = reader.GetGuid(0), PropertyId = reader.GetGuid(1) };
                    property.SubPropertyId = reader.IsDBNull(2) ? null : reader.GetGuid(2);
                    property.Index = reader.GetInt32(3);
                    property.IsGrouping = (bool)reader.GetValue(4);
                    if (!reader.IsDBNull(5))
                        property.CustomReportFieldType = Known(reader.GetInt32(5), 0, 5);
                    if (!reader.IsDBNull(6))
                        property.TimeElapsedType = Known(reader.GetInt32(6), 0, 2);
                    if (!reader.IsDBNull(7))
                        property.TimeElapsedFormat = Known(reader.GetInt32(7), 0, 2);
                    property.TimeElapsedSpecificDate = reader.IsDBNull(8) ? null : reader.GetDateTime(8);
                    property.Label = reader.IsDBNull(9) ? null : reader.GetString(9);
                    property.PropertyName = reader.IsDBNull(10) ? null : reader.GetString(10);
                    property.SubPropertyName = reader.IsDBNull(11) ? null : reader.GetString(11);
                    property.Logic = reader.IsDBNull(12) ? null : reader.GetString(12);
                    report.ReportProperties!.Add(property);
                    if (property.IsGrouping && !report.GroupColumns!.Contains(property.PropertyId))
                        report.GroupColumns.Add(property.PropertyId);
                }
            }
        }

        // ReportDal.Get assigns the filters to the report unconditionally: a missing report fails here.
        var loaded = report ?? throw new NullReferenceException("Report not found.");
        await using (var filters = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Procedure("spr_ReportFilters_Get", new { ReportId = reportId }, cancellationToken)))
        {
            while (await filters.ReadAsync(cancellationToken))
            {
                var id = filters.GetGuid(0);
                var propertyId = filters.GetGuid(1);
                var property = filters.GetString(2);
                var operatorId = filters.GetInt32(3);
                if (operatorId is < 0 or > MaxOperatorId)
                    throw new InvalidOperationException("Invalid value for EFilterOperator");
                loaded.Filters!.Add(new FilterContract
                {
                    Id = id, PropertyId = propertyId, Property = property, OperatorId = operatorId, Operator = filters.GetString(4), Value = filters.GetString(5),
                    Order = filters.GetInt32(6), DisplayValue = null
                });
            }
        }
        return loaded;
    }

    public Task<Guid> GetAssetTypeIdAsync(Guid reportId, CancellationToken cancellationToken) =>
        ScalarGuidAsync("SELECT [AssetTypeId] FROM [Reports] WHERE [Id] = @Id", new { Id = reportId }, cancellationToken);

    public Task<Guid> GetCreatedByAsync(Guid reportId, CancellationToken cancellationToken) =>
        ScalarGuidAsync("SELECT [CreatedBy] FROM [Reports] WHERE [Id] = @Id", new { Id = reportId }, cancellationToken);

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetReportsDictionaryAsync(Guid? memberId, CancellationToken cancellationToken) =>
        GuidPairsAsync(memberId is { } member
                ? Procedure("spr_ReportList_GetByMemberId", new { MemberId = member }, cancellationToken)
                : Procedure("spr_Reports_GetDictionary", null, cancellationToken),
            skipNulls: false, cancellationToken, unique: true);

    public async Task<IReadOnlyList<KeyValuePair<string, string>>> GetCurrencyDisplaysAsync(CancellationToken cancellationToken)
    {
        var result = new List<KeyValuePair<string, string>>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(
            "SELECT [Value], [Display] FROM [CurrencySymbols] WITH (NoLock) ORDER BY [Value]", null, cancellationToken));
        if (!reader.HasRows)
            return result;
        while (await reader.ReadAsync(cancellationToken))
        {
            var value = reader.GetString(0);
            if (!seen.Add(value))
                throw new ArgumentException($"An item with the same key has already been added: {value}");
            result.Add(new KeyValuePair<string, string>(value, reader.GetString(1)));
        }
        return result;
    }

    public async Task<IReadOnlyList<ReportCacheRow>> GetCacheAsync(CancellationToken cancellationToken)
    {
        var rows = new List<ReportCacheRow>();
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Procedure("spr_ReportCache_View", null, cancellationToken));
        if (!reader.HasRows)
            return rows;
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(new ReportCacheRow(reader.GetGuid(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetDateTime(2), reader.IsDBNull(3) ? null : reader.GetString(3)));
        return rows;
    }

    public async Task ClearCacheAsync(Guid reportId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Text("DELETE FROM [ReportCache] WHERE [ReportId] = @ReportId", new { ReportId = reportId }, cancellationToken));

    public async Task<int> CountBookmarksAsync(Guid memberId, Guid reportId, CancellationToken cancellationToken) =>
        (int)(await (await OpenAsync(cancellationToken)).ExecuteScalarAsync(new CommandDefinition(
            $"SELECT COUNT(*) FROM [MemberBookmarks] WITH (NoLock) WHERE {NewLine}[MemberId] = '{memberId:D}' AND {NewLine}[ReportId] = '{reportId:D}'{NewLine}",
            commandType: CommandType.Text, cancellationToken: cancellationToken)))!;

    public async Task AddBookmarkAsync(Guid memberId, Guid reportId, bool showOnHome, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(new CommandDefinition(
            $"INSERT INTO [MemberBookmarks] ([Id], [MemberId], [ReportId], [ShowOnHome]) VALUES ({NewLine}NEWID(), '{memberId:D}', '{reportId:D}', {(showOnHome ? 1 : 0)}){NewLine}",
            commandType: CommandType.Text, commandTimeout: 300, cancellationToken: cancellationToken));

    public async Task RemoveBookmarkAsync(Guid memberId, Guid reportId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Procedure("spr_Bookmark_Delete", new { UserId = memberId, ReportId = reportId }, cancellationToken));

    public async Task SaveReportAsync(ReportContract report, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Id", report.Id);
        parameters.Add("ReportName", report.ReportName);
        parameters.Add("AssetTypeId", report.AssetTypeId);
        if (!string.IsNullOrEmpty(report.FilterLogic))
            parameters.Add("FilterExpression", report.FilterLogic);
        if (!string.IsNullOrEmpty(report.StoredProc))
            parameters.Add("StoredProc", report.StoredProc);
        parameters.Add("IsInstance", report.IsInstance);
        parameters.Add("Created", report.Created, DbType.DateTime);
        parameters.Add("CreatedBy", report.CreatedBy);
        if (report.Deleted is { } deleted)
            parameters.Add("Deleted", deleted, DbType.DateTime);
        if (report.DeletedBy is { } deletedBy)
            parameters.Add("DeletedBy", deletedBy);
        if (!string.IsNullOrEmpty(report.FilterString))
            parameters.Add("FilterString", report.FilterString);
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Procedure("spr_Report_Save", parameters, cancellationToken));
    }

    public async Task DeletePropertiesAsync(Guid reportId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Text("DELETE FROM [ReportsProperties] WHERE [ReportId] = @ReportId" + NewLine, new { ReportId = reportId }, cancellationToken));

    public async Task InsertPropertyAsync(Guid reportId, ReportPropertyContract property, bool isGrouping, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Id", property.Id);
        parameters.Add("ReportId", reportId);
        if (property.PropertyId != Guid.Empty)
            parameters.Add("PropertyId", property.PropertyId);
        if (property.SubPropertyId is { } sub && sub != Guid.Empty)
            parameters.Add("SubPropertyId", sub);
        parameters.Add("Order", property.Index);
        parameters.Add("IsGrouping", isGrouping);
        parameters.Add("CustomReportFieldTypeId", property.CustomReportFieldType);
        parameters.Add("TimeElapsedTypeId", property.TimeElapsedType);
        parameters.Add("TimeElapsedFormatId", property.TimeElapsedFormat);
        if (property.TimeElapsedSpecificDate is { } date)
            parameters.Add("TimeElapsedSpecificDate", date, DbType.DateTime);
        if (!string.IsNullOrEmpty(property.Label))
            parameters.Add("Label", property.Label);
        if (!string.IsNullOrEmpty(property.Logic))
            parameters.Add("Logic", property.Logic);
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Procedure("spr_ReportProperty_Insert", parameters, cancellationToken));
    }

    public async Task InsertPropertyPropertyAsync(Guid reportPropertyId, Guid propertyId, int order, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Text(
            $"INSERT INTO [ReportsPropertiesProperties] ([Id], [ReportsPropertiesId], [PropertyId], [Order]) VALUES (NewId(), '{reportPropertyId:D}', '{propertyId:D}', {order})",
            null, cancellationToken));

    public async Task DeleteFiltersAsync(Guid reportId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Text("DELETE FROM [ReportsFilters] WHERE [ReportId] = @ReportId", new { ReportId = reportId }, cancellationToken));

    public async Task InsertFilterAsync(Guid reportId, FilterContract filter, CancellationToken cancellationToken)
    {
        // A null value was "not supplied" for the inline statement (SQL 8178): the statement fails and inserts nothing.
        if (filter.Value is null)
            throw new InvalidOperationException("The parameterized query expects the parameter '@Value', which was not supplied.");
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Text(
            "INSERT INTO [ReportsFilters] ([Id], [ReportId], [PropertyId], [OperatorId], [Value], [Order])" + NewLine
            + "VALUES (@Id, @ReportId, @PropertyId, @OperatorId, @Value, @Order)" + NewLine,
            new { Id = Guid.NewGuid(), ReportId = reportId, filter.PropertyId, filter.OperatorId, filter.Value, filter.Order }, cancellationToken));
    }

    public async Task KillAsync(Guid reportId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Procedure("spr_Report_Kill", new { Id = reportId }, cancellationToken));

    public async Task DeleteAsync(Guid reportId, Guid deletedBy, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Procedure("spr_Report_Delete", new { Id = reportId, DeletedBy = deletedBy }, cancellationToken));

    /// <summary>ExecuteInLineSql: CommandType.Text with the original's 3000 s timeout.</summary>
    private static CommandDefinition Text(string sql, object? parameters, CancellationToken cancellationToken) =>
        new(sql, parameters, commandType: CommandType.Text, commandTimeout: ProcedureTimeout, cancellationToken: cancellationToken);

    /// <summary>EnumerationOps.*FromValue: an undefined value maps to the enum's first member (0).</summary>
    private static int Known(int value, int min, int max) => value >= min && value <= max ? value : 0;
}
