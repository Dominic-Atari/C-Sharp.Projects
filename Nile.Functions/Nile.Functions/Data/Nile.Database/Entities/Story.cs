namespace Nile.Database.Entities;
using System.ComponentModel.DataAnnotations.Schema;

public class Story
{
    public Guid StoryId { get; set; }
    public Guid SchoolId { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? TopicId { get; set; }
    public Guid? SubTopicId { get; set; }
    public string? PromptId { get; set; }
    [Column("User")]
    public string? User { get; set; }
    public string? Payload { get; set; }
    // Optional client-provided correlation id to match optimistic client stories to server rows
    public string? ClientCorrelationId { get; set; }

    // Soft-delete support
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Subject? Subject { get; set; }
    public SubTopic? SubTopic { get; set; }
}
