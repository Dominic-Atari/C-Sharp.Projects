using System.Threading.Tasks;

namespace Nile.Utilities
{
    // No-op implementation for local/dev when a real social feed provider is not configured
    public sealed class NoOpSocialFeedUtility : ISocialFeedUtility
    {
        public Task Comment(string commentExternalId, string postExternalId, string commentId, string commenterId, string postOwnerId)
            => Task.CompletedTask;

        public Task DeleteNotification(string userId, string entityId)
            => Task.CompletedTask;

        public Task DeletePost(string userId, string postId)
            => Task.CompletedTask;

        public Task DeleteComment(string commentId)
            => Task.CompletedTask;

        public Task Follow(string sourceUserId, params string[] targetUserIds)
            => Task.CompletedTask;

        public Task<string> Notify(string notificationType, string externalId, string userId)
            => Task.FromResult("noop");

        public Task<string> Post(string userId, string postId)
            => Task.FromResult("noop");

        public Task Unfollow(string sourceUserId, string targetUserId)
            => Task.CompletedTask;
    }
}
