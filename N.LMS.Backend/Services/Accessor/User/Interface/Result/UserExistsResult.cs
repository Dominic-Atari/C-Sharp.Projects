using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.User.Interface.Result;

public sealed record UserExistsResult : LoadResultBase
{
    public bool Exists { get; init; }
}
