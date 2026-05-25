using Microsoft.EntityFrameworkCore;
using N.LMS.Accessor.User.Interface.Request;
using N.LMS.Accessor.User.Interface.Result;
using N.LMS.Database.Factory;

namespace N.LMS.Accessor.User.Service;

internal sealed partial class UserAccessor
{
    private async Task<UserExistsResult> Handle(UserExistsRequest request)
    {
        await using var db = DatabaseFactory.CreateContext();

        var exists = await db.Users
            .AnyAsync(u => u.UserId == request.UserId && !u.Deleted);

        return new UserExistsResult { Exists = exists };
    }
}
