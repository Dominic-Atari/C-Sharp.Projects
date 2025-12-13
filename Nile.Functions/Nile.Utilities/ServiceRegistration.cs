using System.Reflection;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Nile.Utilities;

public static class ServiceRegistration
{
    // Wrapper to match the sample naming the user prefers
    public static IServiceCollection AddUtilityServices(this IServiceCollection services)
        => AddNileServices(services);

    public static IServiceCollection AddNileServices(this IServiceCollection services)
    {
        // Ambient context provider needed by proxies/managers
        services.AddSingleton<IContextFactoryUtility, ContextFactoryUtility>();

        // Register shared/public utilities available from this assembly
        // Note: IDateUtility interface is public (Nile.Utilities.AzureSdk),
        // implementation lives in Nile.Accessors (internal). We register it via reflection.

        var accessorsAssembly = GetAccessorsAssembly();
        if (accessorsAssembly == null)
        {
            // Accessors assembly not found; return as-is so callers can decide how to proceed
            return services;
        }

        // Resolve types by full name to avoid referencing internals at compile time
        var dateUtilityImpl = accessorsAssembly.GetType("Nile.Accessors.DateUtility");
        var mapperImpl = accessorsAssembly.GetType("Nile.Accessors.Mapper");
        var mapperAbstraction = accessorsAssembly.GetType("Nile.Accessors.IMapper");
        var userAccessorImpl = accessorsAssembly.GetType("Nile.Accessors.User.UserAccessor");
        var userAccessorAbstraction = accessorsAssembly.GetType("Nile.Accessors.User.IUserAccessor");
        var postAccessorImpl = accessorsAssembly.GetType("Nile.Accessors.Posts.PostAccessor");
        var postAccessorAbstraction = accessorsAssembly.GetType("Nile.Accessors.Posts.IPostAccessor");
        var accountAccessorImpl = accessorsAssembly.GetType("Nile.Accessors.Account.AccountAccessor");
        var accountAccessorAbstraction = accessorsAssembly.GetType("Nile.Accessors.Account.IAccountAccessor");
        var healthCheckAccessorImpl = accessorsAssembly.GetType("Nile.Accessors.HealthCheck.HealthCheckAccessor");
        var healthCheckAccessorAbstraction = accessorsAssembly.GetType("Nile.Accessors.HealthCheck.IHealthCheckAccessor");

        // Date utility (public interface -> internal implementation)
        if (dateUtilityImpl != null)
        {
            services.AddSingleton(typeof(AzureSdk.IDateUtility), dateUtilityImpl);
        }

        // Mapper concrete and internal abstraction
        if (mapperImpl != null)
        {
            services.AddSingleton(mapperImpl);

            if (mapperAbstraction != null)
            {
                services.AddSingleton(mapperAbstraction, sp => sp.GetRequiredService(mapperImpl));
            }

            // Expose AutoMapper IConfigurationProvider publicly for ProjectTo
            services.AddSingleton(typeof(AutoMapper.IConfigurationProvider), sp =>
            {
                var mapperInstance = sp.GetRequiredService(mapperImpl);
                var configProp = mapperImpl.GetProperty("Configuration", BindingFlags.Public | BindingFlags.Instance);
                var cfg = configProp?.GetValue(mapperInstance) as AutoMapper.IConfigurationProvider;
                if (cfg is null)
                    throw new InvalidOperationException("Mapper.Configuration not available");
                return cfg;
            });
        }

        // Accessors (public interfaces -> internal implementations)
        if (userAccessorImpl != null && userAccessorAbstraction != null)
        {
            services.AddScoped(userAccessorAbstraction, userAccessorImpl);
        }

        if (postAccessorImpl != null && postAccessorAbstraction != null)
        {
            services.AddScoped(postAccessorAbstraction, postAccessorImpl);
        }

        if (accountAccessorImpl != null && accountAccessorAbstraction != null)
        {
            services.AddScoped(accountAccessorAbstraction, accountAccessorImpl);
        }

        if (healthCheckAccessorImpl != null && healthCheckAccessorAbstraction != null)
        {
            services.AddSingleton(healthCheckAccessorAbstraction, healthCheckAccessorImpl);
        }

        return services;
    }

    private static Assembly? GetAccessorsAssembly()
    {
        // Try to find the accessors assembly among loaded assemblies
        var loaded = AppDomain.CurrentDomain.GetAssemblies();
        var asm = loaded.FirstOrDefault(a => a.GetName().Name == "Nile.Accessors");

        if (asm != null)
        {
            return asm;
        }

        // Attempt to load by name if not already loaded
        try
        {
            return Assembly.Load("Nile.Accessors");
        }
        catch
        {
            return null;
        }
    }
}