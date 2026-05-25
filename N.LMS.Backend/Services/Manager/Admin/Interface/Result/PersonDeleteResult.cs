using N.LMS.Common.Interface.Result;

namespace N.LMS.Manager.Admin.Interface.Result;

public sealed record PersonDeleteResult : DeleteResultBase
{
    public Guid PersonId { get; init; }
}
