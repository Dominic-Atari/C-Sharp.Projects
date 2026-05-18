namespace Nile.Database.Entities;

public class Lesson
{
    public Guid LessonId { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = null!;
    public string? BodyMarkdown { get; set; }
    public string? ResourceUrl { get; set; }
    public int Order { get; set; }
    public int? DurationMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Course Course { get; set; } = null!;
}
