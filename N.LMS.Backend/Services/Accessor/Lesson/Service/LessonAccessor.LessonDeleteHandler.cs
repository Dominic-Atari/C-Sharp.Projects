using Microsoft.EntityFrameworkCore;
using N.LMS.Accessor.Lesson.Interface.Request;
using N.LMS.Accessor.Lesson.Interface.Result;
using N.LMS.Common.Interface.Context;
using N.LMS.Common.Interface.Exceptions;
using N.LMS.Database.Factory;

namespace N.LMS.Accessor.Lesson.Service;

internal sealed partial class LessonAccessor
{
    private async Task<LessonDeleteResult> Handle(LessonDeleteRequest request)
    {
        await using var db = DatabaseFactory.CreateContext();

        var entity = await db.Lessons.SingleOrDefaultAsync(l => l.LessonId == request.LessonId && !l.Deleted);
        if (entity is null)
        {
            var context = ProxyForService<IContextUtility>().GetContext();
            throw new NotFoundException(context, nameof(DB.Lesson), nameof(DB.Lesson.LessonId), request.LessonId);
        }

        entity.Deleted = true;
        entity.ModifiedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return new LessonDeleteResult { LessonId = entity.LessonId };
    }
}
