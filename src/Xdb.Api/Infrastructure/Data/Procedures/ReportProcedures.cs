using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dapper;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Xdb.Api.Infrastructure.Data.Procedures;

public sealed class ReportProcedures(XdbDataContext db, ILogger<ReportProcedures> logger) : IReportProcedures
{
    /// <summary>The original's command timeout for every report query, in seconds.</summary>
    private const int CommandTimeout = 3000;

    public async Task<(ReportRow? Report, IReadOnlyList<ReportPropertyRow> Properties)> GetReportAsync(Guid reportId, CancellationToken cancellationToken)
    {
        var connection = await OpenAsync(cancellationToken);
        await using var results = await connection.QueryMultipleAsync(Procedure("spr_Report_Get", new { Id = reportId }, cancellationToken));
        var report = (await results.ReadAsync<ReportRow>()).FirstOrDefault();
        var properties = (await results.ReadAsync<ReportPropertyRow>()).ToList();
        return (report, properties);
    }

    public async Task<IReadOnlyList<ReportFilterRow>> GetReportFiltersAsync(Guid reportId, CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<ReportFilterRow>(Procedure("spr_ReportFilters_Get", new { ReportId = reportId }, cancellationToken))).ToList();

    public async Task<IReadOnlyList<PropertyRow>> GetPropertiesAsync(string commaSeparatedIds, CancellationToken cancellationToken) =>
        (await (await OpenAsync(cancellationToken)).QueryAsync<PropertyRow>(Procedure("spr_Properties_GetByListOfPropertyIds",
            new { PropertyIds = new DbString { Value = commaSeparatedIds, IsAnsi = true, Length = -1 } }, cancellationToken))).ToList();

    public Task<string> GetPropertyDisplayValueAsync(Guid propertyId, CancellationToken cancellationToken) =>
        ScalarStringAsync("select IsNull([DisplayValue], [Name]) FROM [Properties] WITH (NoLock) WHERE [Id] = @Id", new { Id = propertyId }, CommandType.Text, cancellationToken);

    public Task<string> GetReportingLabelAsync(Guid? assetTypeId, bool forInstances, CancellationToken cancellationToken) =>
        ScalarStringAsync(
            forInstances
                ? "SELECT [InstanceLabel] FROM [AssetTypes] WITH (NoLock) WHERE [Id] = @Id AND [Deleted] IS NULL"
                : "SELECT [DefinitionLabel] FROM [AssetTypes] WITH (NoLock) WHERE [Id] = @Id AND [Deleted] IS NULL",
            new { Id = assetTypeId }, CommandType.Text, cancellationToken);

    public async Task<string?> GetDatePartDisplayValueAsync(Guid datePartId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).QueryFirstOrDefaultAsync<string?>(
            Text("SELECT [DisplayValue] FROM [DateParts] WHERE [Id] = @Id", new { Id = datePartId }, cancellationToken));

    public Task<string> GetAssetNameAsync(Guid assetId, CancellationToken cancellationToken) =>
        ScalarStringAsync("spr_Asset_Get_Name", new { Id = assetId }, CommandType.StoredProcedure, cancellationToken);

    public async Task<PickListValueRow?> GetPickListValueAsync(Guid pickListValueId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).QueryFirstOrDefaultAsync<PickListValueRow>(Procedure("spr_PickListValue_Get", new { Id = pickListValueId }, cancellationToken));

    public async Task<MemberNameRow?> GetMemberAsync(Guid memberId, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).QueryFirstOrDefaultAsync<MemberNameRow>(Procedure("spr_Member_Get", new { Id = memberId }, cancellationToken));

    public Task<string> GetReportStoredProcAsync(Guid reportId, CancellationToken cancellationToken) =>
        ScalarStringAsync("SELECT [StoredProc] FROM [Reports] WHERE [Id] = @Id", new { Id = reportId }, CommandType.Text, cancellationToken);

    public Task<string> GetReportOrderByColumnAsync(Guid reportId, CancellationToken cancellationToken) =>
        ScalarStringAsync("spr_Report_GetOrderByColumnName", new { Id = reportId }, CommandType.StoredProcedure, cancellationToken);

    public async Task<DataTable> QueryReportViewAsync(Guid reportId, string orderByColumn, CancellationToken cancellationToken)
    {
        // Built exactly as the original built it: no escaping of the column name (a ']' fails in SQL, as it did).
        var view = "vwRpt_" + reportId.ToString("D");
        var sql = orderByColumn.Length == 0 ? $"SELECT * FROM [{view}]" : $"SELECT * FROM [{view}] ORDER BY [{orderByColumn}]";
        return await LoadAsync(sql, CommandType.Text, cancellationToken);
    }

    public async Task<DataTable?> ExecuteReportProcedureAsync(string procedureName, CancellationToken cancellationToken)
    {
        try
        {
            return await LoadAsync(procedureName, CommandType.StoredProcedure, cancellationToken);
        }
        catch (DbException ex)
        {
            logger.LogError(ex, "Report procedure {Procedure} failed", procedureName);
            return null;
        }
    }

    public async Task SaveReportLogAsync(Guid logId, Guid reportId, Guid memberId, int recordsReturned, CancellationToken cancellationToken)
    {
        // @Completed and @SqlText are not sent: the procedure's defaults (NULL) apply, as with the original.
        var connection = await OpenAsync(cancellationToken);
        await connection.ExecuteAsync(Procedure("spSys_ReportLog_Save",
            new { Id = logId, ReportId = reportId, MemberId = memberId, RecordsReturned = recordsReturned, WasCached = false }, cancellationToken));
    }

    public async Task<ReportExecutionStatsRow?> GetExecutionStatsAsync(Guid reportId, Guid userId, CancellationToken cancellationToken)
    {
        // Two of the four columns are unnamed, so they are read by position.
        var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "spSys_Report_Execution_Stats";
        command.CommandType = CommandType.StoredProcedure;
        command.CommandTimeout = CommandTimeout;
        AddParameter(command, "@ReportId", reportId);
        AddParameter(command, "@UserId", userId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new ReportExecutionStatsRow(
            reader.IsDBNull(0) ? null : reader.GetInt32(0),
            reader.IsDBNull(1) ? null : reader.GetBoolean(1),
            reader.IsDBNull(2) ? null : reader.GetFieldValue<TimeSpan>(2),
            reader.IsDBNull(3) ? null : reader.GetFieldValue<TimeSpan>(3));
    }

    private async Task<DataTable> LoadAsync(string commandText, CommandType commandType, CancellationToken cancellationToken)
    {
        var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.CommandType = commandType;
        command.CommandTimeout = CommandTimeout;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    private async Task<string> ScalarStringAsync(string commandText, object parameters, CommandType commandType, CancellationToken cancellationToken)
    {
        var connection = await OpenAsync(cancellationToken);
        var value = await connection.ExecuteScalarAsync(new CommandDefinition(commandText, parameters, commandType: commandType,
            commandTimeout: CommandTimeout, cancellationToken: cancellationToken));
        return value is null or DBNull ? string.Empty : value.ToString()!.Trim();
    }

    private async Task<DbConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await db.Database.OpenConnectionAsync(cancellationToken);
        return connection;
    }

    private static CommandDefinition Procedure(string name, object parameters, CancellationToken cancellationToken) =>
        new(name, parameters, commandType: CommandType.StoredProcedure, commandTimeout: CommandTimeout, cancellationToken: cancellationToken);

    private static CommandDefinition Text(string sql, object parameters, CancellationToken cancellationToken) =>
        new(sql, parameters, commandType: CommandType.Text, commandTimeout: CommandTimeout, cancellationToken: cancellationToken);

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
