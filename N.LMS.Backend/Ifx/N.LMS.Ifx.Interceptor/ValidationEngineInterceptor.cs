using System.Reflection;
using Castle.DynamicProxy;
using N.LMS.Common.Interface.Errors;
using N.LMS.Common.Interface.Framework;
using N.LMS.Common.Interface.Mapping;
using N.LMS.Common.Interface.Request;
using N.LMS.Common.Interface.Result;
using N.LMS.Engine.Validation.Interface;
using N.LMS.Engine.Validation.Interface.Request;
using N.LMS.Ifx.Interceptor.Extension;

namespace N.LMS.Ifx.Interceptor;

/// <summary>
/// Calls <see cref="IValidationEngine.Validate"/> with a request translated by the manager's
/// own <see cref="MapperBase"/>. If validation fails, short-circuits: the wrapped invocation
/// is never proceeded, and a failed result envelope (concrete type derived via name swap) is
/// returned with the engine's errors.
/// </summary>
public sealed class ValidationEngineInterceptor : AsyncMethodInterceptorBase
{
    private readonly MapperBase _mapper;

    public ValidationEngineInterceptor(MapperBase mapper)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    protected override void InterceptMethodInvocation(
        IInvocationProceedInfo invocationProceedInfo,
        IInvocation invocation)
    {
        if (invocation.InvocationTarget is not IProxyEnabledSubsystem)
        {
            throw new InvalidOperationException(
                $"{nameof(ValidationEngineInterceptor)} can only be used on services that implement {nameof(IProxyEnabledSubsystem)}; " +
                $"got {invocation.InvocationTarget?.GetType().Name ?? "null"}.");
        }

        var resultType = invocation.GetTaskResultType();
        var asyncMethod = typeof(ValidationEngineInterceptor)
            .GetMethod(nameof(ValidateThenInvokeAsync), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(resultType);

        invocation.ReturnValue = asyncMethod.Invoke(this, new object[] { invocationProceedInfo, invocation })!;
    }

    private async Task<TResult> ValidateThenInvokeAsync<TResult>(
        IInvocationProceedInfo invocationProceedInfo,
        IInvocation invocation) where TResult : ResultBase
    {
        var inboundRequest = invocation.GetFirstArgument<RequestBase>();

        // Try to map the inbound manager request to a typed engine ValidateRequest via the
        // manager's own Mapper. If no map is configured, fall back to the generic request.
        ValidateRequestBase engineRequest;
        try
        {
            engineRequest = _mapper.Map<ValidateRequestBase>(inboundRequest)
                            ?? new GenericValidateRequest { InboundRequest = inboundRequest };
        }
        catch (AutoMapper.AutoMapperMappingException)
        {
            engineRequest = new GenericValidateRequest { InboundRequest = inboundRequest };
        }

        var engine = ServiceLocator.Current.Resolve<IValidationEngine>();
        var validateResult = await engine.Validate(engineRequest);

        if (!validateResult.IsSuccessful)
        {
            var failed = inboundRequest.CreateResultFromRequest<TResult>();
            return (TResult)(failed with { Errors = validateResult.Errors ?? Array.Empty<ErrorBase>() });
        }

        invocationProceedInfo.Invoke();
        return await (Task<TResult>)invocation.ReturnValue!;
    }
}
