using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Nile.Common;

/// <summary>
/// Builds IConfiguration for application settings.
/// </summary>
public static class ConfigBuilder
{
    /// <summary>
    /// Builds a configuration from local.settings.json, environment variables, and user secrets.
    /// Handles both flat and Azure Functions-style nested "Values" format.
    /// </summary>
    public static IConfiguration Build()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        var config = builder.Build();

        // Handle Azure Functions local.settings.json format with nested "Values" section
        var valuesSection = config.GetSection("Values");
        if (valuesSection.Exists())
        {
            builder = new ConfigurationBuilder()
                .AddConfiguration(valuesSection)
                .AddEnvironmentVariables()
                .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);
            
            return builder.Build();
        }

        builder.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);
        return builder.Build();
    }
}
