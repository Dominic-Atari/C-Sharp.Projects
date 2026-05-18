using Microsoft.Extensions.Logging;
using Nile.Common.Extensions;
using Nile.Managers.Admin;
using Nile.Managers.Proxys;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Nile.Client.Functions.Common;
using Nile.Common.InternalDTOs;
using Nile.Managers.Engagement; // for MobileUserContext (ContextBase-derived)
using Nile.Utilities; // IContextFactoryUtility

namespace Nile.Client.Functions.User.V1;

/// <summary>
/// User-facing HTTP endpoints for authentication, profile management, social interactions, and search.
/// Each endpoint delegates to domain managers via IProxy, keeping transport concerns separate from business logic.
/// </summary>
public class UserFunction : FunctionBase
{
    /// <summary>
    /// Root route prefix for all v1 user endpoints.
    /// </summary>
    private const string RouteBase = "V1/users";

    /// <summary>
    /// Proxy to the user manager (authentication, profile CRUD, settings, etc.).
    /// </summary>
    private readonly IProxy<IUserManager> _userManagerProxy;
    private readonly IProxy<ISocialEngagementManager> _socialEngagementManagerProxy;

    // Social engagement proxy removed for now to focus on user creation only.

    public UserFunction(
        ILogger<UserFunction> logger,
        IConfigUtility configUtility,
        IProxy<IUserManager> userManagerProxy,
        IContextFactoryUtility contextFactory,
        IProxy<ISocialEngagementManager> socialEngagementManagerProxy) : base(logger, configUtility)
    {
        _userManagerProxy = userManagerProxy;
        _socialEngagementManagerProxy = socialEngagementManagerProxy;
        _contextFactory = contextFactory;
    }

    private readonly IContextFactoryUtility _contextFactory;

    /// <summary>
    /// Authenticates the user and returns a context (tokens, user info) for the client session.
    /// Uses the user manager's Login action with an empty body (credentials come from ambient context or headers in the manager).
    /// </summary>
    [Function(nameof(UserFunction) + "_" + nameof(Login) + V1Suffix)]
    [ContextType(typeof(MobileUserContext))]
    [OpenApiOperation(nameof(Login))]
    [OpenApiResponseWithBody(System.Net.HttpStatusCode.OK, "application/json", typeof(CLI.V1.User.UserContextResponse))]
    public async Task<HttpResponseData> Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = RouteBase + "/:login")]
        HttpRequestData req)
    {
        var result =
            await _userManagerProxy.RunWithoutRequestBody<CLI.V1.User.LoginRequest, CLI.V1.User.UserContextResponse>(
                mgr => mgr.Login);

        return await CreateResponse(req, result);
    }

    /// <summary>
    /// Generates and returns username suggestions for a prospective user.
    /// </summary>
    [Function(nameof(UserFunction) + "_" + nameof(GetUsernameSuggestions) + V1Suffix)]
    [ContextType(typeof(MobileUserContext))]
    [OpenApiOperation(nameof(GetUsernameSuggestions))]
    [OpenApiResponseWithBody(System.Net.HttpStatusCode.OK, "application/json", typeof(CLI.V1.User.UsernameSuggestionsResponse))]
    public async Task<HttpResponseData> GetUsernameSuggestions(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = RouteBase + "/:usernameSuggestions")]
        HttpRequestData req)
    {
        var result = await _userManagerProxy
            .RunWithoutRequestBody<CLI.V1.User.UsernameSuggestionsRequest, CLI.V1.User.UsernameSuggestionsResponse>(
                mgr => mgr.GenerateUsernameSuggestions);

        return await CreateResponse(req, result);
    }

    /// <summary>
    /// Creates a new user profile from the request body and returns the stored profile summary.
    /// </summary>
    [Function(nameof(UserFunction) + "_" + nameof(CreateProfile) + V1Suffix)]
    [ContextType(typeof(MobileUserContext))]
    [OpenApiOperation(nameof(CreateProfile))]
    [OpenApiResponseWithBody(System.Net.HttpStatusCode.OK, "application/json", typeof(CLI.V1.User.StoreUserResponseBase))]
    public async Task<HttpResponseData> CreateProfile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = RouteBase + "/profile")]
        HttpRequestData req)
    {
        var result = await _userManagerProxy
            .RunWithRequestStream<CLI.V1.User.CreateUserProfileRequest, CLI.V1.User.StoreUserResponseBase>(
                mgr => mgr.Store,
                req.Body);

        return await CreateResponse(req, result);
    }

    /// <summary>
    /// Updates an existing user profile (FirstName, LastName) for the given Username.
    /// </summary>
    [Function(nameof(UserFunction) + "_" + nameof(UpdateProfile) + V1Suffix)]
    [ContextType(typeof(MobileUserContext))]
    [OpenApiOperation(nameof(UpdateProfile))]
    [OpenApiResponseWithBody(System.Net.HttpStatusCode.OK, "application/json", typeof(CLI.V1.User.StoreUserResponseBase))]
    public async Task<HttpResponseData> UpdateProfile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = RouteBase + "/profile")]
        HttpRequestData req)
    {
        var result = await _userManagerProxy
            .RunWithRequestStream<CLI.V1.User.UpdateUserProfileRequest, CLI.V1.User.StoreUserResponseBase>(
                mgr => mgr.Store,
                req.Body);

        return await CreateResponse(req, result);
    }

    /// <summary>
    /// Stores or replaces the user's profile image filename. The image content itself is uploaded
    /// out-of-band via a SAS token; this endpoint only persists the resulting filename reference.
    /// </summary>
    [Function(nameof(UserFunction) + "_" + nameof(StoreProfileImage) + V1Suffix)]
    [ContextType(typeof(MobileUserContext))]
    [OpenApiOperation(nameof(StoreProfileImage))]
    [OpenApiResponseWithBody(System.Net.HttpStatusCode.OK, "application/json", typeof(CLI.V1.User.StoreUserResponseBase))]
    public async Task<HttpResponseData> StoreProfileImage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = RouteBase + "/profile/image")]
        HttpRequestData req)
    {
        var result = await _userManagerProxy
            .RunWithRequestStream<CLI.V1.User.StoreUserProfileImageRequest, CLI.V1.User.StoreUserResponseBase>(
                mgr => mgr.Store,
                req.Body);

        return await CreateResponse(req, result);
    }

    /// <summary>
    /// Deletes the current user's profile image reference, if present.
    /// </summary>
    [Function(nameof(UserFunction) + "_" + nameof(DeleteProfileImage) + V1Suffix)]
    [ContextType(typeof(MobileUserContext))]
    [OpenApiOperation(nameof(DeleteProfileImage))]
    [OpenApiResponseWithBody(System.Net.HttpStatusCode.OK, "application/json", typeof(CLI.V1.User.StoreUserResponseBase))]
    public async Task<HttpResponseData> DeleteProfileImage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = RouteBase + "/profile/image")]
        HttpRequestData req)
    {
        var result = await _userManagerProxy
            .RunWithRequestStream<CLI.V1.User.DeleteUserProfileImageRequest, CLI.V1.User.StoreUserResponseBase>(
                mgr => mgr.Store,
                req.Body);

        return await CreateResponse(req, result);
    }

    /// <summary>
    /// Stores user notification preferences. The request body carries the new boolean toggles to persist.
    /// </summary>
    [Function(nameof(UserFunction) + "_" + nameof(StoreSettings) + V1Suffix)]
    [ContextType(typeof(MobileUserContext))]
    [OpenApiOperation(nameof(StoreSettings))]
    [OpenApiResponseWithBody(System.Net.HttpStatusCode.OK, "application/json", typeof(CLI.V1.User.StoreUserResponseBase))]
    public async Task<HttpResponseData> StoreSettings(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = RouteBase + "/settings")]
        HttpRequestData req)
    {
        var result = await _userManagerProxy
            .RunWithRequestStream<CLI.V1.User.StoreNotificationPreferencesRequest, CLI.V1.User.StoreUserResponseBase>(
                mgr => mgr.Store,
                req.Body);

        return await CreateResponse(req, result);
    }

}