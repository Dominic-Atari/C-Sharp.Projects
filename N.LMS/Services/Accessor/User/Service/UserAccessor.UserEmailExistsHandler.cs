using Microsoft.EntityFrameworkCore;
using N.LMS.Accessor.User.Interface.Request;
using N.LMS.Accessor.User.Interface.Result;
using N.LMS.Database.Factory;

namespace N.LMS.Accessor.User.Service;

internal sealed partial class UserAccessor
{
    private async Task<UserExistsResult> Handle(UserEmailExistsRequest request)
    {
        await using var db = DatabaseFactory.CreateContext();

        var normalised = request.Email.Trim().ToLowerInvariant();
        var exists = await db.Users
            .AnyAsync(u => u.Email.ToLower() == normalised && !u.Deleted);

        return new UserExistsResult { Exists = exists };
    }
}
