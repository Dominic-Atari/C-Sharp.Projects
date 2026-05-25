using Microsoft.EntityFrameworkCore;
using N.LMS.Accessor.Course.Interface.Model;
using N.LMS.Accessor.Course.Interface.Request;
using N.LMS.Accessor.Course.Interface.Result;
using N.LMS.Common.Interface.Context;
using N.LMS.Common.Interface.Exceptions;
using N.LMS.Database.Factory;

namespace N.LMS.Accessor.Course.Service;

internal sealed partial class CourseAccessor
{
    private async Task<CourseLoadResult> Handle(CourseLoadRequest request)
    {
        await using var db = DatabaseFactory.CreateContext();

        var row = await db.Courses
            .Where(c => c.CourseId == request.CourseId && !c.Deleted)
            .Select(c => new
            {
                c.CourseId,
                c.Title,
                c.Description,
                c.InstructorId,
                InstructorName = c.Instructor != null ? c.Instructor.FirstName + " " + c.Instructor.LastName : null,
                c.Status,
                c.PublishedUtc,
                c.CreatedUtc,
                c.ModifiedUtc
            })
            .SingleOrDefaultAsync();

        if (row is null)
        {
            var context = ProxyForService<IContextUtility>().GetContext();
            throw new NotFoundException(context, nameof(DB.Course), nameof(DB.Course.CourseId), request.CourseId);
        }

        return new CourseLoadResult
        {
            Course = new CourseInfo
            {
                CourseId = row.CourseId,
                Title = row.Title,
                Description = row.Description,
                InstructorId = row.InstructorId,
                InstructorName = row.InstructorName,
                Status = (CourseStatus)row.Status,
                PublishedUtc = row.PublishedUtc,
                CreatedUtc = row.CreatedUtc,
                ModifiedUtc = row.ModifiedUtc
            }
        };
    }
}
