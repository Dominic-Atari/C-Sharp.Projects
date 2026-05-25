using Microsoft.Extensions.Configuration;

namespace N.LMS.Database.Factory;

public sealed class ConfigurationUtility
{
    private const string ConnectionStringKey = "NileDbConnectionString";
    private const string ConnectionStringEnvVar = "N_LMS_NILE_DB_CONNECTION_STRING";
    private const string DefaultConnectionString =
        "Server=localhost;Port=3306;Database=NileDB;User=root;Password=root;";

    private readonly IConfiguration _configuration;

    public ConfigurationUtility()
    {
        _configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    public string NileDbConnectionString =>
        Environment.GetEnvironmentVariable(ConnectionStringEnvVar)
        ?? _configuration[ConnectionStringKey]
        ?? _configuration.GetConnectionString("Nile")
        ?? DefaultConnectionString;
}
