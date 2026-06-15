namespace Nile.Database.Entities;

public class Like
{
    public Guid LikeId { get; set; }
    public Guid? PostId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Post? Post { get; set; }
    public User User { get; set; } = null!;
}
