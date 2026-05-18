using Nile.Common;

namespace Nile.Database.Entities;

public class UserRelationship
{
    public Guid RelationshipId { get; set; }
    public Guid FollowerUserId { get; set; }
    public Guid FollowingUserId { get; set; }
    public UserRelationshipType RelationshipType { get; set; } = UserRelationshipType.Follow; // Follow, Block, Friend
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User Follower { get; set; } = null!;
    public User Following { get; set; } = null!;
}
