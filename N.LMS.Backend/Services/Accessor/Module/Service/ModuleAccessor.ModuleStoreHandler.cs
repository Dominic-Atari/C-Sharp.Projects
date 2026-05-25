using Microsoft.EntityFrameworkCore;
using N.LMS.Accessor.Module.Interface.Model;
using N.LMS.Accessor.Module.Interface.Request;
using N.LMS.Accessor.Module.Interface.Result;
using N.LMS.Common.Interface.Context;
using N.LMS.Common.Interface.Exceptions;
using N.LMS.Database.Factory;

namespace N.LMS.Accessor.Module.Service;

internal sealed partial class ModuleAccessor
{
    private async Task<ModuleStoreResult> Handle(ModuleStoreRequest request)
    {
        await using var db = DatabaseFactory.CreateContext();
        var now = DateTime.UtcNow;
        var context = ProxyForService<IContextUtility>().GetContext();

        var courseExists = await db.Courses.AnyAsync(c => c.CourseId == request.CourseId && !c.Deleted);
        if (!courseExists)
        {
            throw new NotFoundException(context, nameof(DB.Course), nameof(DB.Course.CourseId), request.CourseId);
        }

        DB.Module entity;
        if (request.ModuleId is { } id)
        {
            entity = await db.Modules.SingleOrDefaultAsync(m => m.ModuleId == id && !m.Deleted)
                     ?? throw new NotFoundException(context, nameof(DB.Module), nameof(DB.Module.ModuleId), id);

            entity.Title = request.Title;
            entity.Description = request.Description;
            if (request.SortOrder is { } so) entity.SortOrder = so;
            entity.ModifiedUtc = now;
        }
        else
        {
            var sortOrder = request.SortOrder
                ?? (await db.Modules
                    .Where(m => m.CourseId == request.CourseId && !m.Deleted)
                    .Select(m => (int?)m.SortOrder).MaxAsync() ?? -1) + 1;

            entity = new DB.Module
            {
                ModuleId = Guid.NewGuid(),
                CourseId = request.CourseId,
                Title = request.Title,
                Description = request.Description,
                SortOrder = sortOrder,
                CreatedUtc = now,
                ModifiedUtc = now
            };
            db.Modules.Add(entity);
        }

        await db.SaveChangesAsync();

        return new ModuleStoreResult
        {
            Module = new ModuleInfo
            {
                ModuleId = entity.ModuleId,
                CourseId = entity.CourseId,
                Title = entity.Title,
                Description = entity.Description,
                SortOrder = entity.SortOrder,
                LessonCount = 0,
                CreatedUtc = entity.CreatedUtc,
                ModifiedUtc = entity.ModifiedUtc
            }
        };
    }
}
