using Microsoft.EntityFrameworkCore;
using N.LMS.Accessor.Enrollment.Interface.Model;
using N.LMS.Accessor.Enrollment.Interface.Request;
using N.LMS.Accessor.Enrollment.Interface.Result;
using N.LMS.Database.Factory;

namespace N.LMS.Accessor.Enrollment.Service;

internal sealed partial class EnrollmentAccessor
{
    private async Task<EnrollmentListResult> Handle(EnrollmentsByUserRequest request)
    {
        await using var db = DatabaseFactory.CreateContext();

        var rows = await db.Enrollments
            .Where(e => e.UserId == request.UserId && !e.Deleted)
            .OrderByDescending(e => e.EnrolledUtc)
            .Select(e => new EnrollmentInfo
            {
                EnrollmentId = e.EnrollmentId,
                UserId = e.UserId,
                UserName = e.User != null ? e.User.FirstName + " " + e.User.LastName : null,
                UserEmail = e.User != null ? e.User.Email : null,
                CourseId = e.CourseId,
                CourseTitle = e.Course != null ? e.Course.Title : null,
                Role = (EnrollmentRole)e.Role,
                Status = (EnrollmentStatus)e.Status,
                EnrolledUtc = e.EnrolledUtc,
                CompletedUtc = e.CompletedUtc,
                ProgressPercent = e.ProgressPercent,
                CreatedUtc = e.CreatedUtc,
                ModifiedUtc = e.ModifiedUtc
            })
            .ToListAsync();

        return new EnrollmentListResult { Enrollments = rows };
    }
}
