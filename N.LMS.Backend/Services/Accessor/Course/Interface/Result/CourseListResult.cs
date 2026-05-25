using N.LMS.Accessor.Course.Interface.Model;
using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.Course.Interface.Result;

public sealed record CourseListResult : LoadResultBase
{
    public IReadOnlyList<CourseInfo> Courses { get; init; } = Array.Empty<CourseInfo>();
}
