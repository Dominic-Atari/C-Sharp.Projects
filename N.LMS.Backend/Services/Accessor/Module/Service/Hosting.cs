using N.LMS.Accessor.Module.Interface;
using N.LMS.Common.Interface.Framework;

namespace N.LMS.Accessor.Module.Service;

public static class Hosting
{
    public static readonly RegistrationBase[] Registrations =
    [
        TypeRegistration.New<IModuleAccessor, ModuleAccessor>()
    ];
}
