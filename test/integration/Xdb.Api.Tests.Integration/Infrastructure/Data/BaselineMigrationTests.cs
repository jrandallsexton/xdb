using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.MsSql;

using Xdb.Api.Infrastructure.Data;

using Xunit;

namespace Xdb.Api.Tests.Integration.Infrastructure.Data;

/// <summary>
/// One SQL Server container per test class: starts empty, then the application's startup migration runs once.
/// </summary>
public class MigratedDatabaseFixture : IAsyncLifetime
{
    // Same major version as the local server the original runs on (SQL Server 2022).
    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public ServiceProvider Services { get; private set; } = null!;

    public string ConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();

        ConnectionString = new SqlConnectionStringBuilder(_sql.GetConnectionString()) { InitialCatalog = "XdbTest" }.ConnectionString;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [$"ConnectionStrings:{DatabaseRegistration.ConnectionStringName}"] = ConnectionString })
            .Build();

        Services = new ServiceCollection()
            .AddLogging()
            .AddDataPersistence(configuration)
            .BuildServiceProvider();

        await Services.ApplyMigrationsAsync();
    }

    public async Task DisposeAsync()
    {
        await Services.DisposeAsync();
        await _sql.DisposeAsync();
    }
}

/// <summary>
/// Applies the migrations to an empty SQL Server and checks the result against the reference schema's known shape.
/// The exhaustive check (schema compare against the reconciled original) is done offline; this guards regressions.
/// </summary>
public class BaselineMigrationTests(MigratedDatabaseFixture fixture) : IClassFixture<MigratedDatabaseFixture>
{
    private readonly string _connectionString = fixture.ConnectionString;

    [Fact]
    public async Task Migrations_CreateTheStaticSchema()
    {
        var counts = await QueryAsync("""
            SELECT type, COUNT(*) FROM sys.objects
            WHERE is_ms_shipped = 0 AND type IN ('U','P','FN','IF','TF','V') AND name <> '__EFMigrationsHistory'
            GROUP BY type
            """);

        counts.Should().BeEquivalentTo(new Dictionary<string, int>
        {
            ["U "] = 102,
            ["P "] = 309,
            ["FN"] = 35,
            ["TF"] = 3
        });
    }

    [Fact]
    public async Task Baseline_PreservesDatabaseAndModuleSettings()
    {
        var settings = await QueryAsync("""
            SELECT 'compat', compatibility_level FROM sys.databases WHERE name = DB_NAME()
            UNION ALL SELECT 'ansi_nulls_off', COUNT(*) FROM sys.sql_modules WHERE uses_ansi_nulls = 0
            UNION ALL SELECT 'quoted_identifier_off', COUNT(*) FROM sys.sql_modules WHERE uses_quoted_identifier = 0
            UNION ALL SELECT 'heaps', COUNT(*) FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id WHERE i.index_id = 0
            """);

        settings["compat"].Should().Be(DatabaseRegistration.CompatibilityLevel);
        settings["ansi_nulls_off"].Should().Be(304);
        settings["heaps"].Should().Be(4);

        var collation = await ScalarAsync<string>("SELECT CAST(DATABASEPROPERTYEX(DB_NAME(), 'Collation') AS nvarchar(128))");
        collation.Should().Be("SQL_Latin1_General_CP1_CI_AS");
    }

    [Fact]
    public async Task Baseline_SeedsSystemLookups()
    {
        var rows = await QueryAsync("""
            SELECT 'DataTypes', COUNT(*) FROM dbo.DataTypes
            UNION ALL SELECT 'SystemTypes', COUNT(*) FROM dbo.SystemTypes
            UNION ALL SELECT 'Permissions', COUNT(*) FROM dbo.Permissions
            UNION ALL SELECT 'MimeTypes', COUNT(*) FROM dbo.MimeTypes
            """);

        rows.Should().BeEquivalentTo(new Dictionary<string, int>
        {
            ["DataTypes"] = 25,
            ["SystemTypes"] = 13,
            ["Permissions"] = 11,
            ["MimeTypes"] = 648
        });
    }

    [Fact]
    public async Task Migrations_AreIdempotentOnRestart()
    {
        await fixture.Services.ApplyMigrationsAsync();

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<XdbDataContext>();
        (await db.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
        (await db.Database.GetAppliedMigrationsAsync()).Should().Equal(db.Database.GetMigrations());
    }

    private async Task<Dictionary<string, int>> QueryAsync(string sql)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var result = new Dictionary<string, int>();
        while (await reader.ReadAsync())
            result[reader.GetString(0)] = Convert.ToInt32(reader.GetValue(1));
        return result;
    }

    private async Task<T> ScalarAsync<T>(string sql)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }
}
