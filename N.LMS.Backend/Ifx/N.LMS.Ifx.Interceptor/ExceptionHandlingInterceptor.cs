using System.Reflection;
using Castle.DynamicProxy;
using N.LMS.Common.Interface.Errors;
using N.LMS.Common.Interface.Exceptions;
using N.LMS.Common.Interface.Framework;
using N.LMS.Common.Interface.Request;
using N.LMS.Common.Interface.Result;
using N.LMS.Ifx.Interceptor.Extension;
using N.LMS.Utility.Logging.Interface;
using N.LMS.Utility.Logging.Interface.Request;

namespace N.LMS.Ifx.Interceptor;

/// <summary>
/// Outermost interceptor. Catches both pipeline-time and invocation-time throws, logs them via
/// ILoggingUtility, and converts known exception types to typed <see cref="ErrorBase"/> records
/// on a result envelope of the correct concrete type. Unknown exceptions become <see cref="InternalError"/>.
/// </summary>
public sealed class ExceptionHandlingInterceptor : AsyncMethodInterceptorBase
{
    protected override void InterceptMethodInvocation(
        IInvocationProceedInfo invocationProceedInfo,
        IInvocation invocation)
    {
        var resultType = invocation.GetTaskResultType();

        var asyncMethod = typeof(ExceptionHandlingInterceptor)
            .GetMethod(nameof(TryAsync), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(resultType);

        invocation.ReturnValue = asyncMethod.Invoke(this, new object[] { invocationProceedInfo, invocation })!;
    }

    private async Task<TResult> TryAsync<TResult>(
        IInvocationProceedInfo invocationProceedInfo,
        IInvocation invocation) where TResult : ResultBase
    {
        try
        {
            invocationProceedInfo.Invoke();
            return await (Task<TResult>)invocation.ReturnValue!;
        }
        catch (Exception ex)
        {
            TryLog(ex);
            return CreateFailedResult<TResult>(invocation, ex);
        }
    }

    private static TResult CreateFailedResult<TResult>(IInvocation invocation, Exception exception)
        where TResult : ResultBase
    {
        var inboundRequest = invocation.GetFirstArgument<RequestBase>();
        var emptyResult = inboundRequest.CreateResultFromRequest<TResult>();

        ErrorBase error = exception switch
        {
            NotFoundException nf => new NotFoundError
            {
                EntityName = nf.EntityName,
                FieldName = nf.FieldName,
                FieldValue = nf.FieldValue,
                Message = nf.Message,
                PublicMessage = nf.Message,
                Exception = nf
            },
            ConflictException ce => new ConflictError
            {
                EntityName = ce.EntityName,
                FieldName = ce.FieldName,
                Message = ce.Message,
                PublicMessage = ce.Message,
                Exception = ce
            },
            ValidationException ve => new ValidationError
            {
                FieldName = ve.FieldName,
                Message = ve.Message,
                PublicMessage = ve.Message,
                Exception = ve
            },
            _ => new InternalError
            {
                Message = exception.Message,
                PublicMessage = "An unexpected error occurred.",
                Exception = exception
            }
        };

        return (TResult)(emptyResult with { Errors = [error] });
    }

    private static void TryLog(Exception exception)
    {
        try
        {
            var logger = ServiceLocator.Current.Resolve<ILoggingUtility>();
            logger.Log(new ExceptionLogRequest { Exception = exception });
        }
        catch
        {
            // Logging must never throw out of the outermost handler.
        }
    }
}
