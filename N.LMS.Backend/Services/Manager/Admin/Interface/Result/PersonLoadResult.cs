using N.LMS.Common.Interface.Result;
using N.LMS.Manager.Admin.Interface.Model;

namespace N.LMS.Manager.Admin.Interface.Result;

public sealed record PersonLoadResult : LoadResultBase
{
    public Person? Person { get; init; }
}
