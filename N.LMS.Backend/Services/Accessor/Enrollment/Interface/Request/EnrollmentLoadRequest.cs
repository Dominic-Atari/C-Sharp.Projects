using N.LMS.Common.Interface.Request;

namespace N.LMS.Accessor.Enrollment.Interface.Request;

public sealed record EnrollmentLoadRequest : LoadRequestBase
{
    public Guid EnrollmentId { get; init; }
}
