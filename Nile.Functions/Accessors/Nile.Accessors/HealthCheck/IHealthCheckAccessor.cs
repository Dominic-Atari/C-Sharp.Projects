namespace Nile.Accessors.HealthCheck;

public interface IHealthCheckAccessor
{
    Task<DTO.HealthStatusType> Perform();
}