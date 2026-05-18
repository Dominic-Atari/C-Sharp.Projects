namespace Nile.Database.Entities;

public class SubLevel
{
    public Guid SubLevelId { get; set; }
    public Guid StageId { get; set; }
    public string Name { get; set; } = null!;
    public string? Label { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation
    public Stage Stage { get; set; } = null!;
}
