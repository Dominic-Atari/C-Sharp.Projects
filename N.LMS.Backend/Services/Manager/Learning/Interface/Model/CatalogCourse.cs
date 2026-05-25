namespace N.LMS.Manager.Learning.Interface.Model;

public sealed record CatalogCourse
{
    public Guid CourseId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? InstructorName { get; init; }
    public DateTime? PublishedUtc { get; init; }
}
