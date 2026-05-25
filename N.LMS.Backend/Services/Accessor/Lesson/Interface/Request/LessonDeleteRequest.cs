using N.LMS.Common.Interface.Request;

namespace N.LMS.Accessor.Lesson.Interface.Request;

public sealed record LessonDeleteRequest : DeleteRequestBase
{
    public Guid LessonId { get; init; }
}
