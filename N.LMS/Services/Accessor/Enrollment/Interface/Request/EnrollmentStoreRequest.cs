using N.LMS.Accessor.Enrollment.Interface.Model;
using N.LMS.Common.Interface.Request;

namespace N.LMS.Accessor.Enrollment.Interface.Request;

public sealed record EnrollmentStoreRequest : StoreRequestBase
{
    public Guid? EnrollmentId { get; init; }
    public Guid UserId { get; init; }
    public Guid CourseId { get; init; }
    public EnrollmentRole Role { get; init; } = EnrollmentRole.Student;
    public EnrollmentStatus Status { get; init; } = EnrollmentStatus.Active;
    public decimal? ProgressPercent { get; init; }
}
