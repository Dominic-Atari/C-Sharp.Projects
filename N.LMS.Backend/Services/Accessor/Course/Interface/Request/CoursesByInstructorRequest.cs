using N.LMS.Common.Interface.Request;

namespace N.LMS.Accessor.Course.Interface.Request;

public sealed record CoursesByInstructorRequest : LoadRequestBase
{
    public Guid InstructorId { get; init; }
}
