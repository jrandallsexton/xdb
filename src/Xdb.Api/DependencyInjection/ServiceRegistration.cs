using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

using Xdb.Api.Application.GeneratedObjects;
using Xdb.Api.Application.GeneratedObjects.Commands.RegenerateAll;
using Xdb.Api.Application.GeneratedObjects.GenTables;
using Xdb.Api.Application.GeneratedObjects.Views;
using Xdb.Api.Application.Reports.Queries.GetReportClient;
using Xdb.Api.Application.Reports.Queries.GetReportData;
using Xdb.Api.Application.Reports.Queries.GetReportExecutionStats;
using Xdb.Api.Application.Setup.Commands.InitializeDatabase;
using Xdb.Api.Infrastructure.Background;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.DependencyInjection;

public static class ServiceRegistration
{
    /// <summary>
    /// Handlers are registered explicitly, one line each, grouped by area.
    /// Validators are discovered by assembly scan.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddSingleton<BackgroundWorkQueue>();
        services.AddSingleton<IBackgroundWorkQueue>(sp => sp.GetRequiredService<BackgroundWorkQueue>());
        services.AddHostedService<BackgroundWorkService>();

        services.AddScoped<IReportProcedures, ReportProcedures>();
        services.AddScoped<IGeneratedObjectProcedures, GeneratedObjectProcedures>();

        services.AddOptions<GeneratedObjectsOptions>().BindConfiguration(GeneratedObjectsOptions.SectionName);
        services.AddOptions<FirstAdminOptions>().BindConfiguration(FirstAdminOptions.SectionName);

        // Generated objects
        services.AddScoped<IGenTableGenerator, GenTableGenerator>();
        services.AddScoped<ViewInputLoader>();
        services.AddScoped<IReportViewGenerator, ReportViewGenerator>();
        services.AddScoped<IRoleViewGenerator, RoleViewGenerator>();
        services.AddScoped<IRegenerateAllCommandHandler, RegenerateAllCommandHandler>();

        // Setup
        services.AddScoped<IInitializeDatabaseCommandHandler, InitializeDatabaseCommandHandler>();

        // Reports
        services.AddScoped<IGetReportClientQueryHandler, GetReportClientQueryHandler>();
        services.AddScoped<IGetReportDataQueryHandler, GetReportDataQueryHandler>();
        services.AddScoped<IGetReportExecutionStatsQueryHandler, GetReportExecutionStatsQueryHandler>();

        services.AddValidatorsFromAssemblyContaining<Program>();

        return services;
    }
}
