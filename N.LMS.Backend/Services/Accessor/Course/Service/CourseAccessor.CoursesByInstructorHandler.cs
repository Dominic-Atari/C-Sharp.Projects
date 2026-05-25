using Microsoft.EntityFrameworkCore;
using N.LMS.Accessor.Course.Interface.Model;
using N.LMS.Accessor.Course.Interface.Request;
using N.LMS.Accessor.Course.Interface.Result;
using N.LMS.Database.Factory;

namespace N.LMS.Accessor.Course.Service;

internal sealed partial class CourseAccessor
{
    private async Task<CourseListResult> Handle(CoursesByInstructorRequest request)
    {
        await using var db = DatabaseFactory.CreateContext();

        var rows = await db.Courses
            .Where(c => c.InstructorId == request.InstructorId && !c.Deleted)
            .OrderByDescending(c => c.PublishedUtc)
            .ThenBy(c => c.Title)
            .Select(c => new CourseInfo
            {
                CourseId = c.CourseId,
                Title = c.Title,
                Description = c.Description,
                InstructorId = c.InstructorId,
                InstructorName = c.Instructor != null ? c.Instructor.FirstName + " " + c.Instructor.LastName : null,
                Status = (CourseStatus)c.Status,
                PublishedUtc = c.PublishedUtc,
                CreatedUtc = c.CreatedUtc,
                ModifiedUtc = c.ModifiedUtc
            })
            .ToListAsync();

        return new CourseListResult { Courses = rows };
    }
}
