using N.LMS.Common.Interface.Request;

namespace N.LMS.Accessor.Lesson.Interface.Request;

public sealed record LessonLoadRequest : LoadRequestBase
{
    public Guid LessonId { get; init; }
}
