using N.LMS.Common.Interface.Framework;
using N.LMS.Utility.DateTime.Interface;

namespace N.LMS.Utility.DateTime.Service;

public static class Hosting
{
    public static readonly RegistrationBase[] Registrations =
    [
        TypeRegistration.New<IDateTimeUtility, DateTimeUtility>(LifetimeScope.Singleton)
    ];
}
