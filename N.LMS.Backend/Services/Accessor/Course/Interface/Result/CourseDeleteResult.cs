using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.Course.Interface.Result;

public sealed record CourseDeleteResult : DeleteResultBase
{
    public Guid CourseId { get; init; }
}
