using Microsoft.EntityFrameworkCore;
using N.LMS.Accessor.Enrollment.Interface.Request;
using N.LMS.Accessor.Enrollment.Interface.Result;
using N.LMS.Common.Interface.Context;
using N.LMS.Common.Interface.Exceptions;
using N.LMS.Database.Factory;

namespace N.LMS.Accessor.Enrollment.Service;

internal sealed partial class EnrollmentAccessor
{
    private async Task<EnrollmentDeleteResult> Handle(EnrollmentDeleteRequest request)
    {
        await using var db = DatabaseFactory.CreateContext();

        var entity = await db.Enrollments.SingleOrDefaultAsync(e => e.EnrollmentId == request.EnrollmentId && !e.Deleted);
        if (entity is null)
        {
            var context = ProxyForService<IContextUtility>().GetContext();
            throw new NotFoundException(context, nameof(DB.Enrollment), nameof(DB.Enrollment.EnrollmentId), request.EnrollmentId);
        }

        entity.Deleted = true;
        entity.ModifiedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return new EnrollmentDeleteResult { EnrollmentId = entity.EnrollmentId };
    }
}
