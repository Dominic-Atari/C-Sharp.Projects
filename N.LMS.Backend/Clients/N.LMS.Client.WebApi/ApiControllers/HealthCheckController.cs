using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using N.LMS.Client.WebApi.Response.Admin;
using N.LMS.Common.Interface.Context;
using N.LMS.Common.Service;
using N.LMS.Manager.Admin.Interface;
using N.LMS.Manager.Admin.Interface.Request;
using N.LMS.Manager.Admin.Interface.Result;
using N.LMS.Manager.Learning.Interface;
using N.LMS.Manager.Learning.Interface.Request;
using N.LMS.Manager.Learning.Interface.Result;

namespace N.LMS.Client.WebApi.ApiControllers;

[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public class HealthCheckController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        using var proxy = new ServiceProxyGenerator(new AnonymousContext());

        var adminTask = proxy.ProxyForService<IAdminManager>()
            .HealthCheck(new AdminHealthCheckRequest());
        var learningTask = proxy.ProxyForService<ILearningManager>()
            .HealthCheck(new LearningHealthCheckRequest());

        await Task.WhenAll(adminTask, learningTask);
        var admin = (AdminHealthCheckResult)adminTask.Result;
        var learning = (LearningHealthCheckResult)learningTask.Result;

        var response = new HealthCheckResponse
        {
            Healthy = admin.Healthy && learning.Healthy,
            CheckedAtUtc = DateTime.UtcNow,
            UserAccessorHealthy = admin.UserAccessorHealthy,
            SystemAccessorHealthy = admin.SystemAccessorHealthy,
            CourseAccessorHealthy = learning.CourseAccessorHealthy,
            EnrollmentAccessorHealthy = learning.EnrollmentAccessorHealthy,
            ModuleAccessorHealthy = learning.ModuleAccessorHealthy,
            LessonAccessorHealthy = learning.LessonAccessorHealthy,
            Message = admin.Message ?? learning.Message
        };

        return CreateActionResult(response);
    }
}
