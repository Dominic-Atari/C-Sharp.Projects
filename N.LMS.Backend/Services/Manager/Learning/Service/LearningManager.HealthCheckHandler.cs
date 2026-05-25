using N.LMS.Manager.Learning.Interface.Request;
using N.LMS.Manager.Learning.Interface.Result;

namespace N.LMS.Manager.Learning.Service;

internal sealed partial class LearningManager
{
    private async Task<LearningHealthCheckResult> Handle(LearningHealthCheckRequest _)
    {
        var probes = await Task.WhenAll(
            Probe(() => ProxyForService<CourseAccessor.ICourseAccessor>().Load(
                new CourseAccessor.Request.CourseExistsRequest { CourseId = Guid.Empty })),
            Probe(() => ProxyForService<EnrollmentAccessor.IEnrollmentAccessor>().Load(
                new EnrollmentAccessor.Request.EnrollmentsByUserRequest { UserId = Guid.Empty })),
            Probe(() => ProxyForService<ModuleAccessor.IModuleAccessor>().Load(
                new ModuleAccessor.Request.ModulesByCourseRequest { CourseId = Guid.Empty })),
            Probe(() => ProxyForService<LessonAccessor.ILessonAccessor>().Load(
                new LessonAccessor.Request.LessonsByModuleRequest { ModuleId = Guid.Empty })));

        var allHealthy = probes.All(p => p);
        return new LearningHealthCheckResult
        {
            Healthy = allHealthy,
            CourseAccessorHealthy = probes[0],
            EnrollmentAccessorHealthy = probes[1],
            ModuleAccessorHealthy = probes[2],
            LessonAccessorHealthy = probes[3],
            Message = allHealthy ? null : "One or more downstream accessors are unreachable.",
            CheckedAtUtc = DateTime.UtcNow
        };
    }

    private static async Task<bool> Probe(Func<Task> probe)
    {
        try { await probe(); return true; }
        catch { return false; }
    }
}
