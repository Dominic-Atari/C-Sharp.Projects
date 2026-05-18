using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace Nile.Utilities.AzureSdk;

internal partial class AzureSdkUtility : IVideoContentService
{
    // Default containers and queue names for the minimal pipeline
    private const string RawUploadsContainer = "uploads-raw";
    private const string ProcessedAssetsContainer = "assets-processed";
    private const string VideoProcessingQueue = "video-processing";

    public async Task<UploadSasInfo> IssueUploadSasAsync(Guid userId, string originalFileName, TimeSpan ttl, string? containerOverride = null)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ArgumentException("Original filename is required", nameof(originalFileName));

        var container = string.IsNullOrWhiteSpace(containerOverride) ? RawUploadsContainer : containerOverride;

        // Generate a unique blob name based on user and time to avoid collisions
        var ext = Path.GetExtension(originalFileName);
        var blobName = $"{userId:D}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{ext}";

        // Write + Create to allow direct PUT upload
        var perms = BlobContainerSasPermissions.Write | BlobContainerSasPermissions.Create;

        var dict = await GetSasUri(container, blobName, ttl, perms).ConfigureAwait(false);
        var sas = dict[blobName];

        return new UploadSasInfo
        {
            BlobName = blobName,
            SasUri = sas,
            ContainerName = container,
            ContentType = null,
            MaxSizeBytes = 0,
            ExpiresOn = default
        };
    }

    public async Task EnqueueProcessingAsync(VideoUploadedEnvelope message, string? queueNameOverride = null)
    {
        if (message is null) throw new ArgumentNullException(nameof(message));

        var queue = string.IsNullOrWhiteSpace(queueNameOverride) ? VideoProcessingQueue : queueNameOverride;

        // Serialize the message as JSON
        var payload = JsonSerializer.SerializeToUtf8Bytes(message);
        var sbMessage = new ServiceBusMessage(payload)
        {
            ContentType = "application/json",
            Subject = "video-uploaded",
            MessageId = message.VideoId
        };

        var sender = _serviceBusClient.CreateSender(queue);
        await sender.SendMessageAsync(sbMessage).ConfigureAwait(false);
    }

    public Task<Uri> GetPlaybackUrlAsync(string videoId, string? processedContainerOverride = null, Uri? cdnBaseOverride = null)
    {
        if (string.IsNullOrWhiteSpace(videoId)) throw new ArgumentException("videoId is required", nameof(videoId));

        var assetsContainer = string.IsNullOrWhiteSpace(processedContainerOverride) ? ProcessedAssetsContainer : processedContainerOverride;

        // Minimal assumption: processed output writes HLS manifests to {videoId}/master.m3u8
        var containerClient = _blobServiceClient.GetBlobContainerClient(assetsContainer);
        var manifestUri = new Uri(containerClient.Uri, $"{videoId}/master.m3u8");

        // If a CDN base is provided, map blob path to CDN origin
        if (cdnBaseOverride is not null)
        {
            // Recompose path relative to container root
            var relativePath = $"{assetsContainer}/{videoId}/master.m3u8";
            var cdnUri = new Uri($"{cdnBaseOverride.ToString().TrimEnd('/')}/{relativePath}");
            return Task.FromResult(cdnUri);
        }

        return Task.FromResult(manifestUri);
    }
}
