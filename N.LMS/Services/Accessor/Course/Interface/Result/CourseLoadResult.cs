using N.LMS.Accessor.Course.Interface.Model;
using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.Course.Interface.Result;

public sealed record CourseLoadResult : LoadResultBase
{
    public CourseInfo? Course { get; init; }
}
