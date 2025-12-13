using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace Nile.Utilities.AzureSdk;

internal partial class AzureSdkUtility : IPhotoContentService
{
    private static readonly string[] s_allowedImageExtensions = new[]
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".tif", ".tiff"
    };

    private static readonly Dictionary<string, string[]> s_allowedMimeByExtension =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = new[] { "image/jpeg" },
            [".jpeg"] = new[] { "image/jpeg" },
            [".png"] = new[] { "image/png" },
            [".gif"] = new[] { "image/gif" },
            [".webp"] = new[] { "image/webp" },
            [".bmp"] = new[] { "image/bmp" },
            [".tif"] = new[] { "image/tiff" },
            [".tiff"] = new[] { "image/tiff" },
        };

    private const string PhotosContainer = "photos";
    private const string ThumbnailsContainer = "photos-thumbnails";

    public async Task<UploadSasInfo> IssuePhotoUploadSasAsync(Guid userId, string originalFileName, string contentType, long maxSizeBytes, TimeSpan ttl, string? containerOverride = null)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ArgumentException("Original filename is required", nameof(originalFileName));
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content-Type is required", nameof(contentType));
        if (maxSizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxSizeBytes), "Max size must be greater than zero.");

        var container = string.IsNullOrWhiteSpace(containerOverride) ? PhotosContainer : containerOverride;

        var ext = (Path.GetExtension(originalFileName) ?? string.Empty).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(ext) || Array.IndexOf(s_allowedImageExtensions, ext) < 0)
            throw new ArgumentException($"Unsupported image extension '{ext}'. Allowed: {string.Join(",", s_allowedImageExtensions)}", nameof(originalFileName));

        if (!s_allowedMimeByExtension.TryGetValue(ext, out var allowedMimes) || !allowedMimes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"Content-Type '{contentType}' is not allowed for extension '{ext}'. Allowed: {string.Join(",", allowedMimes ?? Array.Empty<string>())}", nameof(contentType));

        // Unique, hierarchical blob naming for better partitioning
        var blobName = $"{userId:D}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{ext}";

        // Write + Create permissions for direct PUT upload
        var perms = BlobContainerSasPermissions.Write | BlobContainerSasPermissions.Create;

        var dict = await GetSasUri(container, blobName, ttl, perms).ConfigureAwait(false);
        var sas = dict[blobName];

        return new UploadSasInfo
        {
            BlobName = blobName,
            SasUri = sas,
            ContainerName = container,
            ContentType = contentType,
            MaxSizeBytes = maxSizeBytes,
            ExpiresOn = _dateUtility.UtcNowOffset.Add(ttl)
        };
    }

    public async Task ValidateUploadedPhotoAsync(string container, string blobName, string expectedContentType, long maxSizeBytes)
    {
        if (string.IsNullOrWhiteSpace(container)) throw new ArgumentException(nameof(container));
        if (string.IsNullOrWhiteSpace(blobName)) throw new ArgumentException(nameof(blobName));
        if (string.IsNullOrWhiteSpace(expectedContentType)) throw new ArgumentException(nameof(expectedContentType));
        if (maxSizeBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maxSizeBytes));

        var blob = _blobServiceClient.GetBlobContainerClient(container).GetBlobClient(blobName);
        BlobProperties props;
        try
        {
            props = (await blob.GetPropertiesAsync().ConfigureAwait(false)).Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new FileNotFoundException($"Uploaded photo blob not found: {container}/{blobName}");
        }

        if (!string.Equals(props.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Uploaded Content-Type '{props.ContentType}' does not match expected '{expectedContentType}'.");

        if (props.ContentLength > maxSizeBytes)
            throw new InvalidOperationException($"Uploaded photo size {props.ContentLength} exceeds maximum {maxSizeBytes} bytes.");
    }

    public async Task<IReadOnlyDictionary<string, Uri>> GenerateThumbnailsAsync(
        string sourceContainer,
        string sourceBlobName,
        int[] sizes,
        bool alsoWebp,
        string? thumbnailsContainerOverride = null,
        Uri? cdnBaseOverride = null)
    {
        if (sizes is null || sizes.Length == 0) throw new ArgumentException("At least one size must be specified.", nameof(sizes));

        var thumbContainer = string.IsNullOrWhiteSpace(thumbnailsContainerOverride) ? ThumbnailsContainer : thumbnailsContainerOverride;

        var sourceContainerClient = _blobServiceClient.GetBlobContainerClient(sourceContainer);
        var sourceBlob = sourceContainerClient.GetBlobClient(sourceBlobName);

        await _blobServiceClient.GetBlobContainerClient(thumbContainer).CreateIfNotExistsAsync().ConfigureAwait(false);

        // Download the source (dispose the content stream, not the Response wrapper)
        var downloadResponse = await sourceBlob.DownloadStreamingAsync().ConfigureAwait(false);
        await using var contentStream = downloadResponse.Value.Content;
        using var image = await Image.LoadAsync(contentStream).ConfigureAwait(false);

        var baseDir = Path.GetDirectoryName(sourceBlobName)?.Replace('\\', '/');
        var baseFile = Path.GetFileNameWithoutExtension(sourceBlobName);
        var basePrefix = string.IsNullOrWhiteSpace(baseDir) ? baseFile : $"{baseDir}/{baseFile}";

        var results = new Dictionary<string, Uri>(sizes.Length * (alsoWebp ? 2 : 1));

        foreach (var size in sizes.Distinct().OrderBy(s => s))
        {
            // Resize while preserving aspect ratio (fit within WxW)
            using var clone = image.Clone(ctx => ctx.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(size, size)
            }));

            // Save JPEG thumbnail
            var jpegBlobName = $"{basePrefix}_{size}.jpg";
            var jpegClient = _blobServiceClient.GetBlobContainerClient(thumbContainer).GetBlobClient(jpegBlobName);
            await using (var ms = new MemoryStream())
            {
                await clone.SaveAsync(ms, new JpegEncoder { Quality = 80 }).ConfigureAwait(false);
                ms.Position = 0;
                await jpegClient.UploadAsync(ms, overwrite: true).ConfigureAwait(false);
                await jpegClient.SetHttpHeadersAsync(new BlobHttpHeaders { ContentType = "image/jpeg" }).ConfigureAwait(false);
            }
            results[jpegBlobName] = ToPublicOrCdnUri(thumbContainer, jpegBlobName, cdnBaseOverride);

            if (alsoWebp)
            {
                // WebP generation is skipped because a compatible WebP plugin/package is not referenced.
                // Thumbnails are produced in JPEG format above.
            }
        }

        return results;
    }

    public Task<Uri> GetPhotoPublicUrlAsync(string blobName, string? containerOverride = null, Uri? cdnBaseOverride = null)
    {
        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("blobName is required", nameof(blobName));

        var container = string.IsNullOrWhiteSpace(containerOverride) ? PhotosContainer : containerOverride;

        var containerClient = _blobServiceClient.GetBlobContainerClient(container);
        var blobUri = new Uri(containerClient.Uri, blobName);

        if (cdnBaseOverride is not null)
        {
            var relativePath = $"{container}/{blobName}";
            var cdnUri = new Uri($"{cdnBaseOverride.ToString().TrimEnd('/')}/{relativePath}");
            return Task.FromResult(cdnUri);
        }

        return Task.FromResult(blobUri);
    }

    private Uri ToPublicOrCdnUri(string container, string blobName, Uri? cdnBaseOverride)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(container);
        var blobUri = new Uri(containerClient.Uri, blobName);

        if (cdnBaseOverride is not null)
        {
            var relativePath = $"{container}/{blobName}";
            return new Uri($"{cdnBaseOverride.ToString().TrimEnd('/')}/{relativePath}");
        }

        return blobUri;
    }
}
