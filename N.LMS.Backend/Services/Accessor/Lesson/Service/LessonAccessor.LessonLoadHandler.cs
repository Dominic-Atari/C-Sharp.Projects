using Microsoft.EntityFrameworkCore;
using N.LMS.Accessor.Lesson.Interface.Model;
using N.LMS.Accessor.Lesson.Interface.Request;
using N.LMS.Accessor.Lesson.Interface.Result;
using N.LMS.Common.Interface.Context;
using N.LMS.Common.Interface.Exceptions;
using N.LMS.Database.Factory;

namespace N.LMS.Accessor.Lesson.Service;

internal sealed partial class LessonAccessor
{
    private async Task<LessonLoadResult> Handle(LessonLoadRequest request)
    {
        await using var db = DatabaseFactory.CreateContext();

        var row = await db.Lessons
            .Where(l => l.LessonId == request.LessonId && !l.Deleted)
            .Select(l => new LessonInfo
            {
                LessonId = l.LessonId,
                ModuleId = l.ModuleId,
                Title = l.Title,
                Type = (LessonType)l.Type,
                ContentUrl = l.ContentUrl,
                ContentBody = l.ContentBody,
                DurationMinutes = l.DurationMinutes,
                SortOrder = l.SortOrder,
                CreatedUtc = l.CreatedUtc,
                ModifiedUtc = l.ModifiedUtc
            })
            .SingleOrDefaultAsync();

        if (row is null)
        {
            var context = ProxyForService<IContextUtility>().GetContext();
            throw new NotFoundException(context, nameof(DB.Lesson), nameof(DB.Lesson.LessonId), request.LessonId);
        }

        return new LessonLoadResult { Lesson = row };
    }
}
