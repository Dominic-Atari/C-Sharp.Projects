namespace Nile.Database.Entities;

public class Course
{
    public Guid CourseId { get; set; }
    public Guid SchoolId { get; set; }
    public Guid? SubjectId { get; set; }
    public string Title { get; set; } = null!;
    public string? Summary { get; set; }
    public string? Level { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public School School { get; set; } = null!;
    public Subject? Subject { get; set; }
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
