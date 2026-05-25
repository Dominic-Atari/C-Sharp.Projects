using N.LMS.Common.Interface.Request;

namespace N.LMS.Accessor.Module.Interface.Request;

public sealed record ModuleLoadRequest : LoadRequestBase
{
    public Guid ModuleId { get; init; }
}
