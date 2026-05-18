using Nile.Managers.HealthCheck;

namespace Nile.Managers.Admin;

public interface IHealthCheckEngine
{
    Task<HealthCheckResponse> Perform();
}