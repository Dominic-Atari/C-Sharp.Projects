using N.LMS.Common.Interface.Result;

namespace N.LMS.Manager.Admin.Interface.Result;

public sealed record AdminHealthCheckResult : HealthCheckResultBase
{
    public bool UserAccessorHealthy { get; init; }
    public bool SystemAccessorHealthy { get; init; }
}
