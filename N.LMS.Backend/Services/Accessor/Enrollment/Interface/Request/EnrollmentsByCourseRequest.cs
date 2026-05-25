using N.LMS.Common.Interface.Request;

namespace N.LMS.Accessor.Enrollment.Interface.Request;

public sealed record EnrollmentsByCourseRequest : LoadRequestBase
{
    public Guid CourseId { get; init; }
}
