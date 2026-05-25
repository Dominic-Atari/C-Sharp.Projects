using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.Module.Interface.Result;

public sealed record ModuleDeleteResult : DeleteResultBase
{
    public Guid ModuleId { get; init; }
}
