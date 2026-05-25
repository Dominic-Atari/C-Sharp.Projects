using N.LMS.Manager.Learning.Interface.Model;
using N.LMS.Manager.Learning.Interface.Request;
using N.LMS.Manager.Learning.Interface.Result;

namespace N.LMS.Manager.Learning.Service;

internal sealed partial class LearningManager
{
    private async Task<CourseStructureLoadResult> Handle(CourseStructureLoadRequest request)
    {
        var courseAccessor = ProxyForService<CourseAccessor.ICourseAccessor>();
        var courseResult = (CourseAccessor.Result.CourseLoadResult)await courseAccessor.Load(
            new CourseAccessor.Request.CourseLoadRequest { CourseId = request.CourseId });

        if (courseResult.Course is null)
        {
            return new CourseStructureLoadResult { Structure = null };
        }

        var moduleAccessor = ProxyForService<ModuleAccessor.IModuleAccessor>();
        var modulesResult = (ModuleAccessor.Result.ModuleListResult)await moduleAccessor.Load(
            new ModuleAccessor.Request.ModulesByCourseRequest { CourseId = request.CourseId });

        var lessonAccessor = ProxyForService<LessonAccessor.ILessonAccessor>();
        var moduleNodes = new List<ModuleNode>(modulesResult.Modules.Count);
        foreach (var module in modulesResult.Modules)
        {
            var lessonsResult = (LessonAccessor.Result.LessonListResult)await lessonAccessor.Load(
                new LessonAccessor.Request.LessonsByModuleRequest { ModuleId = module.ModuleId });

            var lessonNodes = lessonsResult.Lessons.Select(l => _Mapper.Map<LessonNode>(l)).ToList();
            moduleNodes.Add(_Mapper.Map<ModuleNode>(module) with { Lessons = lessonNodes });
        }

        return new CourseStructureLoadResult
        {
            Structure = new CourseStructure
            {
                CourseId = courseResult.Course.CourseId,
                Title = courseResult.Course.Title,
                Modules = moduleNodes
            }
        };
    }
}
