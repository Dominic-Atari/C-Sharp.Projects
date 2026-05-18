using System.Reflection;
using Nile.Common.Extensions;
using Nile.Utilities;

namespace Nile.MbUp;

/// <summary>
/// Utility for creating developer-specific message bus queues based on the environment username.
/// </summary>
public class QueueCreator
{
    private readonly IConfigUtility _configUtility;
    private readonly IMessageBusUtility _messageBusUtility;
    
    public QueueCreator(IConfigUtility configUtility, IMessageBusUtility messageBusUtility)
    {
        _configUtility = configUtility;
        _messageBusUtility = messageBusUtility;
    }
    
    public async Task CreateMessageBusQueues()
    {
        var postfix = $"-{_configUtility.Username}";
        Console.WriteLine($"Creating message bus queues at {_configUtility.ServiceBusUrl}...");
        Console.WriteLine($"Using machine username postfix of '{postfix}'");

        var queueNames = GetQueueNames();
        foreach (var queueName in queueNames)
        {
            Console.WriteLine($"Creating {queueName}{postfix} queue...");
            await _messageBusUtility.CreateQueueIfNotExists(queueName);
            Console.WriteLine("Success.");
        }

        Console.WriteLine("Successfully created queues.");
    }

    private static string[] GetQueueNames()
    {
        var queueNames = typeof(Queues)
            .GetFields(BindingFlags.Static | BindingFlags.Public)
            .Where(x => x.IsLiteral && !x.IsInitOnly)
            .Select(x => x.GetValue(null)).Cast<string>().ToArray();

        return queueNames;
    }
}
