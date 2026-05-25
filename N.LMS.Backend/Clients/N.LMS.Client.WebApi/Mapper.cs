using AutoMapper;
using N.LMS.Client.WebApi.Response.Admin;
using N.LMS.Client.WebApi.Response.Learning;
using N.LMS.Common.Interface.Mapping;
using AM = N.LMS.Manager.Admin.Interface.Result;
using LM = N.LMS.Manager.Learning.Interface.Result;

namespace N.LMS.Client.WebApi;

internal sealed class Mapper() : MapperBase(CreateConfiguration())
{
    private static MapperConfiguration CreateConfiguration() =>
        CreateConfiguration(options =>
        {
            ConfigureAdminMappings(options);
            ConfigureLearningMappings(options);
        });

    private static void ConfigureAdminMappings(IMapperConfigurationExpression options)
    {
        options.CreateMap<AM.PersonLoadResult,        PersonLoadResponse>();
        options.CreateMap<AM.PersonStoreResult,       PersonStoreResponse>();
        options.CreateMap<AM.PersonDeleteResult,      PersonDeleteResponse>();
        options.CreateMap<AM.MeLoadResult,            MeLoadResponse>();
        options.CreateMap<AM.AdminHealthCheckResult,  HealthCheckResponse>();
    }

    private static void ConfigureLearningMappings(IMapperConfigurationExpression options)
    {
        options.CreateMap<LM.CourseCatalogLoadResult,   CourseCatalogResponse>();
        options.CreateMap<LM.CourseStructureLoadResult, CourseStructureResponse>();
        options.CreateMap<LM.MyEnrollmentsLoadResult,   MyEnrollmentsResponse>();
        options.CreateMap<LM.EnrollInCourseResult,      EnrollInCourseResponse>();
        options.CreateMap<LM.LearningHealthCheckResult, HealthCheckResponse>();
    }
}
