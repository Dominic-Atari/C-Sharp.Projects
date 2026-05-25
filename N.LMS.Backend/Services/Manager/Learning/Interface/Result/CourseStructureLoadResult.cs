using N.LMS.Common.Interface.Result;
using N.LMS.Manager.Learning.Interface.Model;

namespace N.LMS.Manager.Learning.Interface.Result;

public sealed record CourseStructureLoadResult : LoadResultBase
{
    public CourseStructure? Structure { get; init; }
}
