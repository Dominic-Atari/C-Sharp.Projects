namespace N.LMS.Accessor.Course.Interface.Model;

public sealed record CourseInfo
{
    public Guid CourseId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid InstructorId { get; init; }
    public string? InstructorName { get; init; }
    public CourseStatus Status { get; init; }
    public DateTime? PublishedUtc { get; init; }
    public DateTime CreatedUtc { get; init; }
    public DateTime ModifiedUtc { get; init; }
}
