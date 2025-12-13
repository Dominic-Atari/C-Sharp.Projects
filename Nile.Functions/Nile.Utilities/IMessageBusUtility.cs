using Nile.Common.InternalDTOs;

namespace Nile.Utilities
{
    public interface IMessageBusUtility
    {
        Task CreateQueueIfNotExists(string queueName);
        Task<HealthStatusType> Perform();
        Task SendMessageAsync<T>(string queueName, T message) where T : RequestBase;
    }
}