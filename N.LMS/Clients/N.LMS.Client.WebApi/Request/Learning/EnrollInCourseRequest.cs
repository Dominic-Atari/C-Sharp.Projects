namespace N.LMS.Client.WebApi.Request.Learning;

public sealed record EnrollInCourseRequest : RequestBase
{
    public Guid CourseId { get; init; }
}
