using N.LMS.Common.Interface.Request;

namespace N.LMS.Accessor.Enrollment.Interface.Request;

public sealed record EnrollmentsByUserRequest : LoadRequestBase
{
    public Guid UserId { get; init; }
}
