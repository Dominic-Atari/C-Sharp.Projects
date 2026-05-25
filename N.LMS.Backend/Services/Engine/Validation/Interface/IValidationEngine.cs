using N.LMS.Common.Interface.Framework;
using N.LMS.Common.Interface.Request;
using N.LMS.Common.Interface.Result;

namespace N.LMS.Engine.Validation.Interface;

public interface IValidationEngine : IEngine
{
    Task<ValidateResultBase> Validate(ValidateRequestBase request);
}
