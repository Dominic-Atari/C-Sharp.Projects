using Microsoft.Extensions.Configuration;
using Nile.Common.Extensions;

public class ConfigUtility : IConfigUtility
{
    private readonly IConfiguration _configuration;

    public ConfigUtility(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string SqlServerConnectionString => 
        _configuration["Values:SqlServerConnectionString"]
        ?? _configuration["SqlServerConnectionString"] 
        ?? _configuration.GetConnectionString("DefaultConnection") 
        ?? throw new InvalidOperationException("SqlServerConnectionString not found in configuration.");

    public Azure.Core.TokenCredential TokenCredential => 
        new Azure.Identity.DefaultAzureCredential();

    public string AccessTokenDatabaseResource => 
        _configuration["AccessTokenDatabaseResource"] 
        ?? "https://database.windows.net/.default";

    public string AzureComputerVisionEndpoint { get; }

    public string HealthCheckSecret => _configuration["HealthCheckSecret"] ?? string.Empty;
    public string SocialApiKey => _configuration["SocialApiKey"] ?? string.Empty;
    public string SocialApiSecret => _configuration["SocialApiSecret"] ?? string.Empty;
    public string[] StakeholderEmailAddresses { get; }
    public int SubscriptionFreeShuffleLimit { get; }
    public int SubscriptionFreeRecipeLimit { get; }
    public string SqlServerTestConnectionString => _configuration["SqlServerTestConnectionString"] ?? string.Empty;
    public string AzureNotificationHubName => _configuration["AzureNotificationHubName"] ?? string.Empty;
    public string AzureNotificationHubDefaultFullSharedAccessSignature => 
        _configuration["AzureNotificationHubDefaultFullSharedAccessSignature"] ?? string.Empty;
    public string EmailConnectionString => _configuration["EmailConnectionString"] ?? string.Empty;
    public Uri AzureSearchEndpoint { get; }
    public string AzureSearchIndexName { get; }
    public string AzureSearchSemanticConfigurationName { get; }

    public Uri StorageAccountUri => 
        new Uri(_configuration["AppSettings:Azure:StorageAccountUri"] 
        ?? _configuration["StorageAccountUri"] 
        ?? "https://default.blob.core.windows.net");
    
    public string ServiceBusUrl => 
        _configuration["AppSettings:Azure:ServiceBusUrl"] 
        ?? _configuration["ServiceBusUrl"] 
        ?? string.Empty;
    
    public bool IsLocalEnvironment => 
        string.Equals(_configuration["AppSettings:Environment"], "Local", StringComparison.OrdinalIgnoreCase)
        || bool.Parse(_configuration["IsLocalEnvironment"] ?? "true");
    
    public string ManagedDomain => _configuration["ManagedDomain"] ?? "localhost";
    public string OpenAiApiKey { get; }
    public string OpenAiOrganizationId { get; }
    public string OpenAiBaseModel { get; }

    public string Username => 
        _configuration["Username"] 
        ?? Environment.GetEnvironmentVariable("USERNAME") 
        ?? Environment.GetEnvironmentVariable("USER") 
        ?? "unknown";
    
    public System.Text.Json.JsonSerializerOptions JsonSerializerOptions => new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    
}