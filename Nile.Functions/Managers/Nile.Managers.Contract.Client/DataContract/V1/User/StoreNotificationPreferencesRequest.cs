namespace Nile.Managers.Contract.Client.DataContract.V1.User;

public class StoreNotificationPreferencesRequest : StoreUserRequestBase
{
    public bool NotifyOnFriendRequestReceived { get; init; }

    public bool NotifyOnFriendRequestApproved { get; init; }

    public bool NotifyOnPostCommentReceived { get; init; } = true;
}