using Nile.Common.InternalDTOs;
using Nile.Utilities;

namespace Nile.Utilities.AzureSdk;

internal partial class AzureSdkUtility : IMessageBusUtility
{
    public Task CreateQueueIfNotExists(string queueName)
    {
        throw new NotImplementedException();
    }

    public Task<HealthStatusType> Perform()
    {
        throw new NotImplementedException();
    }

    public Task SendMessageAsync<T>(string queueName, T message) where T : RequestBase
    {
        throw new NotImplementedException();
    }
}