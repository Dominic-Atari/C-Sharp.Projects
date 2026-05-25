using N.LMS.Accessor.Lesson.Interface.Model;
using N.LMS.Common.Interface.Request;

namespace N.LMS.Accessor.Lesson.Interface.Request;

public sealed record LessonStoreRequest : StoreRequestBase
{
    public Guid? LessonId { get; init; }
    public Guid ModuleId { get; init; }
    public string Title { get; init; } = string.Empty;
    public LessonType Type { get; init; } = LessonType.Article;
    public string? ContentUrl { get; init; }
    public string? ContentBody { get; init; }
    public int? DurationMinutes { get; init; }
    public int? SortOrder { get; init; }
}
