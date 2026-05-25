using N.LMS.Accessor.Course.Interface;
using N.LMS.Common.Interface.Framework;

namespace N.LMS.Accessor.Course.Service;

public static class Hosting
{
    public static readonly RegistrationBase[] Registrations =
    [
        TypeRegistration.New<ICourseAccessor, CourseAccessor>()
    ];
}
