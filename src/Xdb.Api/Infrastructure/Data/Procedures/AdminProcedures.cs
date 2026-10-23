using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

using Dapper;

using Xdb.Api.Application.Admin.Legacy;

namespace Xdb.Api.Infrastructure.Data.Procedures;

public sealed class AdminProcedures(XdbDataContext db) : LegacyGatewayBase(db), IAdminProcedures
{
    public async Task<int> CountHelpSectionAsync(Guid id, CancellationToken cancellationToken)
    {
        var value = await (await OpenAsync(cancellationToken)).ExecuteScalarAsync(new CommandDefinition(
            "SELECT Count([Id]) FROM [HelpContent] WHERE [Id] = @Id", new { Id = id }, cancellationToken: cancellationToken));
        return int.TryParse(value?.ToString(), out var count) ? count : -1;
    }

    public async Task InsertHelpSectionAsync(Guid id, string? title, Guid imageId, int order, string? content, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(new CommandDefinition(Lines(
                "INSERT INTO [HelpContent] ([Id], [Title], [ImageId], [Order], [Content])",
                " VALUES (@Id, @Title, @ImageId, @Order, @Content)"),
            Supplied(("Id", id), ("Title", title), ("ImageId", imageId), ("Order", order), ("Content", content)), commandTimeout: ProcedureTimeout,
            cancellationToken: cancellationToken));

    public async Task UpdateHelpSectionAsync(Guid id, string? title, Guid imageId, int order, string? content, CancellationToken cancellationToken) =>
        await (await OpenAsync(cancellationToken)).ExecuteAsync(new CommandDefinition(Lines(
                "UPDATE [HelpContent] SET [Title] = @Title, [ImageId] = @ImageId, [Order] = @Order, [Content] = @Content",
                " WHERE [Id] = @Id"),
            Supplied(("Id", id), ("Title", title), ("ImageId", imageId), ("Order", order), ("Content", content)), commandTimeout: ProcedureTimeout,
            cancellationToken: cancellationToken));

    public async Task<ServiceModuleRow?> GetServiceModuleAsync(Guid moduleId, CancellationToken cancellationToken)
    {
        await using var reader = await (await OpenAsync(cancellationToken)).ExecuteReaderAsync(Inline(Lines(
            "SELECT [Name], [Description], [AssemblyName], [ClassName], [AssemblyVersion], [Downloaded], [Created], [LastRan], [LastCompleted]",
            "FROM [ServiceModules] WHERE [Id] = @Id"), new { Id = moduleId }, cancellationToken));
        if (!reader.HasRows || !await reader.ReadAsync(cancellationToken))
            return null;
        string? Text(int i) => reader.IsDBNull(i) ? null : reader.GetString(i);
        DateTime? Date(int i) => reader.IsDBNull(i) ? null : reader.GetDateTime(i);
        return new ServiceModuleRow(Text(0), Text(1), Text(2), Text(3), Text(4), Date(5), reader.GetDateTime(6), Date(7), Date(8));
    }

    public Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetServiceModulesDictionaryAsync(CancellationToken cancellationToken) =>
        GuidPairsAsync(Inline("SELECT [Id], [Name] FROM [ServiceModules] ORDER BY [Name]", null, cancellationToken), skipNulls: false, cancellationToken,
            unique: true);

    public async Task SaveServiceModuleAsync(ServiceModuleMetadataContract module, CancellationToken cancellationToken)
    {
        // Dates go as their wall clock whatever their Kind, as SqlParameter sent them; MinValue fails client-side as it did.
        var parameters = Supplied(
            ("Id", module.Id), ("Name", module.Name), ("Description", string.IsNullOrEmpty(module.Description) ? string.Empty : module.Description),
            ("AssemblyName", module.AssemblyName), ("AssemblyNamespace", module.AssemblyNamespace), ("ClassName", module.ClassName),
            ("AssemblyVersion", module.AssemblyVersion), ("IsActive", module.IsActive));
        foreach (var (name, value) in new[] { ("Deleted", module.Deleted), ("Created", (DateTime?)module.Created), ("LastRan", module.LastRan), ("LastCompleted", module.LastCompleted) })
        {
            if (value is not { } date)
                continue;
            if (date < new DateTime(1753, 1, 1))
                throw new System.Data.SqlTypes.SqlTypeException("SqlDateTime overflow. Must be between 1/1/1753 12:00:00 AM and 12/31/9999 11:59:59 PM.");
            parameters.Add(name, DateTime.SpecifyKind(date, DateTimeKind.Unspecified), DbType.DateTime);
        }
        await (await OpenAsync(cancellationToken)).ExecuteAsync(Procedure("spr_ServiceModule_Save", parameters, cancellationToken));
    }
}
