using System;

namespace Nile.Utilities.AzureSdk;

public sealed class VideoUploadedEnvelope
{
    public required string VideoId { get; init; }
    public required Guid UserId { get; init; }
    public required string Container { get; init; }
    public required string BlobName { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
}
