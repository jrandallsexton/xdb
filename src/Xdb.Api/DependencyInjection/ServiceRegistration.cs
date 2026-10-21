using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

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

        services.AddValidatorsFromAssemblyContaining<Program>();

        return services;
    }
}
