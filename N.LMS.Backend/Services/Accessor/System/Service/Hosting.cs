using N.LMS.Accessor.System.Interface;
using N.LMS.Common.Interface.Framework;

namespace N.LMS.Accessor.System.Service;

public static class Hosting
{
    public static readonly RegistrationBase[] Registrations =
    [
        TypeRegistration.New<ISystemAccessor, SystemAccessor>()
    ];
}
