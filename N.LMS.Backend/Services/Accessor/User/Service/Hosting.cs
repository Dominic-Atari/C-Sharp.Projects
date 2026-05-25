using N.LMS.Accessor.User.Interface;
using N.LMS.Common.Interface.Framework;

namespace N.LMS.Accessor.User.Service;

public static class Hosting
{
    public static readonly RegistrationBase[] Registrations =
    [
        TypeRegistration.New<IUserAccessor, UserAccessor>()
    ];
}
