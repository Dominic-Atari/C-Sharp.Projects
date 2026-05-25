using Microsoft.EntityFrameworkCore;
using N.LMS.Accessor.Lesson.Interface.Model;
using N.LMS.Accessor.Lesson.Interface.Request;
using N.LMS.Accessor.Lesson.Interface.Result;
using N.LMS.Database.Factory;

namespace N.LMS.Accessor.Lesson.Service;

internal sealed partial class LessonAccessor
{
    private async Task<LessonListResult> Handle(LessonsByModuleRequest request)
    {
        await using var db = DatabaseFactory.CreateContext();

        var rows = await db.Lessons
            .Where(l => l.ModuleId == request.ModuleId && !l.Deleted)
            .OrderBy(l => l.SortOrder)
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
            .ToListAsync();

        return new LessonListResult { Lessons = rows };
    }
}
