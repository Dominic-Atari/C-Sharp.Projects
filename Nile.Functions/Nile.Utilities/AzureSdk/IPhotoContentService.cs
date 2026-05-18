using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Sas;

namespace Nile.Utilities.AzureSdk;

public interface IPhotoContentService
{
    // Issue SAS with validation on extension + intended content type and size policy
    Task<UploadSasInfo> IssuePhotoUploadSasAsync(Guid userId, string originalFileName, string contentType, long maxSizeBytes, TimeSpan ttl, string? containerOverride = null);

    // Validate uploaded blob by inspecting its server-side properties (content type, size)
    Task ValidateUploadedPhotoAsync(string container, string blobName, string expectedContentType, long maxSizeBytes);

    // Generate thumbnails (and optional WebP) into a target container and return their URIs (optionally mapped to CDN)
    Task<IReadOnlyDictionary<string, Uri>> GenerateThumbnailsAsync(
        string sourceContainer,
        string sourceBlobName,
        int[] sizes,
        bool alsoWebp,
        string? thumbnailsContainerOverride = null,
        Uri? cdnBaseOverride = null);

    // Compose a public or CDN URL for a stored photo
    Task<Uri> GetPhotoPublicUrlAsync(string blobName, string? containerOverride = null, Uri? cdnBaseOverride = null);
}
