using Microsoft.Extensions.DependencyInjection;
using N.LMS.Common.Interface.Context;
using N.LMS.Common.Interface.Framework;

namespace N.LMS.Common.Service;

/// <summary>
/// Per-call scope: creates a DI scope, deposits the typed source context (e.g. WebContext) into
/// IContextUtility, and points the AsyncLocal ServiceLocator at the scope so
/// ProxyEnabledServiceBase.ProxyForService&lt;T&gt;() resolves from this scope.
/// <para>
/// Derived ambient contexts (e.g. UserContext from claims) are NOT built here — the
/// ContextBuildingInterceptor on managers calls IContextUtility.BuildAmbientContext for that.
/// </para>
/// Dispose unwinds the scope.
/// </summary>
public sealed class ServiceProxyGenerator : IDisposable
{
    private readonly IServiceScope _scope;
    private bool _disposed;

    public ServiceProxyGenerator(object context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        _scope = ServiceProxyHost.Root.CreateScope();

        var contextUtility = _scope.ServiceProvider.GetRequiredService<IContextUtility>();
        DepositSourceContext(contextUtility, context);

        ServiceLocator.EnterScope(new DiServiceLocator(_scope.ServiceProvider));
    }

    public T ProxyForService<T>() where T : IComponent =>
        _scope.ServiceProvider.GetRequiredService<T>();

    private static void DepositSourceContext(IContextUtility utility, object context)
    {
        // Deposit by the runtime type so GetRequiredContext<TThatType>() finds it.
        var method = typeof(IContextUtility).GetMethod(nameof(IContextUtility.SetTypedContext))!
            .MakeGenericMethod(context.GetType());
        method.Invoke(utility, new[] { context });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ServiceLocator.ExitScope();
        _scope.Dispose();
    }
}
