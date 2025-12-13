namespace Nile.Managers.Engagement;

public interface ISocialEngagementManager
{
    Task<CLI.ResponseBase> Get(CLI.RequestBase request);

    Task<CLI.ResponseBase> Interact(CLI.RequestBase request);

    Task<CLI.ResponseBase> OnFriendRequestAcceptedEvent(Social.FriendRequestAcceptedEvent @event);

    Task<CLI.ResponseBase> OnUnfriendEvent(Social.UnfriendEvent @event);

    Task<User.UserSearchResponseBase> Search(User.UserSearchRequestBase request);

    Task<CLI.ResponseBase> Store(CLI.RequestBase request);

    Task<User.UserSearchResponseBase> Search(User.FriendListSearchRequest arg);
    Task<Social.CreatePostResponse> Store(Social.CreatePostRequest arg);
}