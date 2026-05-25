using Microsoft.EntityFrameworkCore;
using N.LMS.Accessor.Course.Interface.Request;
using N.LMS.Accessor.Course.Interface.Result;
using N.LMS.Common.Interface.Context;
using N.LMS.Common.Interface.Exceptions;
using N.LMS.Database.Factory;

namespace N.LMS.Accessor.Course.Service;

internal sealed partial class CourseAccessor
{
    private async Task<CourseDeleteResult> Handle(CourseDeleteRequest request)
    {
        await using var db = DatabaseFactory.CreateContext();

        var entity = await db.Courses.SingleOrDefaultAsync(c => c.CourseId == request.CourseId && !c.Deleted);
        if (entity is null)
        {
            var context = ProxyForService<IContextUtility>().GetContext();
            throw new NotFoundException(context, nameof(DB.Course), nameof(DB.Course.CourseId), request.CourseId);
        }

        entity.Deleted = true;
        entity.ModifiedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return new CourseDeleteResult { CourseId = entity.CourseId };
    }
}
