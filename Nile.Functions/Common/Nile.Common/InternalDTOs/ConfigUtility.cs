using Microsoft.Extensions.Configuration;
using Nile.Common.Extensions;
using System;

public class ConfigUtility : IConfigUtility
{
    private readonly IConfiguration _configuration;

    public ConfigUtility(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string SqlServerConnectionString =>
    Environment.GetEnvironmentVariable("SqlServerConnectionString")
    ?? _configuration["SqlServerConnectionString"]
    ?? _configuration["Values:SqlServerConnectionString"]
    ?? _configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("SqlServerConnectionString not found in configuration.");
    public Azure.Core.TokenCredential TokenCredential => 
        new Azure.Identity.DefaultAzureCredential();

    public string AccessTokenDatabaseResource => 
        _configuration["AccessTokenDatabaseResource"] 
        ?? "https://database.windows.net/.default";

    public string AzureComputerVisionEndpoint =>
        _configuration["AzureComputerVisionEndpoint"]
        ?? _configuration["Values:AzureComputerVisionEndpoint"]
        ?? string.Empty;

    public string HealthCheckSecret => _configuration["HealthCheckSecret"] ?? string.Empty;
    public string SocialApiKey => _configuration["SocialApiKey"] ?? string.Empty;
    public string SocialApiSecret => _configuration["SocialApiSecret"] ?? string.Empty;
    public string[] StakeholderEmailAddresses
    {
        get
        {
            // Try array from section: StakeholderEmailAddresses:0, StakeholderEmailAddresses:1, ...
            var section = _configuration.GetSection("StakeholderEmailAddresses");
            if (section.Exists())
            {
                var children = section.GetChildren().Select(c => c.Value).Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();
                if (children.Length > 0)
                    return children;
            }

            // Fallback to comma-separated string
            var csv = _configuration["StakeholderEmailAddresses"];
            return string.IsNullOrWhiteSpace(csv)
                ? Array.Empty<string>()
                : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }
    public int SubscriptionFreeShuffleLimit =>
        int.TryParse(_configuration["SubscriptionFreeShuffleLimit"], out var v) ? v : 0;
    public int SubscriptionFreeRecipeLimit =>
        int.TryParse(_configuration["SubscriptionFreeRecipeLimit"], out var v) ? v : 0;
    public string SqlServerTestConnectionString => _configuration["SqlServerTestConnectionString"] ?? string.Empty;
    public string AzureNotificationHubName => _configuration["AzureNotificationHubName"] ?? string.Empty;
    public string AzureNotificationHubDefaultFullSharedAccessSignature => 
        _configuration["AzureNotificationHubDefaultFullSharedAccessSignature"] ?? string.Empty;
    public string EmailConnectionString => _configuration["EmailConnectionString"] ?? string.Empty;
    public Uri AzureSearchEndpoint =>
        Uri.TryCreate(_configuration["AzureSearchEndpoint"], UriKind.Absolute, out var uri)
            ? uri
            : new Uri("https://default.search.windows.net");
    public string AzureSearchIndexName =>
        _configuration["AzureSearchIndexName"]
        ?? _configuration["Values:AzureSearchIndexName"]
        ?? string.Empty;
    public string AzureSearchSemanticConfigurationName =>
        _configuration["AzureSearchSemanticConfigurationName"]
        ?? _configuration["Values:AzureSearchSemanticConfigurationName"]
        ?? string.Empty;

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
    public string OpenAiApiKey =>
        _configuration["OpenAiApiKey"]
        ?? _configuration["Values:OpenAiApiKey"]
        ?? _configuration["OpenAi:ApiKey"]
        ?? string.Empty;
    public string OpenAiOrganizationId =>
        _configuration["OpenAiOrganizationId"]
        ?? _configuration["Values:OpenAiOrganizationId"]
        ?? _configuration["OpenAi:OrganizationId"]
        ?? string.Empty;
    public string OpenAiBaseModel =>
        _configuration["OpenAiBaseModel"]
        ?? _configuration["Values:OpenAiBaseModel"]
        ?? _configuration["OpenAi:BaseModel"]
        ?? string.Empty;

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