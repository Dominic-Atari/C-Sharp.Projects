using N.LMS.Common.Interface.Request;

namespace N.LMS.Accessor.User.Interface.Request;

public sealed record UserEmailExistsRequest : LoadRequestBase
{
    public string Email { get; init; } = string.Empty;
}
