namespace N.LMS.Client.WebApi.Request.Learning;

public sealed record CourseCatalogRequest : RequestBase
{
    public Guid? InstructorId { get; init; }
}
