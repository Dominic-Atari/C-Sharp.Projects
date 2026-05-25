using Microsoft.EntityFrameworkCore;
using N.LMS.Accessor.User.Interface.Model;
using N.LMS.Accessor.User.Interface.Request;
using N.LMS.Accessor.User.Interface.Result;
using N.LMS.Common.Interface.Context;
using N.LMS.Common.Interface.Exceptions;
using N.LMS.Database.Factory;

namespace N.LMS.Accessor.User.Service;

internal sealed partial class UserAccessor
{
    private async Task<UserLoadResult> Handle(UserLoadRequest request)
    {
        await using var db = DatabaseFactory.CreateContext();

        var row = await db.Users
            .Where(u => u.UserId == request.UserId && !u.Deleted)
            .Select(u => new
            {
                u.UserId,
                u.FirstName,
                u.LastName,
                u.Email,
                u.Role,
                u.CohortId,
                CohortName = u.Cohort != null ? u.Cohort.Name : null,
                u.CreatedUtc,
                u.ModifiedUtc
            })
            .SingleOrDefaultAsync();

        if (row is null)
        {
            var context = ProxyForService<IContextUtility>().GetContext();
            throw new NotFoundException(context, nameof(DB.User), nameof(DB.User.UserId), request.UserId);
        }

        return new UserLoadResult
        {
            User = new UserInfo
            {
                UserId = row.UserId,
                FirstName = row.FirstName,
                LastName = row.LastName,
                Email = row.Email,
                Role = (UserRole)row.Role,
                CohortId = row.CohortId,
                CohortName = row.CohortName,
                CreatedUtc = row.CreatedUtc,
                ModifiedUtc = row.ModifiedUtc
            }
        };
    }
}
