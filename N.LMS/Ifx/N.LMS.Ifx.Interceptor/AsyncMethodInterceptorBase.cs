using Castle.DynamicProxy;
using FrameworkInterceptor = N.LMS.Common.Interface.Framework.IInterceptor;

namespace N.LMS.Ifx.Interceptor;

/// <summary>
/// Wraps Castle.Core.AsyncInterceptor's <see cref="IAsyncInterceptor"/> into the EC.LMS-style
/// "InterceptMethodInvocation" shape used by every concrete interceptor in this layer.
/// Also implements the Common.Interface metadata marker so instances can be passed to
/// <c>TypeRegistration.New(interceptors: [...])</c>.
/// </summary>
public abstract class AsyncMethodInterceptorBase : IAsyncInterceptor, FrameworkInterceptor
{
    // Castle entry points -------------------------------------------------------

    public void InterceptSynchronous(IInvocation invocation)
    {
        // Synchronous interception isn't used in this codebase; pass through.
        invocation.Proceed();
    }

    public void InterceptAsynchronous(IInvocation invocation)
    {
        // Plain Task (no result) isn't used by Managers; pass through.
        invocation.Proceed();
    }

    public void InterceptAsynchronous<TResult>(IInvocation invocation)
    {
        InterceptMethodInvocation(invocation.CaptureProceedInfo(), invocation);
    }

    // EC.LMS-style entry point that derived classes override -------------------

    protected abstract void InterceptMethodInvocation(
        IInvocationProceedInfo invocationProceedInfo,
        IInvocation invocation);

    // Common.Interface metadata marker ------------------------------------------
    public string Name => GetType().Name;
}
