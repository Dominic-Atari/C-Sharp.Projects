using AutoMapper;
using N.LMS.Common.Interface.Mapping;
using N.LMS.Manager.Admin.Interface.Model;
using N.LMS.Manager.Admin.Interface.Request;
using N.LMS.Manager.Admin.Interface.Result;
using UserAccessorModel = N.LMS.Accessor.User.Interface.Model;
using UserAccessorRequest = N.LMS.Accessor.User.Interface.Request;
using UserAccessorResult = N.LMS.Accessor.User.Interface.Result;

namespace N.LMS.Manager.Admin.Service;

internal sealed class Mapper() : MapperBase(CreateConfiguration())
{
    private static MapperConfiguration CreateConfiguration() =>
        CreateConfiguration(options =>
        {
            ConfigureUserAccessorMappings(options);
        });

    private static void ConfigureUserAccessorMappings(IMapperConfigurationExpression options)
    {
        // Models: accessor's UserInfo → manager's Person
        options.CreateMap<UserAccessorModel.UserInfo, Person>()
            .ForMember(d => d.PersonId, opt => opt.MapFrom(s => s.UserId))
            .ForMember(d => d.Role, opt => opt.MapFrom(s => (PersonRole)(int)s.Role));

        // Requests: Manager → Accessor
        options.CreateMap<PersonStoreRequest, UserAccessorRequest.UserStoreRequest>()
            .ForMember(d => d.UserId, opt => opt.MapFrom(s => s.PersonId))
            .ForMember(d => d.Role, opt => opt.MapFrom(s => (UserAccessorModel.UserRole)(int)s.Role));

        options.CreateMap<PersonDeleteRequest, UserAccessorRequest.UserDeleteRequest>()
            .ForMember(d => d.UserId, opt => opt.MapFrom(s => s.PersonId));

        options.CreateMap<PersonLoadRequest, UserAccessorRequest.UserLoadRequest>()
            .ForMember(d => d.UserId, opt => opt.MapFrom(s => s.PersonId));

        // Results: Accessor → Manager
        options.CreateMap<UserAccessorResult.UserStoreResult, PersonStoreResult>()
            .ForMember(d => d.Person, opt => opt.MapFrom(s => s.User));

        options.CreateMap<UserAccessorResult.UserLoadResult, PersonLoadResult>()
            .ForMember(d => d.Person, opt => opt.MapFrom(s => s.User));

        options.CreateMap<UserAccessorResult.UserLoadResult, MeLoadResult>()
            .ForMember(d => d.Person, opt => opt.MapFrom(s => s.User));
    }
}
