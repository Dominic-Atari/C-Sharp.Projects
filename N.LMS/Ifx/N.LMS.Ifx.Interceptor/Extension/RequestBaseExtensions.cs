using N.LMS.Common.Interface.Request;
using N.LMS.Common.Interface.Result;

namespace N.LMS.Ifx.Interceptor.Extension;

public static class RequestBaseExtensions
{
    /// <summary>
    /// Fabricates an instance of the result type that pairs with the given request, by
    /// string-substituting "Request" → "Result" in the request's assembly-qualified name.
    /// Used by interceptors when a failure (exception or validation) needs a result envelope
    /// of the correct concrete type.
    /// </summary>
    public static TResult CreateResultFromRequest<TResult>(this RequestBase request)
        where TResult : ResultBase
    {
        var requestName = request.GetType().AssemblyQualifiedName!;
        var resultName = requestName.Replace("Request", "Result");
        var resultType = Type.GetType(resultName)
            ?? throw new TypeLoadException(
                $"Result type '{resultName}' was not found. Ensure a result type exists to match this request " +
                $"and that the XxxRequest ↔ XxxResult naming convention is followed.");
        return (TResult)Activator.CreateInstance(resultType)!;
    }
}
