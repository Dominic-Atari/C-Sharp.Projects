namespace Nile.Utilities;

public interface ISocialFeedUtility
{
    Task Comment(string commentExternalId, string postExternalId, string commentId, string commenterId, string postOwnerId);

    Task DeleteNotification(string userId, string entityId);

    Task DeletePost(string userId, string postId);

    Task DeleteComment(string commentId);

    Task Follow(string sourceUserId, params string[] targetUserIds);

    Task<string> Notify(string notificationType, string externalId, string userId);

    Task<string> Post(string userId, string postId);

    Task Unfollow(string sourceUserId, string targetUserId);
}