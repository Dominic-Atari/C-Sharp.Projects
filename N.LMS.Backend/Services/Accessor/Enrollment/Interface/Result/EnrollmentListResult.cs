using N.LMS.Accessor.Enrollment.Interface.Model;
using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.Enrollment.Interface.Result;

public sealed record EnrollmentListResult : LoadResultBase
{
    public IReadOnlyList<EnrollmentInfo> Enrollments { get; init; } = Array.Empty<EnrollmentInfo>();
}
