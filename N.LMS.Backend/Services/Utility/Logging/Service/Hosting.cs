using N.LMS.Common.Interface.Framework;
using N.LMS.Utility.Logging.Interface;

namespace N.LMS.Utility.Logging.Service;

public static class Hosting
{
    public static readonly RegistrationBase[] Registrations =
    [
        TypeRegistration.New<ILoggingUtility, LoggingUtility>(LifetimeScope.Singleton)
    ];
}
