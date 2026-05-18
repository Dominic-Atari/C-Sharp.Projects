namespace Nile.Database.Entities;

public class Post
{
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? ImageUrl { get; set; }
    public string PostType { get; set; } = "Text"; // Text, Image, Video, Recipe
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
    public int SharesCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
}
