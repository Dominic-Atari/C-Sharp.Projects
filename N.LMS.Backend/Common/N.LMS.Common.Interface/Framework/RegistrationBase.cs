namespace N.LMS.Common.Interface.Framework;

public abstract record RegistrationBase
{
    public abstract Type ServiceType { get; }
    public abstract Type ImplementationType { get; }
    public abstract LifetimeScope Lifetime { get; }
    public abstract IReadOnlyList<IInterceptor> Interceptors { get; }
}

public sealed record TypeRegistration : RegistrationBase
{
    public override Type ServiceType { get; }
    public override Type ImplementationType { get; }
    public override LifetimeScope Lifetime { get; }
    public override IReadOnlyList<IInterceptor> Interceptors { get; }

    private TypeRegistration(
        Type serviceType,
        Type implementationType,
        LifetimeScope lifetime,
        IReadOnlyList<IInterceptor> interceptors)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
        Lifetime = lifetime;
        Interceptors = interceptors;
    }

    public static TypeRegistration New<TService, TImpl>()
        where TService : IComponent
        where TImpl : class, TService =>
        new(typeof(TService), typeof(TImpl), LifetimeScope.Scoped, Array.Empty<IInterceptor>());

    public static TypeRegistration New<TService, TImpl>(LifetimeScope lifetime)
        where TService : IComponent
        where TImpl : class, TService =>
        new(typeof(TService), typeof(TImpl), lifetime, Array.Empty<IInterceptor>());

    public static TypeRegistration New<TService, TImpl>(IInterceptor[] interceptors)
        where TService : IComponent
        where TImpl : class, TService =>
        new(typeof(TService), typeof(TImpl), LifetimeScope.Scoped, interceptors);

    public static TypeRegistration New<TService, TImpl>(LifetimeScope lifetime, IInterceptor[] interceptors)
        where TService : IComponent
        where TImpl : class, TService =>
        new(typeof(TService), typeof(TImpl), lifetime, interceptors);
}
