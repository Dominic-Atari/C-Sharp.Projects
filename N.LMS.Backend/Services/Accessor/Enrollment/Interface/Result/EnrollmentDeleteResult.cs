using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.Enrollment.Interface.Result;

public sealed record EnrollmentDeleteResult : DeleteResultBase
{
    public Guid EnrollmentId { get; init; }
}
