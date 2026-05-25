using N.LMS.Common.Interface.Context;
using N.LMS.Common.Interface.Framework;

namespace N.LMS.Utility.Context.Service;

public static class Hosting
{
    public static readonly RegistrationBase[] Registrations =
    [
        // Scoped — must be one ContextUtility per call graph so AsyncLocal stays
        // pinned to a single request flow.
        TypeRegistration.New<IContextUtility, ContextUtility>(LifetimeScope.Scoped)
    ];
}
