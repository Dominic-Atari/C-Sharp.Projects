using N.LMS.Accessor.Enrollment.Interface.Model;
using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.Enrollment.Interface.Result;

public sealed record EnrollmentLoadResult : LoadResultBase
{
    public EnrollmentInfo? Enrollment { get; init; }
}
