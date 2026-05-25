using N.LMS.Accessor.System.Interface.Model;
using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.System.Interface.Result;

public sealed record SystemHealthResult : LoadResultBase
{
    public SystemHealth? Health { get; init; }
}
