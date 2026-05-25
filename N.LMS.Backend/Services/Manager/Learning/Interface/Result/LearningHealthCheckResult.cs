using N.LMS.Common.Interface.Result;

namespace N.LMS.Manager.Learning.Interface.Result;

public sealed record LearningHealthCheckResult : HealthCheckResultBase
{
    public bool CourseAccessorHealthy { get; init; }
    public bool EnrollmentAccessorHealthy { get; init; }
    public bool ModuleAccessorHealthy { get; init; }
    public bool LessonAccessorHealthy { get; init; }
}
