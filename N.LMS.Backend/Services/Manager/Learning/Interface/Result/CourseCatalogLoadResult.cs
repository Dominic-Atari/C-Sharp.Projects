using N.LMS.Common.Interface.Result;
using N.LMS.Manager.Learning.Interface.Model;

namespace N.LMS.Manager.Learning.Interface.Result;

public sealed record CourseCatalogLoadResult : LoadResultBase
{
    public IReadOnlyList<CatalogCourse> Courses { get; init; } = Array.Empty<CatalogCourse>();
}
