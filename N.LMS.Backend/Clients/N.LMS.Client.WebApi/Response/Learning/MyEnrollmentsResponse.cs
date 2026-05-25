using N.LMS.Manager.Learning.Interface.Model;

namespace N.LMS.Client.WebApi.Response.Learning;

public sealed record MyEnrollmentsResponse : ResponseBase
{
    public IReadOnlyList<MyEnrollment> Enrollments { get; init; } = Array.Empty<MyEnrollment>();
}
