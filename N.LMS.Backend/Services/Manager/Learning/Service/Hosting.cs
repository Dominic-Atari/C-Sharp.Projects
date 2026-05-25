using N.LMS.Common.Interface.Framework;
using N.LMS.Ifx.Interceptor;
using N.LMS.Manager.Learning.Interface;

namespace N.LMS.Manager.Learning.Service;

public static class Hosting
{
    public static readonly RegistrationBase[] Registrations =
    [
        TypeRegistration.New<ILearningManager, LearningManager>(
            interceptors:
            [
                new ExceptionHandlingInterceptor(),
                new ContextBuildingInterceptor(),
                new ValidationEngineInterceptor(new Mapper())
            ]
        )
    ];
}
