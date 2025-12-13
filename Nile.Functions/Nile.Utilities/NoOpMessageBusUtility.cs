using System.Threading.Tasks;
using Nile.Common.InternalDTOs;

namespace Nile.Utilities
{
    // Simple no-op implementation for local/dev where Service Bus is not needed
    public sealed class NoOpMessageBusUtility : IMessageBusUtility
    {
        public Task CreateQueueIfNotExists(string queueName)
        {
            // Intentionally no-op
            return Task.CompletedTask;
        }

        public Task<HealthStatusType> Perform()
        {
            // Report healthy to avoid failing health checks locally
            return Task.FromResult(HealthStatusType.Healthy);
        }

        public Task SendMessageAsync<T>(string queueName, T message) where T : RequestBase
        {
            // Intentionally no-op
            return Task.CompletedTask;
        }
    }
}
