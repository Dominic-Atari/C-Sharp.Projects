using N.LMS.Common.Interface.Request;

namespace N.LMS.Accessor.Enrollment.Interface.Request;

public sealed record EnrollmentDeleteRequest : DeleteRequestBase
{
    public Guid EnrollmentId { get; init; }
}
