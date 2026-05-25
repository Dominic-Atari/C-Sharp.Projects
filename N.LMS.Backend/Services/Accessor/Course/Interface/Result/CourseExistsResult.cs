using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.Course.Interface.Result;

public sealed record CourseExistsResult : LoadResultBase
{
    public bool Exists { get; init; }
}
