using N.LMS.Manager.Learning.Interface.Model;
using N.LMS.Manager.Learning.Interface.Request;
using N.LMS.Manager.Learning.Interface.Result;

namespace N.LMS.Manager.Learning.Service;

internal sealed partial class LearningManager
{
    private async Task<CourseCatalogLoadResult> Handle(CourseCatalogLoadRequest request)
    {
        if (request.InstructorId is null)
        {
            return new CourseCatalogLoadResult { Courses = Array.Empty<CatalogCourse>() };
        }

        var accessor = ProxyForService<CourseAccessor.ICourseAccessor>();
        var accessorResult = (CourseAccessor.Result.CourseListResult)await accessor.Load(
            new CourseAccessor.Request.CoursesByInstructorRequest { InstructorId = request.InstructorId.Value });

        var courses = accessorResult.Courses.Select(c => _Mapper.Map<CatalogCourse>(c)).ToList();
        return new CourseCatalogLoadResult { Courses = courses };
    }
}
