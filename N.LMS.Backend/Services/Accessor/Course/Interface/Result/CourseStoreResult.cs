using N.LMS.Accessor.Course.Interface.Model;
using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.Course.Interface.Result;

public sealed record CourseStoreResult : StoreResultBase
{
    public CourseInfo? Course { get; init; }
}
