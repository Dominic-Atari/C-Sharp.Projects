namespace Nile.Database.Entities;

public class Stage
{
    public Guid StageId { get; set; }
    public Guid SchoolId { get; set; }
    public string Name { get; set; } = null!;
    public string? Label { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Optional link to a Topic (backfilled by migration in future)
    public Guid? TopicId { get; set; }

    // Navigation
    public School School { get; set; } = null!;
}
