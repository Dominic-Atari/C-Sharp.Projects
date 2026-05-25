namespace N.LMS.Manager.Learning.Interface.Model;

public sealed record CourseStructure
{
    public Guid CourseId { get; init; }
    public string Title { get; init; } = string.Empty;
    public IReadOnlyList<ModuleNode> Modules { get; init; } = Array.Empty<ModuleNode>();
}

public sealed record ModuleNode
{
    public Guid ModuleId { get; init; }
    public string Title { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public IReadOnlyList<LessonNode> Lessons { get; init; } = Array.Empty<LessonNode>();
}

public sealed record LessonNode
{
    public Guid LessonId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public int? DurationMinutes { get; init; }
}
