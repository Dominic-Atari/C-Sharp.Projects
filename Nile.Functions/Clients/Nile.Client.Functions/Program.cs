using System.Text.Json;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Nile.Common.Extensions; // IConfigUtility
using Nile.Utilities;          // AddNileServices
using Nile.Managers;           // AddManagersServices
using Nile.Managers.Proxys;    // IProxy<>
using Nile.Utilities.AzureSdk; // AzureSdkUtility
using Nile.Engines.Validation; // ValidationEngine
using Nile.Database.DataContracts; // DatabaseContext

var host = new HostBuilder()
    .ConfigureAppConfiguration((context, config) =>
    {
        var env = context.HostingEnvironment.EnvironmentName;
        config
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();
    })
    // Use defaults for the isolated worker in this Clients host
    // (this project does not reference the AspNetCore HTTP extension)
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        // Emit/accept camelCase JSON everywhere so HTTP responses match the
        // frontend's lowercased-key expectations (e.g. res.token, res.roles).
        // JsonSerializerDefaults.Web sets PropertyNamingPolicy=CamelCase and
        // PropertyNameCaseInsensitive=true in one go.
        services.Configure<WorkerOptions>(options =>
        {
            options.Serializer = new JsonObjectSerializer(
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
        });

        services.AddSingleton<IConfiguration>(sp => context.Configuration);
        services.AddSingleton<ConfigUtility>();
        services.AddSingleton<IConfigUtility>(sp => sp.GetRequiredService<ConfigUtility>());

        // Database context needed by accessors (User, Account, Posts)
        services.AddDbContext<DatabaseContext>();

        services.AddNileServices();
        services.AddManagersServices();

        services.AddSingleton<IValidationEngine, ValidationEngine>();
        services.AddSingleton<IBlobStorageUtility, AzureSdkUtility>();
        services.AddSingleton<IVideoContentService, AzureSdkUtility>();
        services.AddSingleton<IPhotoContentService, AzureSdkUtility>();
        services.AddSingleton<IMessageBusUtility, NoOpMessageBusUtility>();
        services.AddSingleton<ISocialAuthUtility, NoOpSocialAuthUtility>();
        services.AddSingleton<ISocialFeedUtility, NoOpSocialFeedUtility>();
        services.AddSingleton<ISecurityUtility, NoOpSecurityUtility>();
        // Scope the generic proxy to align with scoped managers and dependencies
        services.AddScoped(typeof(IProxy<>), typeof(Proxy<>));
    })
    .Build();

host.Run();