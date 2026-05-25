namespace N.LMS.Client.WebApi.Response.Admin;

public sealed record HealthCheckResponse : ResponseBase
{
    public bool Healthy { get; init; }
    public DateTime CheckedAtUtc { get; init; }
    public bool? UserAccessorHealthy { get; init; }
    public bool? SystemAccessorHealthy { get; init; }
    public bool? CourseAccessorHealthy { get; init; }
    public bool? EnrollmentAccessorHealthy { get; init; }
    public bool? ModuleAccessorHealthy { get; init; }
    public bool? LessonAccessorHealthy { get; init; }
    public string? Message { get; init; }
}
