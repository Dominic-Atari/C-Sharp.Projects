using Castle.DynamicProxy;

namespace N.LMS.Ifx.Interceptor.Extension;

internal static class InvocationExtensions
{
    public static Type GetTaskResultType(this IInvocation invocation)
    {
        var returnType = invocation.Method.ReturnType;
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            return returnType.GetGenericArguments()[0];
        }
        throw new InvalidOperationException(
            $"Method '{invocation.Method.Name}' does not return Task<TResult>; cannot run async interceptor.");
    }

    public static T GetFirstArgument<T>(this IInvocation invocation)
    {
        if (invocation.Arguments.Length == 0)
            throw new InvalidOperationException(
                $"Method '{invocation.Method.Name}' has no arguments; cannot read first.");
        return (T)invocation.Arguments[0];
    }
}
