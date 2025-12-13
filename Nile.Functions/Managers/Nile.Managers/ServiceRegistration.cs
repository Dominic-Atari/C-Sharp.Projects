using Microsoft.Extensions.DependencyInjection;
using Nile.Managers.Admin;
using Nile.Managers.Engagement;
using Nile.Managers.Proxys;

namespace Nile.Managers;

public static class ServiceRegistration
{
    public static IServiceCollection AddManagersServices(this IServiceCollection services)
    {
        // Only registrations required for user account creation flows
        services.AddScoped<IAuthorizer, ProxyAuthorizer>();
        services.AddScoped<IConverter, ProxyConverter>();
        services.AddScoped<IValidator, ProxyValidator>();

        // Health check engine used by AdminManager
        services.AddSingleton<IHealthCheckEngine, HealthCheckEngine>();

        // Provide the concrete manager used by Proxy<IUserManager>
        services.AddScoped<IUserManager, AdminManager>();

        // Social engagement manager used by UserFunction_CreateProfile_V1 and related endpoints
        services.AddScoped<ISocialEngagementManager, Engagement.EngagementManager>();

        return services;
    }

    // All non-user related factory methods and registrations intentionally removed/commented per scope.
}