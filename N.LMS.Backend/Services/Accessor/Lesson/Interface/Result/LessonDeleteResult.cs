using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.Lesson.Interface.Result;

public sealed record LessonDeleteResult : DeleteResultBase
{
    public Guid LessonId { get; init; }
}
