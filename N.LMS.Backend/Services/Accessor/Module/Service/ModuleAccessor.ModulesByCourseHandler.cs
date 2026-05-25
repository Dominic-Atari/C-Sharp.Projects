using Microsoft.EntityFrameworkCore;
using N.LMS.Accessor.Module.Interface.Model;
using N.LMS.Accessor.Module.Interface.Request;
using N.LMS.Accessor.Module.Interface.Result;
using N.LMS.Database.Factory;

namespace N.LMS.Accessor.Module.Service;

internal sealed partial class ModuleAccessor
{
    private async Task<ModuleListResult> Handle(ModulesByCourseRequest request)
    {
        await using var db = DatabaseFactory.CreateContext();

        var rows = await db.Modules
            .Where(m => m.CourseId == request.CourseId && !m.Deleted)
            .OrderBy(m => m.SortOrder)
            .Select(m => new ModuleInfo
            {
                ModuleId = m.ModuleId,
                CourseId = m.CourseId,
                Title = m.Title,
                Description = m.Description,
                SortOrder = m.SortOrder,
                LessonCount = m.Lessons.Count(l => !l.Deleted),
                CreatedUtc = m.CreatedUtc,
                ModifiedUtc = m.ModifiedUtc
            })
            .ToListAsync();

        return new ModuleListResult { Modules = rows };
    }
}
