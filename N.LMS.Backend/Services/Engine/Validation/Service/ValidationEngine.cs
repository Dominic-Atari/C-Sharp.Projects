using N.LMS.Common.Interface.Framework;
using N.LMS.Common.Interface.Request;
using N.LMS.Common.Interface.Result;
using N.LMS.Engine.Validation.Interface;

namespace N.LMS.Engine.Validation.Service;

internal sealed class ValidationEngine : ProxyEnabledServiceBase, IValidationEngine
{
    public Task<ValidateResultBase> Validate(ValidateRequestBase request)
    {
        // Default-pass implementation. Per-manager rules can be added by switching
        // on request.GetType() / request.InboundRequest?.GetType() here, or by adding
        // additional engine implementations and a dispatcher.
        ValidateResultBase result = new ValidateResult();
        return Task.FromResult(result);
    }
}
