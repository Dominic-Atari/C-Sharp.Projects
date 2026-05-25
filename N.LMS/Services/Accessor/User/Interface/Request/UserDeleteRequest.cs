using N.LMS.Common.Interface.Request;

namespace N.LMS.Accessor.User.Interface.Request;

public sealed record UserDeleteRequest : DeleteRequestBase
{
    public Guid UserId { get; init; }
}
