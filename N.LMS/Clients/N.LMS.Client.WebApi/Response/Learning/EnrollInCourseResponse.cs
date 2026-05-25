using N.LMS.Manager.Learning.Interface.Model;

namespace N.LMS.Client.WebApi.Response.Learning;

public sealed record EnrollInCourseResponse : ResponseBase
{
    public required MyEnrollment Enrollment { get; init; }
}
