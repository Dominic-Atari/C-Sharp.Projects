using N.LMS.Accessor.System.Interface.Model;
using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.System.Interface.Result;

public sealed record SystemInfoResult : LoadResultBase
{
    public SystemInfo? Info { get; init; }
}
