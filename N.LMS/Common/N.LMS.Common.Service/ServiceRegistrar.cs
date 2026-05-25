using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using N.LMS.Common.Interface.Framework;

namespace N.LMS.Common.Service;

public static class ServiceRegistrar
{
    private static readonly ProxyGenerator _ProxyGenerator = new();

    public static IServiceCollection RegisterServices(IServiceCollection services, RegistrationBuilder builder)
    {
        foreach (var registration in builder.Registrations)
        {
            RegisterOne(services, registration);
        }
        return services;
    }

    private static void RegisterOne(IServiceCollection services, RegistrationBase registration)
    {
        if (registration.Interceptors.Count == 0)
        {
            // Naked registration: accessors, engines, utilities.
            services.Add(new ServiceDescriptor(
                registration.ServiceType,
                registration.ImplementationType,
                ToServiceLifetime(registration.Lifetime)));
            return;
        }

        // Intercepted registration: managers.
        // 1) Register the concrete implementation under its own type so the proxy factory
        //    can resolve it as the proxy target.
        services.Add(new ServiceDescriptor(
            registration.ImplementationType,
            registration.ImplementationType,
            ToServiceLifetime(registration.Lifetime)));

        // 2) Register the interface as a factory that wraps the target in a dynamic proxy
        //    with this registration's interceptor chain.
        var serviceType = registration.ServiceType;
        var implType = registration.ImplementationType;
        var interceptors = registration.Interceptors
            .OfType<IAsyncInterceptor>()
            .ToArray();
        if (interceptors.Length != registration.Interceptors.Count)
        {
            throw new InvalidOperationException(
                $"All interceptors registered on '{serviceType.Name}' must implement IAsyncInterceptor " +
                $"(typically by inheriting AsyncMethodInterceptorBase).");
        }

        services.Add(new ServiceDescriptor(
            serviceType,
            sp =>
            {
                var target = sp.GetRequiredService(implType);
                return _ProxyGenerator.CreateInterfaceProxyWithTargetInterface(
                    serviceType,
                    target,
                    interceptors);
            },
            ToServiceLifetime(registration.Lifetime)));
    }

    private static ServiceLifetime ToServiceLifetime(LifetimeScope lifetime) => lifetime switch
    {
        LifetimeScope.Singleton => ServiceLifetime.Singleton,
        LifetimeScope.Transient => ServiceLifetime.Transient,
        _                       => ServiceLifetime.Scoped
    };
}
