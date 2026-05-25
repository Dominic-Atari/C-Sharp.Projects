using Microsoft.EntityFrameworkCore;
using N.LMS.Accessor.Enrollment.Interface.Model;
using N.LMS.Accessor.Enrollment.Interface.Request;
using N.LMS.Accessor.Enrollment.Interface.Result;
using N.LMS.Common.Interface.Context;
using N.LMS.Common.Interface.Exceptions;
using N.LMS.Database.Factory;

namespace N.LMS.Accessor.Enrollment.Service;

internal sealed partial class EnrollmentAccessor
{
    private async Task<EnrollmentStoreResult> Handle(EnrollmentStoreRequest request)
    {
        await using var db = DatabaseFactory.CreateContext();
        var now = DateTime.UtcNow;
        var context = ProxyForService<IContextUtility>().GetContext();

        var userExists = await db.Users.AnyAsync(u => u.UserId == request.UserId && !u.Deleted);
        if (!userExists)
        {
            throw new NotFoundException(context, nameof(DB.User), nameof(DB.User.UserId), request.UserId);
        }

        var courseExists = await db.Courses.AnyAsync(c => c.CourseId == request.CourseId && !c.Deleted);
        if (!courseExists)
        {
            throw new NotFoundException(context, nameof(DB.Course), nameof(DB.Course.CourseId), request.CourseId);
        }

        DB.Enrollment entity;
        if (request.EnrollmentId is { } id)
        {
            entity = await db.Enrollments.SingleOrDefaultAsync(e => e.EnrollmentId == id && !e.Deleted)
                     ?? throw new NotFoundException(context, nameof(DB.Enrollment), nameof(DB.Enrollment.EnrollmentId), id);

            entity.Role = (DB.EnrollmentRole)request.Role;
            entity.Status = (DB.EnrollmentStatus)request.Status;
            if (request.ProgressPercent is { } p)
            {
                entity.ProgressPercent = p;
            }
            if (entity.Status == DB.EnrollmentStatus.Completed && entity.CompletedUtc is null)
            {
                entity.CompletedUtc = now;
            }
            entity.ModifiedUtc = now;
        }
        else
        {
            var alreadyEnrolled = await db.Enrollments
                .AnyAsync(e => e.UserId == request.UserId && e.CourseId == request.CourseId && !e.Deleted);
            if (alreadyEnrolled)
            {
                throw new ConflictException(context, nameof(DB.Enrollment), nameof(DB.Enrollment.UserId),
                    "User is already enrolled in this course.");
            }

            entity = new DB.Enrollment
            {
                EnrollmentId = Guid.NewGuid(),
                UserId = request.UserId,
                CourseId = request.CourseId,
                Role = (DB.EnrollmentRole)request.Role,
                Status = (DB.EnrollmentStatus)request.Status,
                EnrolledUtc = now,
                ProgressPercent = request.ProgressPercent ?? 0m,
                CreatedUtc = now,
                ModifiedUtc = now
            };
            db.Enrollments.Add(entity);
        }

        await db.SaveChangesAsync();

        var enriched = await db.Enrollments
            .Where(e => e.EnrollmentId == entity.EnrollmentId)
            .Select(e => new
            {
                UserName = e.User != null ? e.User.FirstName + " " + e.User.LastName : null,
                UserEmail = e.User != null ? e.User.Email : null,
                CourseTitle = e.Course != null ? e.Course.Title : null
            })
            .SingleAsync();

        return new EnrollmentStoreResult
        {
            Enrollment = new EnrollmentInfo
            {
                EnrollmentId = entity.EnrollmentId,
                UserId = entity.UserId,
                UserName = enriched.UserName,
                UserEmail = enriched.UserEmail,
                CourseId = entity.CourseId,
                CourseTitle = enriched.CourseTitle,
                Role = (EnrollmentRole)entity.Role,
                Status = (EnrollmentStatus)entity.Status,
                EnrolledUtc = entity.EnrolledUtc,
                CompletedUtc = entity.CompletedUtc,
                ProgressPercent = entity.ProgressPercent,
                CreatedUtc = entity.CreatedUtc,
                ModifiedUtc = entity.ModifiedUtc
            }
        };
    }
}
