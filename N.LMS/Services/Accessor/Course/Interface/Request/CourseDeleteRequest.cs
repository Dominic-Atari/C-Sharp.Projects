using N.LMS.Common.Interface.Request;

namespace N.LMS.Accessor.Course.Interface.Request;

public sealed record CourseDeleteRequest : DeleteRequestBase
{
    public Guid CourseId { get; init; }
}
