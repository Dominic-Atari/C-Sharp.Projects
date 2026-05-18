using Azure.Storage.Sas;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using System.Text;
using Azure.Storage.Blobs.Models;

namespace Nile.Utilities.AzureSdk;

internal partial class AzureSdkUtility : IBlobStorageUtility
{
    private static readonly Dictionary<TimeSpan, UserDelegationKey> s_userDelegationKeys = new();

    public Uri GetBlobNameUri()
    {
        // Return the Blob Service endpoint from the client
        return _blobServiceClient.Uri;
    }

    public async Task<string> GetBlobContents(string blobName, string containerName, TimeSpan duration,
        BlobContainerSasPermissions blobContainerSasPermissions)
    {
        var blob = BlockBlobClientForBlob(containerName, blobName);
        var download = await blob.DownloadContentAsync().ConfigureAwait(false);
        return download.Value.Content.ToString();
    }

    public async Task<Dictionary<string, Uri>> GetSasUri(string containerName, string blobName, TimeSpan duration,
        BlobContainerSasPermissions blobContainerSasPermissions)
    {
        var userDelegationKey = await GetUserDelegationKeyAsync(duration).ConfigureAwait(false);

        // Build a blob-level SAS using a builder and attach via Uri builder
        var blockBlobClient = BlockBlobClientForBlob(containerName, blobName);

        var blobSasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b",
            StartsOn = _dateUtility.UtcNowOffset.AddMinutes(-1),
            ExpiresOn = _dateUtility.UtcNowOffset.Add(duration)
        };
        blobSasBuilder.SetPermissions(MapContainerToBlobPermissions(blobContainerSasPermissions));

        var blobUriBuilder = new BlobUriBuilder(blockBlobClient.Uri)
        {
            Sas = blobSasBuilder.ToSasQueryParameters(userDelegationKey, _blobServiceClient.AccountName)
        };

        var result = new Dictionary<string, Uri>
        {
            // Key by blob name to align with the strategy of returning blob SAS per object
            [blobName] = blobUriBuilder.ToUri()
        };

        // Optionally include a container-level SAS as well
        var containerSasBuilder = new BlobSasBuilder(blobContainerSasPermissions, _dateUtility.UtcNow.Add(duration))
        {
            BlobContainerName = containerName,
            Resource = "c"
        };
        var containerClient = GetBlobContainerClient(containerName);
        var containerSasParams = containerSasBuilder.ToSasQueryParameters(userDelegationKey, _blobServiceClient.AccountName);
        result["ContainerSasUri"] = new Uri($"{containerClient.Uri}?{containerSasParams}");

        return result;
    }

    // Convenience helper: return a single blob SAS URI
    public async Task<Uri> GetBlobSasUri(string containerName, string blobName, TimeSpan duration,
        BlobContainerSasPermissions blobContainerSasPermissions)
    {
        var dict = await GetSasUri(containerName, blobName, duration, blobContainerSasPermissions)
            .ConfigureAwait(false);
        return dict[blobName];
    }

    public async Task<DTO.HealthStatusType> CheckBlobHealth()
    {
        const string container = "health";
        const string fileName = "healthcheck.txt";
        const string content = "Success!";

        try
        {
            var containerClient = GetBlobContainerClient(container);
            await containerClient.CreateIfNotExistsAsync().ConfigureAwait(false);

            var blob = containerClient.GetBlockBlobClient(fileName);
            await blob.DeleteIfExistsAsync().ConfigureAwait(false);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            await blob.UploadAsync(stream).ConfigureAwait(false);

            var downloaded = await blob.DownloadContentAsync().ConfigureAwait(false);
            var downloadedContent = downloaded.Value.Content.ToString();

            return downloadedContent == content ? DTO.HealthStatusType.Healthy : DTO.HealthStatusType.Unhealthy;
        }
        catch
        {
            return DTO.HealthStatusType.Unhealthy;
        }
    }

    private BlobContainerClient GetBlobContainerClient(string container)
    {
        return _blobServiceClient.GetBlobContainerClient(container);
    }

    private BlockBlobClient BlockBlobClientForBlob(string container, string blobName)
    {
        return GetBlobContainerClient(container).GetBlockBlobClient(blobName);
    }

    private async Task<UserDelegationKey> GetUserDelegationKeyAsync(TimeSpan duration)
    {
        var now = _dateUtility.UtcNowOffset;

        if (s_userDelegationKeys.TryGetValue(duration, out var cachedKey) &&
            !IsKeyExpiredOrNearExpiration(cachedKey, now, duration))
        {
            return cachedKey;
        }

        var newKeyResponse = await _blobServiceClient.GetUserDelegationKeyAsync(
            _dateUtility.UtcNow.AddMinutes(-1),
            _dateUtility.UtcNow.Add(duration)
        ).ConfigureAwait(false);

        var key = newKeyResponse.Value;
        s_userDelegationKeys[duration] = key;
        return key;
    }

    private static bool IsKeyExpiredOrNearExpiration(UserDelegationKey key, DateTimeOffset now, TimeSpan duration)
    {
        var timeUntilExpiration = key.SignedExpiresOn - now;
        var buffer = TimeSpan.FromTicks((long)(duration.Ticks * 0.2)); // 20% buffer
        return timeUntilExpiration <= buffer;
    }

    private static BlobSasPermissions MapContainerToBlobPermissions(BlobContainerSasPermissions containerPerms)
    {
        // Compose BlobSasPermissions using flag enums.
        var blobPerms = (BlobSasPermissions)0;

        if (containerPerms.HasFlag(BlobContainerSasPermissions.Read))   blobPerms |= BlobSasPermissions.Read;
        if (containerPerms.HasFlag(BlobContainerSasPermissions.Write))  blobPerms |= BlobSasPermissions.Write;
        if (containerPerms.HasFlag(BlobContainerSasPermissions.Create)) blobPerms |= BlobSasPermissions.Create;
        if (containerPerms.HasFlag(BlobContainerSasPermissions.Delete)) blobPerms |= BlobSasPermissions.Delete;
        if (containerPerms.HasFlag(BlobContainerSasPermissions.Add))    blobPerms |= BlobSasPermissions.Add;
        if (containerPerms.HasFlag(BlobContainerSasPermissions.Tag))    blobPerms |= BlobSasPermissions.Tag;
        if (containerPerms.HasFlag(BlobContainerSasPermissions.Move))   blobPerms |= BlobSasPermissions.Move;
        if (containerPerms.HasFlag(BlobContainerSasPermissions.Execute))blobPerms |= BlobSasPermissions.Execute;

        return blobPerms;
    }

}