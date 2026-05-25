using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.User.Interface.Result;

public sealed record UserDeleteResult : DeleteResultBase
{
    public Guid UserId { get; init; }
}
