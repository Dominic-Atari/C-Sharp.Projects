using N.LMS.Common.Interface.Framework;
using N.LMS.Ifx.Interceptor;
using N.LMS.Manager.Admin.Interface;

namespace N.LMS.Manager.Admin.Service;

public static class Hosting
{
    public static readonly RegistrationBase[] Registrations =
    [
        TypeRegistration.New<IAdminManager, AdminManager>(
            interceptors:
            [
                new ExceptionHandlingInterceptor(),
                new ContextBuildingInterceptor(),
                new ValidationEngineInterceptor(new Mapper())
            ]
        )
    ];
}
