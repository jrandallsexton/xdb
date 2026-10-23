using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
        var regenerateAll = args.Length > 0 && args[0] == RegenerateAllVerb;
        if (regenerateAll)
            args = args[1..];

        var builder = WebApplication.CreateBuilder(args);
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
            await app.Services.ApplyMigrationsAsync();

            if (regenerateAll)
                return await RegenerateAllAsync(app.Services);

            await InitializeDatabaseAsync(app.Services);
        }

        app.UseSerilogRequestLogging();

        if (!app.Environment.IsProduction())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // A known operation called with the wrong verb: the original host's 405 page instead of an empty body.
        app.Use(async (context, next) =>
        {
            await next();
            if (context.Response.StatusCode == StatusCodes.Status405MethodNotAllowed && !context.Response.HasStarted)
            {
                var helpUrl = context.RequestServices.GetRequiredService<IOptions<LegacyEnvironmentOptions>>().Value.ServiceHelpUrl;
                context.Response.ContentType = "text/html";
                await context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes(LegacyWire.MethodNotAllowedPage(helpUrl)));
            }
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
