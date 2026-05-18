using Nile.Managers.Contract.Client.Mapping;
using Nile.Common.InternalDTOs;

namespace Nile.Managers.Engagement;

internal partial class EngagementManager
{
    public async Task<CLI.ResponseBase> Get(CLI.RequestBase request)
    {
        return request switch
        {
            Social.PostEnrichmentRequest req => await GetPostEnrichment(req),
            _ => throw new NotSupportedException($"Unsupported request type: {request.GetType().Name}")
        };
    }

    private async Task<CLI.ResponseBase> GetPostEnrichment(Social.PostEnrichmentRequest req)
    {
        // Map client request to internal request and fetch via accessor
        var dtoRequest = ClientDtoMapper.Map<DTO.PostEnrichmentRequest>(req);
        var response = await _postAccessor.Get(dtoRequest);

        // TODO: enrich and map to actual client post models
        return ClientDtoMapper.Map<Social.PostEnrichmentResponse>(response);
        
    }

    public async Task<CLI.ResponseBase> Interact(CLI.RequestBase request)
    {
        throw new NotImplementedException();
    }

    public async Task<CLI.ResponseBase> OnFriendRequestAcceptedEvent(Social.FriendRequestAcceptedEvent @event)
    {
        throw new NotImplementedException();
    }

    public async Task<CLI.ResponseBase> OnUnfriendEvent(Social.UnfriendEvent @event)
    {
        throw new NotImplementedException();
    }

    public async Task<User.UserSearchResponseBase> Search(User.UserSearchRequestBase request)
    {
        throw new NotImplementedException();
    }

    public async Task<CLI.ResponseBase> Store(CLI.RequestBase request)
    {
        return request switch
        {
            Social.CreatePostRequest req => await CreatePost(req),
            _ => throw new NotSupportedException($"Unsupported request type: {request.GetType().Name}")
        };
    }

    // Strongly-typed convenience overload required by ISocialEngagementManager
    public async Task<Social.CreatePostResponse> Store(Social.CreatePostRequest request)
    {
        var resp = await CreatePost(request) as Social.CreatePostResponse
                   ?? throw new InvalidOperationException("Unexpected response type from CreatePost");
        return resp;
    }

    public async Task<User.UserSearchResponseBase> Search(User.FriendListSearchRequest arg)
    {
        throw new NotImplementedException();
    }

    private async Task<CLI.ResponseBase> CreatePost(Social.CreatePostRequest request)
    {
        // Map client request to internal DTO and inject current user from context
        var dtoReq = ClientDtoMapper.Map<DTO.CreatePostRequest>(request);
        var ctx = _contextFactoryUtility.GetContext<MobileUserContext>();
        dtoReq.UserId = ctx.UserId;
        if (dtoReq.UserId == Guid.Empty)
        {
            throw new InvalidOperationException("UserId missing in context; ensure Authorization header carries the user id.");
        }

        // Persist the post and get PostId
        var createResp = await _postAccessor.Store(dtoReq) as DTO.CreatePostResponse
                         ?? throw new InvalidOperationException("Unexpected response type from PostAccessor for CreatePostRequest");

        // Post to social feed to obtain an external id
        var externalIdStr = await _socialFeedUtility.Post(ctx.UserId.ToString(), createResp.PostId.ToString());
        var externalId = Guid.TryParse(externalIdStr, out var parsed) ? parsed : Guid.NewGuid();

        // Persist the external id on our side (no-op if schema does not support it yet)
        var updateReq = new DTO.UpdatePostExternalIdRequest
        {
            PostId = createResp.PostId,
            ExternalId = externalId
        };
        await _postAccessor.Store(updateReq);

        // Build client response
        return new Social.CreatePostResponse
        {
            PostId = createResp.PostId,
            ExternalId = externalId
        };
    }
}