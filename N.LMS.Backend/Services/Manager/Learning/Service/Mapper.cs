using AutoMapper;
using N.LMS.Common.Interface.Mapping;
using N.LMS.Manager.Learning.Interface.Model;
using CourseAccessorModel = N.LMS.Accessor.Course.Interface.Model;
using EnrollmentAccessorModel = N.LMS.Accessor.Enrollment.Interface.Model;
using LessonAccessorModel = N.LMS.Accessor.Lesson.Interface.Model;
using ModuleAccessorModel = N.LMS.Accessor.Module.Interface.Model;

namespace N.LMS.Manager.Learning.Service;

internal sealed class Mapper() : MapperBase(CreateConfiguration())
{
    private static MapperConfiguration CreateConfiguration() =>
        CreateConfiguration(options =>
        {
            ConfigureCourseAccessorMappings(options);
            ConfigureEnrollmentAccessorMappings(options);
            ConfigureModuleLessonMappings(options);
        });

    private static void ConfigureCourseAccessorMappings(IMapperConfigurationExpression options)
    {
        options.CreateMap<CourseAccessorModel.CourseInfo, CatalogCourse>();
    }

    private static void ConfigureEnrollmentAccessorMappings(IMapperConfigurationExpression options)
    {
        options.CreateMap<EnrollmentAccessorModel.EnrollmentInfo, MyEnrollment>()
            .ForMember(d => d.CourseTitle, opt => opt.MapFrom(s => s.CourseTitle ?? string.Empty))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));
    }

    private static void ConfigureModuleLessonMappings(IMapperConfigurationExpression options)
    {
        options.CreateMap<LessonAccessorModel.LessonInfo, LessonNode>()
            .ForMember(d => d.Type, opt => opt.MapFrom(s => s.Type.ToString()));
        options.CreateMap<ModuleAccessorModel.ModuleInfo, ModuleNode>()
            .ForMember(d => d.Lessons, opt => opt.Ignore());
    }
}
