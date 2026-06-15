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
        // Resolve base path and try to locate local.settings.json even when running from bin/*
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        var baseDir = new DirectoryInfo(AppContext.BaseDirectory);
        FileInfo? FindFileUpwards(string fileName)
        {
            var dir = current;
            for (int i = 0; i < 6 && dir != null; i++, dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, fileName);
                if (File.Exists(candidate)) return new FileInfo(candidate);
            }
            return null;
        }
        FileInfo? FindFileUpwardsFromBase(string fileName)
        {
            var dir = baseDir;
            for (int i = 0; i < 6 && dir != null; i++, dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, fileName);
                if (File.Exists(candidate)) return new FileInfo(candidate);
            }
            return null;
        }

        var builder = new ConfigurationBuilder();

        // Try add local.settings.json from current dir or any parent
        var localSettings = FindFileUpwards("local.settings.json")
                            ?? FindFileUpwardsFromBase("local.settings.json");
        if (localSettings != null)
        {
            builder.AddJsonFile(localSettings.FullName, optional: true, reloadOnChange: true);
        }
        else
        {
            // Fallback to template if present (useful for dev defaults)
            var template = FindFileUpwards("local.settings.template.json")
                           ?? FindFileUpwardsFromBase("local.settings.template.json");
            if (template != null)
            {
                builder.AddJsonFile(template.FullName, optional: true, reloadOnChange: true);
            }
        }

        // Environment variables and user-secrets
        builder.AddEnvironmentVariables();
        builder.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);

        // Build initial config
        var config = builder.Build();

        // If Azure Functions-style Values section exists, overlay it with higher precedence
        var valuesSection = config.GetSection("Values");
        if (valuesSection.Exists())
        {
            var finalBuilder = new ConfigurationBuilder()
                .AddConfiguration(config)                 // base
                .AddConfiguration(valuesSection)          // overlay Values/* as flat keys
                .AddEnvironmentVariables()                // env still highest among files
                .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);

            return finalBuilder.Build();
        }

        return config;
    }
}
