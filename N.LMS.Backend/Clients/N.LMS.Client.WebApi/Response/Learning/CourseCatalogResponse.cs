using N.LMS.Manager.Learning.Interface.Model;

namespace N.LMS.Client.WebApi.Response.Learning;

public sealed record CourseCatalogResponse : ResponseBase
{
    public IReadOnlyList<CatalogCourse> Courses { get; init; } = Array.Empty<CatalogCourse>();
}
