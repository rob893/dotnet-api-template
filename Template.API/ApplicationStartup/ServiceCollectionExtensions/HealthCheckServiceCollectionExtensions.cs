using System;
using Microsoft.Extensions.DependencyInjection;

namespace Template.API.ApplicationStartup.ServiceCollectionExtensions;

public static class HealthCheckServiceCollectionExtensions
{
    public static IServiceCollection AddHealthCheckServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));

        services.AddHealthChecks();

        return services;
    }
}
