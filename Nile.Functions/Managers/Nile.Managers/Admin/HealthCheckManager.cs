using Nile.Managers.DataContract.HealthCheck;
using Nile.Managers.HealthCheck;

namespace Nile.Managers.Admin;

internal partial class AdminManager : IHealthCheckManager
{
    async Task<HealthCheckResponse> IHealthCheckManager.Perform(HealthCheckRequest _)
    {
        var engineHealthCheck = await _healthCheckEngine.Perform();
        var databaseStatus = await _healthCheckAccessor.Perform();
        var blobStorageStatus = await _blobStorageUtility.Perform();
        var messageBusStatus = await _messageBusUtility.Perform();

        return new HealthCheckResponse
        {
            DatabaseStatus = databaseStatus,
            FormatUtilityStatus = engineHealthCheck.FormatUtilityStatus,
            OcrUtilityStatus = engineHealthCheck.OcrUtilityStatus,
            BlobStorageStatus = blobStorageStatus,
            MessageBusStatus = messageBusStatus
        };
    }
}