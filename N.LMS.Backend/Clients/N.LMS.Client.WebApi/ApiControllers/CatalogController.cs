using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using N.LMS.Client.WebApi.Extension;
using N.LMS.Client.WebApi.Request.Learning;
using N.LMS.Client.WebApi.Response.Learning;
using N.LMS.Common.Interface.Context;
using N.LMS.Common.Service;
using N.LMS.Manager.Learning.Interface;
using LearnReq = N.LMS.Manager.Learning.Interface.Request;
using LearnRes = N.LMS.Manager.Learning.Interface.Result;

namespace N.LMS.Client.WebApi.ApiControllers;

[ApiController]
[Route("[controller]")]
public class CatalogController : ApiControllerBase
{
    [HttpPost("Courses")]
    [AllowAnonymous]
    public async Task<IActionResult> Courses([FromBody] CourseCatalogRequest request)
    {
        using var proxy = new ServiceProxyGenerator(new AnonymousContext());

        var result = (LearnRes.CourseCatalogLoadResult)await proxy.ProxyForService<ILearningManager>()
            .Load(new LearnReq.CourseCatalogLoadRequest { InstructorId = request.InstructorId });

        return CreateActionResult(Mapper.Map<CourseCatalogResponse>(result));
    }

    [HttpPost("Structure")]
    [AllowAnonymous]
    public async Task<IActionResult> Structure([FromBody] CourseStructureRequest request)
    {
        using var proxy = new ServiceProxyGenerator(new AnonymousContext());

        var result = (LearnRes.CourseStructureLoadResult)await proxy.ProxyForService<ILearningManager>()
            .Load(new LearnReq.CourseStructureLoadRequest { CourseId = request.CourseId });

        return CreateActionResult(Mapper.Map<CourseStructureResponse>(result));
    }

    [HttpPost("Enroll")]
    [Authorize]
    public async Task<IActionResult> Enroll([FromBody] EnrollInCourseRequest request)
    {
        using var proxy = new ServiceProxyGenerator(HttpContext.ToWebContext());

        var result = (LearnRes.EnrollInCourseResult)await proxy.ProxyForService<ILearningManager>()
            .Store(new LearnReq.EnrollInCourseRequest { CourseId = request.CourseId });

        return CreateActionResult(Mapper.Map<EnrollInCourseResponse>(result));
    }

    [HttpGet("MyEnrollments")]
    [Authorize]
    public async Task<IActionResult> MyEnrollments()
    {
        using var proxy = new ServiceProxyGenerator(HttpContext.ToWebContext());

        var result = (LearnRes.MyEnrollmentsLoadResult)await proxy.ProxyForService<ILearningManager>()
            .Load(new LearnReq.MyEnrollmentsLoadRequest());

        return CreateActionResult(Mapper.Map<MyEnrollmentsResponse>(result));
    }
}
