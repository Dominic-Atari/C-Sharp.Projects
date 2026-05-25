namespace N.LMS.Common.Interface.Framework;

public interface IServiceLocator
{
    T Resolve<T>() where T : IComponent;
}

public static class ServiceLocator
{
    private static readonly AsyncLocal<IServiceLocator?> _scoped = new();
    private static IServiceLocator? _fallback;

    public static IServiceLocator Current =>
        _scoped.Value ?? _fallback ?? throw new InvalidOperationException(
            "ServiceLocator is not configured. Call ServiceLocator.Configure() at host startup " +
            "and EnterScope/ExitScope per request.");

    public static void Configure(IServiceLocator locator) => _fallback = locator;
    public static void EnterScope(IServiceLocator locator) => _scoped.Value = locator;
    public static void ExitScope() => _scoped.Value = null;
}
