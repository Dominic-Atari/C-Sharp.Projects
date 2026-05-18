using System.Data.Common;
using Azure.Core;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nile.Common.Extensions;

namespace Nile.Database.Interceptors;

/// <summary>
/// EF Core connection interceptor that sets/refreshes the Azure AD access token
/// for SQL connections whenever a connection is opened.
/// </summary>
public sealed class AzureSqlAccessTokenInterceptor : DbConnectionInterceptor
{
    private readonly IConfigUtility _config;

    public AzureSqlAccessTokenInterceptor(IConfigUtility config)
    {
        _config = config;
    }

    public override InterceptionResult ConnectionOpening(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        if (connection is SqlConnection sqlConnection)
        {
            var token = _config.TokenCredential.GetToken(
                new TokenRequestContext(new[] { _config.AccessTokenDatabaseResource }),
                default);

            sqlConnection.AccessToken = token.Token;
        }

        return base.ConnectionOpening(connection, eventData, result);
    }

    public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        if (connection is SqlConnection sqlConnection)
        {
            var token = await _config.TokenCredential.GetTokenAsync(
                new TokenRequestContext(new[] { _config.AccessTokenDatabaseResource }),
                cancellationToken).ConfigureAwait(false);

            sqlConnection.AccessToken = token.Token;
        }

        return await base.ConnectionOpeningAsync(connection, eventData, result, cancellationToken);
    }
}
