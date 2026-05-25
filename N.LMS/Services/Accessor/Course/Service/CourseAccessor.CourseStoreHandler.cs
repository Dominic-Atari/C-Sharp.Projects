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
    private async Task<CourseStoreResult> Handle(CourseStoreRequest request)
    {
        await using var db = DatabaseFactory.CreateContext();
        var now = DateTime.UtcNow;
        var context = ProxyForService<IContextUtility>().GetContext();

        var instructorExists = await db.Users.AnyAsync(u => u.UserId == request.InstructorId && !u.Deleted);
        if (!instructorExists)
        {
            throw new NotFoundException(context, nameof(DB.User), nameof(DB.User.UserId), request.InstructorId);
        }

        DB.Course entity;
        if (request.CourseId is { } id)
        {
            entity = await db.Courses.SingleOrDefaultAsync(c => c.CourseId == id && !c.Deleted)
                     ?? throw new NotFoundException(context, nameof(DB.Course), nameof(DB.Course.CourseId), id);

            var wasPublished = entity.Status == DB.CourseStatus.Published;
            entity.Title = request.Title;
            entity.Description = request.Description;
            entity.InstructorId = request.InstructorId;
            entity.Status = (DB.CourseStatus)request.Status;
            entity.ModifiedUtc = now;
            if (!wasPublished && entity.Status == DB.CourseStatus.Published)
            {
                entity.PublishedUtc = now;
            }
        }
        else
        {
            entity = new DB.Course
            {
                CourseId = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                InstructorId = request.InstructorId,
                Status = (DB.CourseStatus)request.Status,
                PublishedUtc = request.Status == CourseStatus.Published ? now : null,
                CreatedUtc = now,
                ModifiedUtc = now
            };
            db.Courses.Add(entity);
        }

        await db.SaveChangesAsync();

        var instructorName = await db.Users
            .Where(u => u.UserId == entity.InstructorId)
            .Select(u => u.FirstName + " " + u.LastName)
            .SingleOrDefaultAsync();

        return new CourseStoreResult
        {
            Course = new CourseInfo
            {
                CourseId = entity.CourseId,
                Title = entity.Title,
                Description = entity.Description,
                InstructorId = entity.InstructorId,
                InstructorName = instructorName,
                Status = (CourseStatus)entity.Status,
                PublishedUtc = entity.PublishedUtc,
                CreatedUtc = entity.CreatedUtc,
                ModifiedUtc = entity.ModifiedUtc
            }
        };
    }
}
