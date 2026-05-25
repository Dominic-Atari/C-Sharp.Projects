using N.LMS.Common.Interface.Framework;

namespace N.LMS.Common.Service;

public sealed class RegistrationBuilder
{
    public IReadOnlyList<RegistrationBase> Registrations { get; }

    public RegistrationBuilder(RegistrationBase[] registrations)
    {
        Registrations = registrations;
    }
}
