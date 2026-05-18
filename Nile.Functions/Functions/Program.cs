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
using Nile.Functions.Functions.Infrastructure; // CorsMiddleware

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
    // Configure the Functions HTTP pipeline and plug in CORS middleware
    .ConfigureFunctionsWebApplication(builder =>
    {
        // This ensures ALL HTTP responses (success + errors like 401) get CORS headers
        builder.UseMiddleware<CorsMiddleware>();
    })
    .ConfigureServices((context, services) =>
    {
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