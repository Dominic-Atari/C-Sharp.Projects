using System.Text.Json;
using Azure.Core;

namespace Nile.Common.Extensions;

/// <summary>
/// Interface for accessing application configuration settings.
/// Provides connection strings, Azure credentials, and environment-specific configurations.
/// </summary>
public interface IConfigUtility
{
    // ========== Authentication & Security ==========
    
    string AccessTokenDatabaseResource { get; }

    string AzureComputerVisionEndpoint { get; }

    string AzureNotificationHubDefaultFullSharedAccessSignature { get; }

    string AzureNotificationHubName { get; }
    
    string EmailConnectionString { get; }

    Uri AzureSearchEndpoint { get; }

    string AzureSearchIndexName { get; }

    string AzureSearchSemanticConfigurationName { get; }

    bool IsLocalEnvironment { get; }
    
    string ManagedDomain { get; }

    string OpenAiApiKey { get; }

    string OpenAiOrganizationId { get; }

    string OpenAiBaseModel { get; }
    
    //string OpenAiGroceryListModel { get; }
    
    string SqlServerConnectionString { get; }

    string SqlServerTestConnectionString { get; }

    string ServiceBusUrl { get; }
    
    string SocialApiKey { get; }
    
    string SocialApiSecret { get; }
    
    string[] StakeholderEmailAddresses { get; }

    int SubscriptionFreeShuffleLimit { get; }

    int SubscriptionFreeRecipeLimit { get; }

    Uri StorageAccountUri { get; }

    string HealthCheckSecret { get; }

    TokenCredential TokenCredential { get; }

    string Username { get; }

    /// <summary>
    /// Global JSON serializer options for consistent serialization.
    /// </summary>
    JsonSerializerOptions JsonSerializerOptions { get; }
}
