using Microsoft.Extensions.Logging;
using Nile.Accessors.User;
using Nile.Utilities;

namespace Nile.Engines.Validation;

public class ValidationEngine : EngineBase, IValidationEngine
{
    private readonly IContextFactoryUtility _contextFactoryUtility;
    private readonly IUserAccessor _userAccessor;

    public ValidationEngine(ILogger<ValidationEngine> logger,
        IContextFactoryUtility contextFactoryUtility,
        IUserAccessor userAccessor) : base(logger)
    {
        _contextFactoryUtility = contextFactoryUtility;
        _userAccessor = userAccessor;
    }

    // this is used for validation of CLI requests like friend request
    public async Task Validate(CLI.RequestBase request)
    {
        throw new NotImplementedException();
    }
    // this is used for validation of user requests like store user
    public Task Validate(DTO.StoreUserRequestBase request)
    {
        return request switch
        {
            DTO.CreateUserProfileRequest req => Validate(req),
            _ => throw new NotImplementedException($"{nameof(Validate)} not implemented for '{request.GetType().Name}' (yet!).")
        };
    }
}