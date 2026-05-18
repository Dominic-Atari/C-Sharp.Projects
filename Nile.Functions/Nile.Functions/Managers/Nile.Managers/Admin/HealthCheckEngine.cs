using Nile.Common.InternalDTOs;
using Nile.Managers.HealthCheck;

namespace Nile.Managers.Admin;

// Minimal health check engine placeholder; returns healthy for all checks.
internal class HealthCheckEngine : IHealthCheckEngine
{
    public Task<HealthCheckResponse> Perform()
    {
        var healthy = HealthStatusType.Healthy;
        var response = new HealthCheckResponse
        {
            BlobStorageStatus = healthy,
            DatabaseStatus = healthy,
            FormatUtilityStatus = healthy,
            MessageBusStatus = healthy,
            OcrUtilityStatus = healthy
        };

        return Task.FromResult(response);
    }
}
