using Microsoft.Extensions.Logging;

namespace Nile.Accessors.HealthCheck;

/// <summary>
/// Minimal health check accessor placeholder.
/// Returns healthy to satisfy dependency wiring.
/// </summary>
internal class HealthCheckAccessor : AccessorBase, IHealthCheckAccessor
{
    public HealthCheckAccessor(ILogger<HealthCheckAccessor> logger) : base(logger)
    {
    }

    public Task<DTO.HealthStatusType> Perform()
    {
        return Task.FromResult(DTO.HealthStatusType.Healthy);
    }
}
