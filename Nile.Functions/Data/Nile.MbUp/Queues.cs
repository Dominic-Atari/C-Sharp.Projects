namespace Nile.MbUp;

/// <summary>
/// Defines queue names for the Nile message bus system.
/// </summary>
public static class Queues
{
    public const string PostCreated = "post-created";
    public const string PostUpdated = "post-updated";
    public const string PostDeleted = "post-deleted";
    public const string CommentCreated = "comment-created";
    public const string NotificationSend = "notification-send";
    public const string UserFollowed = "user-followed";
    public const string ImageProcessing = "image-processing";
}
