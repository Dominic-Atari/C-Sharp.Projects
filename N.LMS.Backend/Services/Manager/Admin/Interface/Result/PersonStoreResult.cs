using N.LMS.Common.Interface.Result;
using N.LMS.Manager.Admin.Interface.Model;

namespace N.LMS.Manager.Admin.Interface.Result;

public sealed record PersonStoreResult : StoreResultBase
{
    public required Person Person { get; init; }
}
