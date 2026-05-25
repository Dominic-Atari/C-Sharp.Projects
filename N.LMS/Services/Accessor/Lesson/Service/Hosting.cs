using N.LMS.Accessor.Lesson.Interface;
using N.LMS.Common.Interface.Framework;

namespace N.LMS.Accessor.Lesson.Service;

public static class Hosting
{
    public static readonly RegistrationBase[] Registrations =
    [
        TypeRegistration.New<ILessonAccessor, LessonAccessor>()
    ];
}
