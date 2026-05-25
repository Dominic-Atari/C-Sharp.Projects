using N.LMS.Accessor.Lesson.Interface.Model;
using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.Lesson.Interface.Result;

public sealed record LessonLoadResult : LoadResultBase
{
    public LessonInfo? Lesson { get; init; }
}
