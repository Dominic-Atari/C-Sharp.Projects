namespace Nile.Common.InternalDTOs;

public class StoreNotificationPreferencesRequest : UserRequestBase
{
    public bool NotifyOnFriendRequestReceived { get; set; }
    public bool NotifyOnFriendRequestApproved { get; set; }
    public bool NotifyOnPostCommentReceived { get; set; }
}
