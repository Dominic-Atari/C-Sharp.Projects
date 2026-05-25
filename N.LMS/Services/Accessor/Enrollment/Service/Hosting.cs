using N.LMS.Accessor.Enrollment.Interface;
using N.LMS.Common.Interface.Framework;

namespace N.LMS.Accessor.Enrollment.Service;

public static class Hosting
{
    public static readonly RegistrationBase[] Registrations =
    [
        TypeRegistration.New<IEnrollmentAccessor, EnrollmentAccessor>()
    ];
}
