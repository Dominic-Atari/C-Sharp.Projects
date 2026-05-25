namespace N.LMS.Client.WebApi.Request.Learning;

public sealed record CourseStructureRequest : RequestBase
{
    public Guid CourseId { get; init; }
}
