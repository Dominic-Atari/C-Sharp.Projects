using Nile.Common.InternalDTOs;
using ResponseBase = Nile.Managers.Contract.Client.DataContract.ResponseBase;

namespace Nile.Managers.HealthCheck;

public class HealthCheckResponse : ResponseBase
{
    public required HealthStatusType? BlobStorageStatus { get; init; }

    public required HealthStatusType? DatabaseStatus { get; init; }

    public required HealthStatusType? FormatUtilityStatus { get; init; }

    public required HealthStatusType MessageBusStatus { get; init; }

    public required HealthStatusType? OcrUtilityStatus { get; init; }
}