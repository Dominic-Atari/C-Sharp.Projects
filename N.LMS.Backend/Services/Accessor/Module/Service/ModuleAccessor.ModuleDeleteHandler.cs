using Microsoft.EntityFrameworkCore;
using N.LMS.Accessor.Module.Interface.Request;
using N.LMS.Accessor.Module.Interface.Result;
using N.LMS.Common.Interface.Context;
using N.LMS.Common.Interface.Exceptions;
using N.LMS.Database.Factory;

namespace N.LMS.Accessor.Module.Service;

internal sealed partial class ModuleAccessor
{
    private async Task<ModuleDeleteResult> Handle(ModuleDeleteRequest request)
    {
        await using var db = DatabaseFactory.CreateContext();

        var entity = await db.Modules.SingleOrDefaultAsync(m => m.ModuleId == request.ModuleId && !m.Deleted);
        if (entity is null)
        {
            var context = ProxyForService<IContextUtility>().GetContext();
            throw new NotFoundException(context, nameof(DB.Module), nameof(DB.Module.ModuleId), request.ModuleId);
        }

        var now = DateTime.UtcNow;
        entity.Deleted = true;
        entity.ModifiedUtc = now;

        var lessons = await db.Lessons.Where(l => l.ModuleId == entity.ModuleId && !l.Deleted).ToListAsync();
        foreach (var lesson in lessons)
        {
            lesson.Deleted = true;
            lesson.ModifiedUtc = now;
        }

        await db.SaveChangesAsync();

        return new ModuleDeleteResult { ModuleId = entity.ModuleId };
    }
}
