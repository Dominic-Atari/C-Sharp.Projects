using System;
using System.Threading.Tasks;
using Azure.Storage.Sas;

namespace Nile.Utilities.AzureSdk;

public interface IVideoContentService
{
    Task<UploadSasInfo> IssueUploadSasAsync(Guid userId, string originalFileName, TimeSpan ttl, string? containerOverride = null);
    Task EnqueueProcessingAsync(VideoUploadedEnvelope message, string? queueNameOverride = null);
    Task<Uri> GetPlaybackUrlAsync(string videoId, string? processedContainerOverride = null, Uri? cdnBaseOverride = null);
}
