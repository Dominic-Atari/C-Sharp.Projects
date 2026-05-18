using Microsoft.Extensions.Logging;
using Nile.Accessors.Account;
using Nile.Accessors.HealthCheck;
// using Nile.Accessors.Account;
using Nile.Accessors.User;
using Nile.Engines.Validation;
using Nile.Utilities;
using Nile.Utilities.AzureSdk;

// using Nile.Utilities;
// using Nile.Utilities.AzureSdk;

namespace Nile.Managers.Admin;

internal partial class AdminManager : ManagerBase, IUserManager
{
    private readonly IAccountAccessor _accountAccessor;

    //private readonly IAccountEngine _accountEngine;

    //private readonly IAdministratorAccessor _administratorAccessor;

    private readonly IContextFactoryUtility _contextFactoryUtility;

    private readonly IHealthCheckEngine _healthCheckEngine;

    //private readonly IUsernameSuggestionEngine _usernameSuggestionEngine;

    private readonly IValidationEngine _validationEngine;

    private readonly IHealthCheckAccessor _healthCheckAccessor;

    private readonly IBlobStorageUtility _blobStorageUtility;

    private readonly IMessageBusUtility _messageBusUtility;

    private readonly IUserAccessor _userAccessor;

    private readonly ISocialFeedUtility _socialFeedUtility;

    public AdminManager(
        ILogger<AdminManager> logger,
        IAccountAccessor accountAccessor,
        IContextFactoryUtility contextFactoryUtility,
        IHealthCheckEngine healthCheckEngine,
        //IUsernameSuggestionEngine usernameSuggestionEngine,
        IValidationEngine validationEngine,
        IHealthCheckAccessor healthCheckAccessor,
        IBlobStorageUtility blobStorageUtility,
        IMessageBusUtility messageBusUtility,
        IUserAccessor userAccessor,
        ISocialFeedUtility socialFeedUtility
    ) : base(logger)
    {
        _accountAccessor = accountAccessor;
        //_accountEngine = accountEngine;
        //_administratorAccessor = administratorAccessor;
        _contextFactoryUtility = contextFactoryUtility;
        _healthCheckEngine = healthCheckEngine;
        //_usernameSuggestionEngine = usernameSuggestionEngine;
        _validationEngine = validationEngine;
        _healthCheckAccessor = healthCheckAccessor;
        _blobStorageUtility = blobStorageUtility;
        _messageBusUtility = messageBusUtility;
        _userAccessor = userAccessor;
        _socialFeedUtility = socialFeedUtility;
    }
}