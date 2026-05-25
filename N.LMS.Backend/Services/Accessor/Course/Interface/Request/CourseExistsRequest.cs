using N.LMS.Common.Interface.Request;

namespace N.LMS.Accessor.Course.Interface.Request;

public sealed record CourseExistsRequest : LoadRequestBase
{
    public Guid CourseId { get; init; }
}
