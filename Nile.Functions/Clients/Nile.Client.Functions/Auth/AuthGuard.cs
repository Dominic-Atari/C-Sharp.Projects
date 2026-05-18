using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Nile.Client.Functions.Auth;

public static class AuthGuard
{
    private const string ApiKeyHeader = "x-api-key";

    public static Task<HttpResponseData?> IsAuthorized(HttpRequestData req, IConfiguration config) => Authorize(req, config);

    public static async Task<HttpResponseData?> Authorize(HttpRequestData req, IConfiguration config)
    {
        var jwtSecret = GetConfigValue(config, "Jwt:Secret");
        var apiKey = GetConfigValue(config, "ApiKey");

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            return await TryValidateApiKey(req, apiKey, config);
        }

        if (!string.IsNullOrWhiteSpace(jwtSecret))
        {
            return await TryValidateBearer(req, jwtSecret, config);
        }

        // No auth configured: allow (local/dev convenience)
        return null;
    }

    private static async Task<HttpResponseData?> TryValidateApiKey(HttpRequestData req, string configuredKey, IConfiguration config)
    {
        if (!req.Headers.TryGetValues(ApiKeyHeader, out var values))
        {
            return await BuildUnauthorized(req, "ApiKey realm=\"Functions\" header=\"x-api-key\"");
        }

        var provided = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(provided) || !TimingSafeEquals(provided, configuredKey))
        {
            return await BuildUnauthorized(req, "ApiKey realm=\"Functions\" header=\"x-api-key\"");
        }

        return null;
    }

    private static async Task<HttpResponseData?> TryValidateBearer(HttpRequestData req, string secret, IConfiguration config)
    {
        if (!req.Headers.TryGetValues("Authorization", out var values))
        {
            return await BuildUnauthorized(req, "Bearer");
        }

        var header = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return await BuildUnauthorized(req, "Bearer");
        }

        var token = header.Substring("Bearer ".Length).Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return await BuildUnauthorized(req, "Bearer");
        }

        try
        {
            var validationParameters = GetValidationParameters(secret, config);
            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(token, validationParameters, out _);
            return null;
        }
        catch
        {
            return await BuildUnauthorized(req, "Bearer");
        }
    }

    private static TokenValidationParameters GetValidationParameters(string secret, IConfiguration config)
    {
        var issuer = config["Jwt:Issuer"];
        var audience = config["Jwt:Audience"];
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        return new TokenValidationParameters
        {
            ValidIssuer = string.IsNullOrWhiteSpace(issuer) ? null : issuer,
            ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
            ValidAudience = string.IsNullOrWhiteSpace(audience) ? null : audience,
            ValidateAudience = !string.IsNullOrWhiteSpace(audience),
            IssuerSigningKey = key,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    }

    private static async Task<HttpResponseData> BuildUnauthorized(HttpRequestData req, string scheme)
    {
        var res = req.CreateResponse(HttpStatusCode.Unauthorized);
        res.Headers.Add("WWW-Authenticate", scheme);
        await res.WriteStringAsync("Unauthorized");
        return res;
    }

    private static bool TimingSafeEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        var diff = 0;
        for (var i = 0; i < a.Length; i++)
        {
            diff |= a[i] ^ b[i];
        }
        return diff == 0;
    }

    // Support both flat keys and Functions "Values:" section
    private static string? GetConfigValue(IConfiguration config, string key)
    {
        return config[key] ?? config[$"Values:{key}"];
    }
}
