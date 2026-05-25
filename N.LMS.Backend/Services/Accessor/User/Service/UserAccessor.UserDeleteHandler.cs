using Microsoft.EntityFrameworkCore;
using N.LMS.Accessor.User.Interface.Request;
using N.LMS.Accessor.User.Interface.Result;
using N.LMS.Common.Interface.Context;
using N.LMS.Common.Interface.Exceptions;
using N.LMS.Database.Factory;

namespace N.LMS.Accessor.User.Service;

internal sealed partial class UserAccessor
{
    private async Task<UserDeleteResult> Handle(UserDeleteRequest request)
    {
        await using var db = DatabaseFactory.CreateContext();

        var entity = await db.Users.SingleOrDefaultAsync(u => u.UserId == request.UserId && !u.Deleted);
        if (entity is null)
        {
            var context = ProxyForService<IContextUtility>().GetContext();
            throw new NotFoundException(context, nameof(DB.User), nameof(DB.User.UserId), request.UserId);
        }

        entity.Deleted = true;
        entity.ModifiedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return new UserDeleteResult { UserId = entity.UserId };
    }
}
