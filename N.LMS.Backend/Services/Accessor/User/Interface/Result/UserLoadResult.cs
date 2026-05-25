using N.LMS.Accessor.User.Interface.Model;
using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.User.Interface.Result;

public sealed record UserLoadResult : LoadResultBase
{
    public UserInfo? User { get; init; }
}
