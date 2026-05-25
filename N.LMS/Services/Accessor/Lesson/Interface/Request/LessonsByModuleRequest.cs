using N.LMS.Common.Interface.Request;

namespace N.LMS.Accessor.Lesson.Interface.Request;

public sealed record LessonsByModuleRequest : LoadRequestBase
{
    public Guid ModuleId { get; init; }
}
