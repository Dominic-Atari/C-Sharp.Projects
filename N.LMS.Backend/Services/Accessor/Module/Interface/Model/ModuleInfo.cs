namespace N.LMS.Accessor.Module.Interface.Model;

public sealed record ModuleInfo
{
    public Guid ModuleId { get; init; }
    public Guid CourseId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int SortOrder { get; init; }
    public int LessonCount { get; init; }
    public DateTime CreatedUtc { get; init; }
    public DateTime ModifiedUtc { get; init; }
}
