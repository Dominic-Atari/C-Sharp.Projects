namespace Nile.Database.Entities;

using System.ComponentModel.DataAnnotations.Schema;

public class SubTopic
{
    public Guid SubTopicId { get; set; }
    public Guid TopicId { get; set; }
    public string Name { get; set; } = null!;
    public string? Notes { get; set; }

    // Soft-delete support
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Topic Topic { get; set; } = null!;
}
