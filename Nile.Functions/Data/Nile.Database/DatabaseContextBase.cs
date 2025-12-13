using Azure.Core;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Nile.Common.Extensions;

namespace Nile.Database;

public abstract class DatabaseContextBase<TContext> : DbContext
    where TContext : DbContext
{
    private readonly IConfigUtility _config;

    protected DatabaseContextBase(DbContextOptions<TContext> options, IConfigUtility config)
        : base(options)
    {
        _config = config;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (!optionsBuilder.IsConfigured)
        {
            string? connectionString = null;

            try
            {
                connectionString = _config.SqlServerConnectionString;
            }
            catch (InvalidOperationException) when (_config.IsLocalEnvironment)
            {
                // For local development without a SQL connection string, fall back to in-memory provider
                optionsBuilder.UseInMemoryDatabase("NileLocal");
                return;
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                if (_config.IsLocalEnvironment)
                {
                    optionsBuilder.UseInMemoryDatabase("NileLocal");
                    return;
                }

                throw new InvalidOperationException("SqlServerConnectionString not found in configuration.");
            }

            // Always configure using the connection string with resiliency options
            optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: System.TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(60);
            });

            // If using Azure AD token-based auth, add an interceptor to refresh tokens on open
            var containsPassword =
                connectionString.Contains("password", StringComparison.InvariantCultureIgnoreCase) ||
                connectionString.Contains("integrated security", StringComparison.InvariantCultureIgnoreCase);

            if (!containsPassword)
            {
                optionsBuilder.AddInterceptors(
                    new Nile.Database.Interceptors.AzureSqlAccessTokenInterceptor(_config));
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        var result = await base
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        ChangeTracker.Clear();

        return result;
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var result = await base
            .SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken)
            .ConfigureAwait(false);

        ChangeTracker.Clear();

        return result;
    }

    public static string GetAzureAccessToken(IConfigUtility config)
    {
        var accessToken = config.TokenCredential.GetToken(
            new TokenRequestContext(scopes: new[] { config.AccessTokenDatabaseResource }),
            new CancellationToken()
        );

        return accessToken.Token;
    }

    public static bool ShouldUseAzureAccessTokenAuth(IConfigUtility config)
    {
        // If a password is supplied, use sql auth, otherwise use AD Auth
        var containsPassword =
            config.SqlServerConnectionString.Contains("password", StringComparison.InvariantCultureIgnoreCase) ||
            config.SqlServerConnectionString.Contains("integrated security",
                StringComparison.InvariantCultureIgnoreCase);

        return !containsPassword;
    }
}
