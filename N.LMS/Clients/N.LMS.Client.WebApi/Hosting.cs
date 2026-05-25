using N.LMS.Common.Interface.Framework;

namespace N.LMS.Client.WebApi;

public static class Hosting
{
    public static readonly RegistrationBase[] Registrations =
    [
        // Common (currently empty — kept for forward compat)
        ..N.LMS.Common.Interface.Hosting.Registrations,

        // Utilities (bottom of the graph — singletons except Context which is Scoped)
        ..N.LMS.Utility.Context.Service.Hosting.Registrations,
        ..N.LMS.Utility.DateTime.Service.Hosting.Registrations,
        ..N.LMS.Utility.Logging.Service.Hosting.Registrations,

        // Engines
        ..N.LMS.Engine.Validation.Service.Hosting.Registrations,

        // Managers (subsystems) — registered with Exception + Context + Validation interceptors
        ..N.LMS.Manager.Admin.Service.Hosting.Registrations,
        ..N.LMS.Manager.Learning.Service.Hosting.Registrations,

        // Accessors (leaves)
        ..N.LMS.Accessor.User.Service.Hosting.Registrations,
        ..N.LMS.Accessor.Course.Service.Hosting.Registrations,
        ..N.LMS.Accessor.Enrollment.Service.Hosting.Registrations,
        ..N.LMS.Accessor.Module.Service.Hosting.Registrations,
        ..N.LMS.Accessor.Lesson.Service.Hosting.Registrations,
        ..N.LMS.Accessor.System.Service.Hosting.Registrations
    ];
}
