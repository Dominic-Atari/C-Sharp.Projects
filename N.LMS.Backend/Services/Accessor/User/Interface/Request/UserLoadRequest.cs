using N.LMS.Common.Interface.Request;

namespace N.LMS.Accessor.User.Interface.Request;

public sealed record UserLoadRequest : LoadRequestBase
{
    public Guid UserId { get; init; }
}
