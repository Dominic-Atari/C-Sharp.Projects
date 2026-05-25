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
    private async Task<LessonStoreResult> Handle(LessonStoreRequest request)
    {
        await using var db = DatabaseFactory.CreateContext();
        var now = DateTime.UtcNow;
        var context = ProxyForService<IContextUtility>().GetContext();

        var moduleExists = await db.Modules.AnyAsync(m => m.ModuleId == request.ModuleId && !m.Deleted);
        if (!moduleExists)
        {
            throw new NotFoundException(context, nameof(DB.Module), nameof(DB.Module.ModuleId), request.ModuleId);
        }

        DB.Lesson entity;
        if (request.LessonId is { } id)
        {
            entity = await db.Lessons.SingleOrDefaultAsync(l => l.LessonId == id && !l.Deleted)
                     ?? throw new NotFoundException(context, nameof(DB.Lesson), nameof(DB.Lesson.LessonId), id);

            entity.Title = request.Title;
            entity.Type = (DB.LessonType)request.Type;
            entity.ContentUrl = request.ContentUrl;
            entity.ContentBody = request.ContentBody;
            entity.DurationMinutes = request.DurationMinutes;
            if (request.SortOrder is { } so) entity.SortOrder = so;
            entity.ModifiedUtc = now;
        }
        else
        {
            var sortOrder = request.SortOrder
                ?? (await db.Lessons
                    .Where(l => l.ModuleId == request.ModuleId && !l.Deleted)
                    .Select(l => (int?)l.SortOrder).MaxAsync() ?? -1) + 1;

            entity = new DB.Lesson
            {
                LessonId = Guid.NewGuid(),
                ModuleId = request.ModuleId,
                Title = request.Title,
                Type = (DB.LessonType)request.Type,
                ContentUrl = request.ContentUrl,
                ContentBody = request.ContentBody,
                DurationMinutes = request.DurationMinutes,
                SortOrder = sortOrder,
                CreatedUtc = now,
                ModifiedUtc = now
            };
            db.Lessons.Add(entity);
        }

        await db.SaveChangesAsync();

        return new LessonStoreResult
        {
            Lesson = new LessonInfo
            {
                LessonId = entity.LessonId,
                ModuleId = entity.ModuleId,
                Title = entity.Title,
                Type = (LessonType)entity.Type,
                ContentUrl = entity.ContentUrl,
                ContentBody = entity.ContentBody,
                DurationMinutes = entity.DurationMinutes,
                SortOrder = entity.SortOrder,
                CreatedUtc = entity.CreatedUtc,
                ModifiedUtc = entity.ModifiedUtc
            }
        };
    }
}
