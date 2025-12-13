using Azure.Storage.Sas;
using Nile.Common.InternalDTOs;

namespace Nile.Utilities.AzureSdk;

public interface IBlobStorageUtility
{
    Uri GetBlobNameUri();
    Task<string> GetBlobContents(string blobName, string containerName, TimeSpan duration,  BlobContainerSasPermissions blobContainerSasPermissions);

    Task<Dictionary<string, Uri>> GetSasUri(string containerName, string blobName, TimeSpan duration,
        BlobContainerSasPermissions blobContainerSasPermissions);
    Task<HealthStatusType> Perform();
}