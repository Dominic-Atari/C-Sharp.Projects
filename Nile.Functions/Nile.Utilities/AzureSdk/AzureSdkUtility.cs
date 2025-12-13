using Azure.Messaging.ServiceBus;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using Nile.Common.Extensions;
using Nile.Common.InternalDTOs;
using Nile.Utilities.AzureSdk;
using Azure.Storage.Blobs;

namespace Nile.Utilities.AzureSdk;

internal partial class AzureSdkUtility : UtilityBase, IDisposable, IAsyncDisposable
{
    private readonly IDateUtility _dateUtility;
    private readonly IConfigUtility _configUtility;
    private ServiceBusClient? _serviceBusClient;
    private BlobServiceClient? _blobServiceClient;

    public AzureSdkUtility(ILogger<AzureSdkUtility> logger, IDateUtility dateUtility,
        IConfigUtility configurationUtility) : base(logger)
    {
        _dateUtility = dateUtility;
        _configUtility = configurationUtility;
        // In local/dev we may not have Service Bus or Storage configured; guard to avoid ctor throws
        var serviceBusUrl = _configUtility.ServiceBusUrl;
        if (!string.IsNullOrWhiteSpace(serviceBusUrl))
        {
            _serviceBusClient = new ServiceBusClient(serviceBusUrl, _configUtility.TokenCredential);
        }

        var storageUri = _configUtility.StorageAccountUri;
        if (storageUri != null)
        {
            _blobServiceClient = new BlobServiceClient(storageUri, _configUtility.TokenCredential);
        }
    }

    public async void Dispose()
    {
        if (_serviceBusClient != null)
        {
            await _serviceBusClient.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
   {
       if (_serviceBusClient != null)
       {
           await _serviceBusClient.DisposeAsync();
       }
   }
}