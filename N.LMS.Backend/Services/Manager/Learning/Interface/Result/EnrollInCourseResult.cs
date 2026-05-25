using N.LMS.Common.Interface.Result;
using N.LMS.Manager.Learning.Interface.Model;

namespace N.LMS.Manager.Learning.Interface.Result;

public sealed record EnrollInCourseResult : StoreResultBase
{
    public required MyEnrollment Enrollment { get; init; }
}
