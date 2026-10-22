using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

using Xdb.Api.Application.Reports.Queries.GetReportClient;
using Xdb.Api.Application.Reports.Queries.GetReportData;
using Xdb.Api.Application.Reports.Queries.GetReportExecutionStats;
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

        // Reports
        services.AddScoped<IGetReportClientQueryHandler, GetReportClientQueryHandler>();
        services.AddScoped<IGetReportDataQueryHandler, GetReportDataQueryHandler>();
        services.AddScoped<IGetReportExecutionStatsQueryHandler, GetReportExecutionStatsQueryHandler>();

        services.AddValidatorsFromAssemblyContaining<Program>();

        return services;
    }
}
