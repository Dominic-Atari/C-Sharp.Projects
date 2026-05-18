using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Nile.Client.Functions.Common;
using Nile.Common.InternalDTOs;
using Nile.Managers.Engagement;
using Nile.Managers.Proxys;
using Nile.Utilities;
using System.Linq;
using System.Threading.Tasks;
using Nile.Common.Extensions;

namespace Nile.Client.Functions.Social.V1;

public class PostFunction : FunctionBase
{
    private const string RouteBase = "V1/posts";
    private readonly IProxy<ISocialEngagementManager> _socialEngagementManagerProxy;
    private readonly IContextFactoryUtility _contextFactory;

    public PostFunction(
        ILogger<PostFunction> logger,
        IConfigUtility configUtility,
        IProxy<ISocialEngagementManager> socialEngagementManagerProxy,
        IContextFactoryUtility contextFactory) : base(logger, configUtility)
    {
        _socialEngagementManagerProxy = socialEngagementManagerProxy;
        _contextFactory = contextFactory;
    }

    [Function(nameof(PostFunction) + "_CreatePost" + V1Suffix)]
    [ContextType(typeof(MobileUserContext))]
    public async Task<HttpResponseData> CreatePost(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = RouteBase)] HttpRequestData req)
    {
        var authHeader = req.Headers.TryGetValues("Authorization", out var vals)
            ? vals.FirstOrDefault()
            : null;
        _contextFactory.BuildContext(typeof(MobileUserContext), authHeader);

        var result = await _socialEngagementManagerProxy
            .RunWithRequestStream<CLI.V1.Social.CreatePostRequest, CLI.V1.Social.CreatePostResponse>(
                mgr => mgr.Store,
                req.Body);

        return await CreateResponse(req, result);
    }
}
