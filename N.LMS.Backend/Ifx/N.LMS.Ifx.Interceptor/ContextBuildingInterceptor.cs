using System.Reflection;
using Castle.DynamicProxy;
using N.LMS.Common.Interface.Context;
using N.LMS.Common.Interface.Framework;
using N.LMS.Common.Interface.Result;
using N.LMS.Ifx.Interceptor.Extension;

namespace N.LMS.Ifx.Interceptor;

/// <summary>
/// Builds the ambient context for the call graph by invoking
/// <see cref="IContextUtility.BuildAmbientContext"/> exactly once before the handler runs.
/// Asserts that the target is an <see cref="IProxyEnabledSubsystem"/>.
/// </summary>
public sealed class ContextBuildingInterceptor : AsyncMethodInterceptorBase
{
    protected override void InterceptMethodInvocation(
        IInvocationProceedInfo invocationProceedInfo,
        IInvocation invocation)
    {
        if (invocation.InvocationTarget is not IProxyEnabledSubsystem)
        {
            throw new InvalidOperationException(
                $"{nameof(ContextBuildingInterceptor)} can only be used on services that implement {nameof(IProxyEnabledSubsystem)}; " +
                $"got {invocation.InvocationTarget?.GetType().Name ?? "null"}.");
        }

        var resultType = invocation.GetTaskResultType();
        var asyncMethod = typeof(ContextBuildingInterceptor)
            .GetMethod(nameof(BuildContextAsync), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(resultType);

        invocation.ReturnValue = asyncMethod.Invoke(this, new object[] { invocationProceedInfo, invocation })!;
    }

    private async Task<TResult> BuildContextAsync<TResult>(
        IInvocationProceedInfo invocationProceedInfo,
        IInvocation invocation) where TResult : ResultBase
    {
        var contextUtility = ServiceLocator.Current.Resolve<IContextUtility>();
        await contextUtility.BuildAmbientContext();

        invocationProceedInfo.Invoke();
        return await (Task<TResult>)invocation.ReturnValue!;
    }
}
