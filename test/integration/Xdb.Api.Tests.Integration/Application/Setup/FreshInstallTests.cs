using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.MsSql;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects;
using Xdb.Api.Application.GeneratedObjects.Views;
using Xdb.Api.Application.Setup.Commands.InitializeDatabase;
using Xdb.Api.DependencyInjection;
using Xdb.Api.Infrastructure.Data;

using Xunit;

namespace Xdb.Api.Tests.Integration.Application.Setup;

/// <summary>
/// A fresh install, as startup performs it: migrations (baseline and system seed) on an empty server, then
/// initialization with a configured first administrator, which builds the generated objects.
/// </summary>
public class FreshInstallFixture : IAsyncLifetime
{
    public const string FirstAdminUserId = "first.admin";

    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public ServiceProvider Services { get; private set; } = null!;

    public string ConnectionString { get; private set; } = null!;

    public InitializeDatabaseResult FirstRun { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();

        ConnectionString = new SqlConnectionStringBuilder(_sql.GetConnectionString()) { InitialCatalog = "XdbFresh" }.ConnectionString;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{DatabaseRegistration.ConnectionStringName}"] = ConnectionString,
                [$"{FirstAdminOptions.SectionName}:UserId"] = FirstAdminUserId,
                [$"{FirstAdminOptions.SectionName}:FirstName"] = "Ada",
                [$"{FirstAdminOptions.SectionName}:LastName"] = "Lovelace",
                [$"{GeneratedObjectsOptions.SectionName}:ViewImageUrl"] = "https://images.example/"
            })
            .Build();

        Services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(configuration)
            .AddApplicationServices()
            .AddDataPersistence(configuration)
            .BuildServiceProvider();

        await Services.ApplyMigrationsAsync();
        FirstRun = await InitializeAsync(Services);
    }

    public static async Task<InitializeDatabaseResult> InitializeAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        return (await scope.ServiceProvider.GetRequiredService<IInitializeDatabaseCommandHandler>().ExecuteAsync(new InitializeDatabaseCommand())).Value;
    }

    public async Task DisposeAsync()
    {
        await Services.DisposeAsync();
        await _sql.DisposeAsync();
    }
}

public class FreshInstallTests(FreshInstallFixture fixture) : IClassFixture<FreshInstallFixture>
{
    private static readonly string UserTable = GeneratedObjectNames.Table(SystemAssetTypeIds.User, isInstance: false, isRaw: false);
    private const string ReportFilterLogic = "({0})";
    private const string RoleFilterLogic = "{0}";

    private static readonly string UserRawTable = GeneratedObjectNames.Table(SystemAssetTypeIds.User, isInstance: false, isRaw: true);

    [Fact]
    public void FirstRun_CreatesTheAdminAndBuildsTheUserTypesObjects()
    {
        fixture.FirstRun.FirstAdminCreated.Should().BeTrue();
        fixture.FirstRun.Regenerated.Should().NotBeNull();
        fixture.FirstRun.Regenerated!.Failures.Should().BeEmpty();
        fixture.FirstRun.Regenerated.GenTables.Should().Be(2, "the seeded User type has no instance label");
    }

    [Fact]
    public async Task GenTables_AreLoadedWithTheSystemAndFirstAdministrators()
    {
        var names = await ListAsync($"SELECT [Name] FROM [dbo].[{UserTable}] ORDER BY [Name]");

        names.Should().Equal("Admin", FreshInstallFixture.FirstAdminUserId);
        (await ScalarAsync<int>($"SELECT COUNT(*) FROM [dbo].[{UserRawTable}]")).Should().Be(2);
        (await ScalarAsync<int>($"""
            SELECT COUNT(*) FROM sys.procedures p JOIN sys.sql_modules m ON m.object_id = p.object_id
            WHERE p.name IN ('spr_{UserTable}_Update', 'spr_{UserRawTable}_Update') AND m.uses_ansi_nulls = 1 AND m.uses_quoted_identifier = 1
            """)).Should().Be(2);
    }

    [Fact]
    public async Task FirstAdmin_IsAMemberOfTheAdminRole()
    {
        (await ScalarAsync<int>($"""
            SELECT COUNT(*) FROM [dbo].[RoleMembers] RM JOIN [dbo].[Members] M ON M.[Id] = RM.[MemberId]
            WHERE RM.[RoleId] = '{SystemRoleIds.Admin:D}' AND M.[UserId] = '{FreshInstallFixture.FirstAdminUserId}' AND RM.[Deleted] IS NULL
            """)).Should().Be(1);
    }

    [Fact]
    public async Task SecondRun_ChangesNothing()
    {
        var result = await FreshInstallFixture.InitializeAsync(fixture.Services);

        result.FirstAdminCreated.Should().BeFalse();
        result.Regenerated.Should().BeNull();
        (await ScalarAsync<int>($"SELECT COUNT(*) FROM [dbo].[Members] WHERE [UserId] = '{FreshInstallFixture.FirstAdminUserId}'")).Should().Be(1);
    }

    [Fact]
    public async Task MergeProcedure_RefreshesAnAsset()
    {
        await ExecuteAsync($"UPDATE [dbo].[Assets] SET [DisplayValue] = 'Renamed' WHERE [Id] = '{SystemMemberIds.Admin:D}'");
        await ExecuteAsync($"EXEC [dbo].[spr_{UserTable}_Update] '{SystemMemberIds.Admin:D}'");

        (await ScalarAsync<string>($"SELECT [DisplayValue] FROM [dbo].[{UserTable}] WHERE [AssetId] = '{SystemMemberIds.Admin:D}'")).Should().Be("Renamed");
    }

    [Fact]
    public async Task ReportAndRoleViews_AreBuiltOverTheGenTables()
    {
        var reportId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        await ExecuteAsync($"""
            INSERT INTO [dbo].[Reports] ([Id], [ReportName], [AssetTypeId], [IsInstance], [FilterExpression], [Created], [CreatedBy])
            VALUES ('{reportId:D}', 'Users', '{SystemAssetTypeIds.User:D}', 0, '{ReportFilterLogic}', GETDATE(), '{SystemMemberIds.Admin:D}');
            INSERT INTO [dbo].[ReportsProperties] ([Id], [ReportId], [PropertyId], [Order], [IsGrouping])
            VALUES (NEWID(), '{reportId:D}', '{SystemPropertyIds.AssetName:D}', 0, 0),
                   (NEWID(), '{reportId:D}', '{SystemPropertyIds.Description:D}', 1, 0);
            INSERT INTO [dbo].[ReportsFilters] ([Id], [ReportId], [PropertyId], [OperatorId], [Value], [Order])
            VALUES (NEWID(), '{reportId:D}', '{SystemPropertyIds.AssetName:D}', 6, 'first', 0);

            DECLARE @RoleAssetType uniqueidentifier = NEWID(), @Permission uniqueidentifier = NEWID();
            INSERT INTO [dbo].[Roles] ([Id], [Name], [CreatedBy], [IsSystem], [IsActive]) VALUES ('{roleId:D}', 'Editors', '{SystemMemberIds.Admin:D}', 0, 1);
            INSERT INTO [dbo].[RoleAssetTypes] ([Id], [RoleId], [AssetTypeId], [IsInstance], [IsInherited])
            VALUES (@RoleAssetType, '{roleId:D}', '{SystemAssetTypeIds.User:D}', 0, 0);
            INSERT INTO [dbo].[RolePermissions] ([Id], [RoleAssetTypesId], [PermissionId], [IsIncludesFilter], [FilterLogic])
            VALUES (@Permission, @RoleAssetType, 1, 1, '{RoleFilterLogic}');
            INSERT INTO [dbo].[RoleFilters] ([Id], [RolePermissionsId], [Order], [PropertyId], [OperatorId], [Value])
            VALUES (NEWID(), @Permission, 0, '{SystemPropertyIds.AssetName:D}', 0, 'Admin');
            """);

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            (await scope.ServiceProvider.GetRequiredService<IReportViewGenerator>().GenerateAsync(reportId)).Should().BeTrue();
            (await scope.ServiceProvider.GetRequiredService<IRoleViewGenerator>().GenerateAsync(roleId)).Should().Be(1);
        }

        (await ListAsync($"SELECT [User] FROM [dbo].[{GeneratedObjectNames.ReportView(reportId)}]")).Should().Equal(FreshInstallFixture.FirstAdminUserId);
        (await ListAsync($"SELECT [User] FROM [dbo].[{GeneratedObjectNames.RoleView(roleId, SystemAssetTypeIds.User, 1)}]")).Should().Equal("Admin");
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var connection = new SqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<T> ScalarAsync<T>(string sql)
    {
        await using var connection = new SqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private async Task<List<string>> ListAsync(string sql)
    {
        await using var connection = new SqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<string>();
        while (await reader.ReadAsync())
            values.Add(reader.GetString(0));
        return values;
    }
}
