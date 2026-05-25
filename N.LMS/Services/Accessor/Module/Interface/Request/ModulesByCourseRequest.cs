using N.LMS.Common.Interface.Request;

namespace N.LMS.Accessor.Module.Interface.Request;

public sealed record ModulesByCourseRequest : LoadRequestBase
{
    public Guid CourseId { get; init; }
}
