using System;

namespace Nile.Utilities.AzureSdk;

public sealed class UploadSasInfo
{
    public required string BlobName { get; init; }
    public required Uri SasUri { get; init; }
    public required string ContainerName { get; init; }

    // Policy info for client UX / validations
    public required string ContentType { get; init; }
    public required long MaxSizeBytes { get; init; }
    public required DateTimeOffset ExpiresOn { get; init; }
}
