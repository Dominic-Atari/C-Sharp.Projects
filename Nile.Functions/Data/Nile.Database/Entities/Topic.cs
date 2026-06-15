namespace Nile.Database.Entities;

using System.ComponentModel.DataAnnotations.Schema;

public class Topic
{
    public Guid TopicId { get; set; }
    public Guid SchoolId { get; set; }
    public Guid SubjectId { get; set; }
    // Subtopic can be null in existing DB rows; make nullable to avoid materialization errors
    public string? Subtopic { get; set; }
    public string? Notes { get; set; }
    public string? Name { get; set; }
    public Guid? ParentTopicId { get; set; }
    // Soft-delete support
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Subject Subject { get; set; } = null!;
    // SubTopics associated with this Topic (soft-deletable)
    public ICollection<SubTopic>? SubTopics { get; set; }
}
