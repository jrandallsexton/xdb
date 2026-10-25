using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Serilog;

using Xdb.Api.Application.GeneratedObjects.Commands.RegenerateAll;
using Xdb.Api.Application.Setup.Commands.InitializeDatabase;
using Xdb.Api.DependencyInjection;
using Xdb.Api.Application.Metadata.Queries.Lookups;
using Xdb.Api.Infrastructure.Data;
using Xdb.Core.LegacyWire;

namespace Xdb.Api;

public partial class Program
{
    /// <summary>
    /// Integration tests boot the app under this environment name via WebApplicationFactory.
    /// Startup will skip external dependencies (database, migrations) when it is active.
    /// </summary>
    public const string TestingEnvironmentName = "Testing";

    /// <summary>
    /// First argument that makes the process apply migrations, rebuild every generated object and exit instead of
    /// serving requests (e.g. <c>dotnet run -- regenerate-all</c>).
    /// </summary>
    public const string RegenerateAllVerb = "regenerate-all";

    public static async Task<int> Main(string[] args)
    {
        // Before anything reads the local time zone (logging included): the legacy wire writes US Eastern dates.
        LegacyTimeZone.Apply();

        var regenerateAll = args.Length > 0 && args[0] == RegenerateAllVerb;
        if (regenerateAll)
            args = args[1..];

        var importSportDeets = args.Length > 0 && args[0] == ImportSportDeetsVerb;
        var resetBeforeImport = importSportDeets && args.Contains(ResetOption);
        if (importSportDeets)
            args = args[1..].Where(a => a != ResetOption).ToArray();

        var builder = WebApplication.CreateBuilder(args);
        // Only the import reads this store (the Producer connection string); it never holds the app's own settings.
        if (importSportDeets)
            builder.Configuration.AddUserSecrets(SportDeetsSecretsId);
        // The original host (IIS) wrote response header values as UTF-8 bytes (e.g. a file name with "é" in
        // Content-disposition); Kestrel would refuse a non-ASCII value.
        builder.WebHost.ConfigureKestrel(kestrel => kestrel.ResponseHeaderEncodingSelector = _ => Encoding.UTF8);
        var isTestingEnv = builder.Environment.IsEnvironment(TestingEnvironmentName);

        builder.Host.UseSerilog((context, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console());

        var services = builder.Services;
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddHealthChecks();
        services.AddApplicationServices();

        if (!isTestingEnv)
        {
            services.AddDataPersistence(builder.Configuration);
        }

        var app = builder.Build();

        if (!isTestingEnv)
        {
            // Before any startup write: migrations and seeding would otherwise reach whatever database is configured.
            if (importSportDeets)
                await RequireSportDeetsDatabaseAsync(app.Services);
            if (resetBeforeImport)
                await ResetSportDeetsDatabaseAsync(app.Services);

            await app.Services.ApplyMigrationsAsync();

            if (regenerateAll)
                return await RegenerateAllAsync(app.Services);

            await InitializeDatabaseAsync(app.Services);

            if (importSportDeets)
                return await ImportSportDeets(app.Services);
        }

        app.UseSerilogRequestLogging();

        // The original web UI (src/ui; copied into this repository and sanitized at M9) is served at the site root,
        // on the same origin as the legacy service it calls. LegacyUi:Root overrides the folder (e.g. in a container).
        var uiRoot = Path.GetFullPath(app.Configuration["LegacyUi:Root"] ?? Path.Combine(app.Environment.ContentRootPath, "..", "ui"));
        if (Directory.Exists(uiRoot))
        {
            var uiFiles = new PhysicalFileProvider(uiRoot);
            app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = uiFiles, DefaultFileNames = ["default.html"] });
            app.UseStaticFiles(new StaticFileOptions { FileProvider = uiFiles });
        }

        if (!app.Environment.IsProduction())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // A known operation called with the wrong verb: the original host's 405 page instead of an empty body. A path
        // under the service that is no operation: its 404 page (other paths, such as health, keep their behaviour).
        app.Use(async (context, next) =>
        {
            await next();
            if (context.Response.HasStarted)
                return;
            var helpUrl = context.RequestServices.GetRequiredService<IOptions<LegacyEnvironmentOptions>>().Value.ServiceHelpUrl;
            string? page = null;
            if (context.Response.StatusCode == StatusCodes.Status405MethodNotAllowed)
                page = LegacyWire.MethodNotAllowedPage(helpUrl);
            else if (context.Response.StatusCode == StatusCodes.Status404NotFound && context.GetEndpoint() is null
                     && context.Request.Path.StartsWithSegments("/" + LegacyWire.BaseRoute, StringComparison.OrdinalIgnoreCase))
                page = LegacyWire.NotFoundPage(helpUrl);
            if (page is null)
                return;
            context.Response.ContentType = "text/html";
            context.Response.Headers.CacheControl = "private";
            await context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes(page));
        });

        app.MapControllers();
        app.MapHealthChecks("/health");

        await app.RunAsync();
        return 0;
    }

    private static async Task InitializeDatabaseAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<IInitializeDatabaseCommandHandler>();
        await handler.ExecuteAsync(new InitializeDatabaseCommand());
    }

    /// <returns>The process exit code: 1 when any object failed to generate.</returns>
    private static async Task<int> RegenerateAllAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<IRegenerateAllCommandHandler>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var result = (await handler.ExecuteAsync(new RegenerateAllCommand())).Value;
        foreach (var failure in result.Failures)
            logger.LogError("Generation failed for {Kind} {Id}: {Error}", failure.Kind, failure.Id, failure.Error);
        return result.Failures.Any() ? 1 : 0;
    }
}
