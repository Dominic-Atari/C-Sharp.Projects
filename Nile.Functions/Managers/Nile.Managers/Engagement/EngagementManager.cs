using Microsoft.Extensions.Logging;
using Nile.Accessors.Posts;
using Nile.Accessors.User;
using Nile.Utilities;

namespace Nile.Managers.Engagement;

internal partial class EngagementManager : ManagerBase, ISocialEngagementManager
{
    private readonly IContextFactoryUtility _contextFactoryUtility;

    private readonly IUserAccessor _userAccessor;

    private readonly IMessageBusUtility _messageBusUtility;

    private readonly ISocialAuthUtility _socialAuthUtility;
    
    private readonly ISocialFeedUtility _socialFeedUtility;
    
    private readonly IPostAccessor _postAccessor;
    
    public EngagementManager(ILogger<EngagementManager> logger,
        IContextFactoryUtility contextFactoryUtility,
        IUserAccessor userAccessor,
        IMessageBusUtility messageBusUtility,
        ISocialAuthUtility socialAuthUtility,
        ISocialFeedUtility socialFeedUtility,
        IPostAccessor postAccessor
    ) : base(logger)
    {
        _contextFactoryUtility = contextFactoryUtility;
        _userAccessor = userAccessor;
        _messageBusUtility = messageBusUtility;
        _socialAuthUtility = socialAuthUtility;
        _socialFeedUtility = socialFeedUtility;
        _postAccessor = postAccessor;
    }
}