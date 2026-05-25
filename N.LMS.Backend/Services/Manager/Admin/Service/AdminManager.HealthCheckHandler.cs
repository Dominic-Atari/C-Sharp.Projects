using N.LMS.Manager.Admin.Interface.Request;
using N.LMS.Manager.Admin.Interface.Result;

namespace N.LMS.Manager.Admin.Service;

internal sealed partial class AdminManager
{
    private async Task<AdminHealthCheckResult> Handle(AdminHealthCheckRequest _)
    {
        var systemAccessor = ProxyForService<SystemAccessor.ISystemAccessor>();
        var health = (SystemAccessor.Result.SystemHealthResult)await systemAccessor.Load(
            new SystemAccessor.Request.SystemHealthRequest());

        var userAccessor = ProxyForService<UserAccessor.IUserAccessor>();
        bool userHealthy;
        try
        {
            await userAccessor.Load(new UserAccessor.Request.UserEmailExistsRequest { Email = "__healthcheck__@n.lms" });
            userHealthy = true;
        }
        catch
        {
            userHealthy = false;
        }

        var allHealthy = (health.Health?.Healthy ?? false) && userHealthy;

        return new AdminHealthCheckResult
        {
            Healthy = allHealthy,
            Message = allHealthy ? null : health.Health?.Message ?? "Downstream accessor unhealthy.",
            CheckedAtUtc = DateTime.UtcNow,
            SystemAccessorHealthy = health.Health?.Healthy ?? false,
            UserAccessorHealthy = userHealthy
        };
    }
}
