namespace N.LMS.Accessor.Lesson.Interface.Model;

public sealed record LessonInfo
{
    public Guid LessonId { get; init; }
    public Guid ModuleId { get; init; }
    public string Title { get; init; } = string.Empty;
    public LessonType Type { get; init; }
    public string? ContentUrl { get; init; }
    public string? ContentBody { get; init; }
    public int? DurationMinutes { get; init; }
    public int SortOrder { get; init; }
    public DateTime CreatedUtc { get; init; }
    public DateTime ModifiedUtc { get; init; }
}
