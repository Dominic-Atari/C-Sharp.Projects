using N.LMS.Common.Interface.Result;
using N.LMS.Manager.Learning.Interface.Model;

namespace N.LMS.Manager.Learning.Interface.Result;

public sealed record MyEnrollmentsLoadResult : LoadResultBase
{
    public IReadOnlyList<MyEnrollment> Enrollments { get; init; } = Array.Empty<MyEnrollment>();
}
