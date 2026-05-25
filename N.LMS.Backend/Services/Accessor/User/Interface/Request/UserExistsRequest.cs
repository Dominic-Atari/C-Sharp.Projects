using N.LMS.Common.Interface.Request;

namespace N.LMS.Accessor.User.Interface.Request;

public sealed record UserExistsRequest : LoadRequestBase
{
    public Guid UserId { get; init; }
}
