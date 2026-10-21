using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Xdb.Api.Infrastructure.Data;

public static class DatabaseRegistration
{
    /// <summary>
    /// The original database runs at SQL Server 2008 compatibility (100). EF Core is told so that it does
    /// not emit newer constructs (e.g. OPENJSON for Contains). Raised later, in its own migration.
    /// </summary>
    public const int CompatibilityLevel = 100;

    public const string ConnectionStringName = "Xdb";

    private static readonly TimeSpan MigrationCommandTimeout = TimeSpan.FromMinutes(10);

    public static IServiceCollection AddDataPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured (use user secrets locally).");

        services.AddDbContext<XdbDataContext>(options => options.UseSqlServer(connectionString,
            sql => sql.UseCompatibilityLevel(CompatibilityLevel)));

        return services;
    }

    /// <summary>
    /// Applies pending migrations at startup. An existing database (the original's schema, created before
    /// migrations existed) adopts the baseline: it is recorded as applied instead of being run.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<XdbDataContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<XdbDataContext>>();

        db.Database.SetCommandTimeout(MigrationCommandTimeout);

        await AdoptBaselineIfExistingAsync(db, logger, cancellationToken);

        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count == 0)
        {
            logger.LogInformation("Database is up to date.");
            return;
        }

        logger.LogInformation("Applying {Count} migration(s): {Migrations}", pending.Count, string.Join(", ", pending));
        await db.Database.MigrateAsync(cancellationToken);
    }

    private static async Task AdoptBaselineIfExistingAsync(XdbDataContext db, ILogger logger, CancellationToken cancellationToken)
    {
        if (!await db.Database.CanConnectAsync(cancellationToken))
            return;

        if ((await db.Database.GetAppliedMigrationsAsync(cancellationToken)).Any())
            return;

        var coreSchemaExists = await db.Database
            .SqlQueryRaw<int>("SELECT CASE WHEN OBJECT_ID(N'dbo.Assets', N'U') IS NULL THEN 0 ELSE 1 END AS [Value]")
            .SingleAsync(cancellationToken) == 1;
        if (!coreSchemaExists)
            return;

        var baselineId = db.Database.GetMigrations().First();
        var history = db.GetService<IHistoryRepository>();
        var efVersion = typeof(DbContext).Assembly.GetName().Version?.ToString(3) ?? "unknown";

        await db.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript(), cancellationToken);
        await db.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(baselineId, efVersion)), cancellationToken);

        logger.LogWarning("Existing database adopted: baseline {Baseline} recorded as applied without running it.", baselineId);
    }
}
