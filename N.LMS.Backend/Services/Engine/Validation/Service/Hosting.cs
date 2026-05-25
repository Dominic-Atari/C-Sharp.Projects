using N.LMS.Common.Interface.Framework;
using N.LMS.Engine.Validation.Interface;

namespace N.LMS.Engine.Validation.Service;

public static class Hosting
{
    public static readonly RegistrationBase[] Registrations =
    [
        TypeRegistration.New<IValidationEngine, ValidationEngine>()
    ];
}
