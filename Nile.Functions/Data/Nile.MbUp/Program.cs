using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nile.Common.Extensions;
using Nile.Database.DataContracts;
using Nile.Utilities.AzureSdk;
using Nile.Utilities;

namespace Nile.MbUp;

/// <summary>
/// This CLI tool is a utility that handles message bus-related tasks for Nile. Primarily,
/// without args, it creates developer-specific service bus queues based on the environment username.
/// Optional commands perform other utilities, see command descriptions for details.
/// </summary>
internal static class Program
{
    private static async Task Main(string[]? args)
    {
        IServiceProvider serviceProvider = ConfigureServices();
        var rootCommand = new RootCommand("Create developer-specific message bus queues.");
        rootCommand.SetHandler(() => CreateQueuesHandler(serviceProvider));

        await rootCommand.InvokeAsync(args); 
    }

    private static IServiceProvider ConfigureServices()
    {
        var configuration = BuildConfiguration();
        var config = new ConfigUtility(configuration);

        IServiceCollection services = new ServiceCollection();
        services.AddLogging(logger => logger.AddConsole());
        services.AddScoped<IConfigUtility>(_ => config);
        services.AddScoped<IAsyncDelayer, AsyncDelayer>();
        services.AddScoped<IMessageBusUtility, AzureSdkUtility>();
        
        // TODO: Add Database context and other required services
        services.AddDbContext<DatabaseContext>();
        
        return services.BuildServiceProvider();
    }

    private static IConfiguration BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddUserSecrets<ConfigUtility>(optional: true);

        return builder.Build();
    }

    private static async Task CreateQueuesHandler(IServiceProvider serviceProvider)
    {
        var config = serviceProvider.GetRequiredService<IConfigUtility>();
        Console.WriteLine($"Service Bus URL: {config.ServiceBusUrl}");
        Console.WriteLine($"Username: {config.Username}");
        Console.WriteLine("Queue creation functionality to be implemented.");
        
        // TODO: Implement queue creation logic
        // var queueCreator = new QueueCreator(config, messageBusUtility);
        // await queueCreator.CreateMessageBusQueues();
        
        await Task.CompletedTask;
    }
}
