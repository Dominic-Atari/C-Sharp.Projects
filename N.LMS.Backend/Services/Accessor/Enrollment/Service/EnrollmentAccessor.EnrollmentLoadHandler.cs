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
    private async Task<EnrollmentLoadResult> Handle(EnrollmentLoadRequest request)
    {
        await using var db = DatabaseFactory.CreateContext();

        var row = await db.Enrollments
            .Where(e => e.EnrollmentId == request.EnrollmentId && !e.Deleted)
            .Select(e => new
            {
                e.EnrollmentId,
                e.UserId,
                UserName = e.User != null ? e.User.FirstName + " " + e.User.LastName : null,
                UserEmail = e.User != null ? e.User.Email : null,
                e.CourseId,
                CourseTitle = e.Course != null ? e.Course.Title : null,
                e.Role,
                e.Status,
                e.EnrolledUtc,
                e.CompletedUtc,
                e.ProgressPercent,
                e.CreatedUtc,
                e.ModifiedUtc
            })
            .SingleOrDefaultAsync();

        if (row is null)
        {
            var context = ProxyForService<IContextUtility>().GetContext();
            throw new NotFoundException(context, nameof(DB.Enrollment), nameof(DB.Enrollment.EnrollmentId), request.EnrollmentId);
        }

        return new EnrollmentLoadResult
        {
            Enrollment = new EnrollmentInfo
            {
                EnrollmentId = row.EnrollmentId,
                UserId = row.UserId,
                UserName = row.UserName,
                UserEmail = row.UserEmail,
                CourseId = row.CourseId,
                CourseTitle = row.CourseTitle,
                Role = (EnrollmentRole)row.Role,
                Status = (EnrollmentStatus)row.Status,
                EnrolledUtc = row.EnrolledUtc,
                CompletedUtc = row.CompletedUtc,
                ProgressPercent = row.ProgressPercent,
                CreatedUtc = row.CreatedUtc,
                ModifiedUtc = row.ModifiedUtc
            }
        };
    }
}
