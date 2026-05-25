using N.LMS.Accessor.Lesson.Interface.Model;
using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.Lesson.Interface.Result;

public sealed record LessonListResult : LoadResultBase
{
    public IReadOnlyList<LessonInfo> Lessons { get; init; } = Array.Empty<LessonInfo>();
}
