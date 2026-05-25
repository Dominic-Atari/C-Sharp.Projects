namespace N.LMS.Common.Service;

public static class ServiceProxyHost
{
    private static IServiceProvider? _root;

    public static IServiceProvider Root =>
        _root ?? throw new InvalidOperationException(
            "ServiceProxyHost has not been configured. Call ServiceProxyHost.Configure(provider) at startup.");

    public static void Configure(IServiceProvider provider) => _root = provider;
}
