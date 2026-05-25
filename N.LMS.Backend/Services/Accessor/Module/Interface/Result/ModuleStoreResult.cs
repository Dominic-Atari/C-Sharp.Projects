using N.LMS.Accessor.Module.Interface.Model;
using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.Module.Interface.Result;

public sealed record ModuleStoreResult : StoreResultBase
{
    public ModuleInfo? Module { get; init; }
}
