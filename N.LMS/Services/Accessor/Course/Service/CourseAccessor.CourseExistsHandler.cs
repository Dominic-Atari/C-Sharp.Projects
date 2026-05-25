using Microsoft.EntityFrameworkCore;
using N.LMS.Accessor.Course.Interface.Request;
using N.LMS.Accessor.Course.Interface.Result;
using N.LMS.Database.Factory;

namespace N.LMS.Accessor.Course.Service;

internal sealed partial class CourseAccessor
{
    private async Task<CourseExistsResult> Handle(CourseExistsRequest request)
    {
        await using var db = DatabaseFactory.CreateContext();
        var exists = await db.Courses.AnyAsync(c => c.CourseId == request.CourseId && !c.Deleted);
        return new CourseExistsResult { Exists = exists };
    }
}
