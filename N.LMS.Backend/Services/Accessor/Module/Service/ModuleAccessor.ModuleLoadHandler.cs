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
    private async Task<ModuleLoadResult> Handle(ModuleLoadRequest request)
    {
        await using var db = DatabaseFactory.CreateContext();

        var row = await db.Modules
            .Where(m => m.ModuleId == request.ModuleId && !m.Deleted)
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
            .SingleOrDefaultAsync();

        if (row is null)
        {
            var context = ProxyForService<IContextUtility>().GetContext();
            throw new NotFoundException(context, nameof(DB.Module), nameof(DB.Module.ModuleId), request.ModuleId);
        }

        return new ModuleLoadResult { Module = row };
    }
}
