using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;

using Xdb.Api.DependencyInjection;

namespace Xdb.Api;

public partial class Program
{
    /// <summary>
    /// Integration tests boot the app under this environment name via WebApplicationFactory.
    /// Startup will skip external dependencies (database, migrations) when it is active.
    /// </summary>
    public const string TestingEnvironmentName = "Testing";

    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

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

        var app = builder.Build();

        app.UseSerilogRequestLogging();

        if (!app.Environment.IsProduction())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapControllers();
        app.MapHealthChecks("/health");

        await app.RunAsync();
    }
}
