using N.LMS.Common.Interface.Framework;

namespace N.LMS.Common.Interface;

/// <summary>
/// Common.Interface ships no registrations of its own — concrete types it defines
/// (ContextUtility) are registered under their owning utility layer.
/// This stub exists so callers that splat <c>..Common.Interface.Hosting.Registrations</c>
/// don't break when those types relocate.
/// </summary>
public static class Hosting
{
    public static readonly RegistrationBase[] Registrations = Array.Empty<RegistrationBase>();
}
