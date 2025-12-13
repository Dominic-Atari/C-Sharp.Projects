namespace Nile.Database.Entities;

public class Notification
{
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string NotificationType { get; set; } = string.Empty; // Like, Comment, Follow, Mention
    public string? EntityType { get; set; } // Post, Comment, User
    public Guid? EntityId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public User? Actor { get; set; }
}
