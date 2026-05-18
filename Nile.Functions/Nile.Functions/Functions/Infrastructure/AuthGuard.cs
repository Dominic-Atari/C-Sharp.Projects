using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Nile.Functions.Functions.Infrastructure;

public static class AuthGuard
{
    private const string ApiKeyHeader = "x-api-key";

    // Backwards-compatible helper used by existing functions
    public static Task<HttpResponseData?> IsAuthorized(HttpRequestData req, IConfiguration config) => Authorize(req, config);

    // Returns null when authorized; otherwise an Unauthorized response to return from the function
    public static async Task<HttpResponseData?> Authorize(HttpRequestData req, IConfiguration config)
    {
        // Prefer explicit x-api-key when provided; otherwise fall back to JWT; allow open only when neither configured
        var jwtSecret = GetConfigValue(config, "Jwt:Secret");
        var apiKey = GetConfigValue(config, "ApiKey");

        // If an ApiKey is configured, validate it first (case-insensitive header lookup)
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            var unauthorized = await TryValidateApiKey(req, apiKey, config);
            return unauthorized;
        }

        // If JWT is configured, require a valid bearer token
        if (!string.IsNullOrWhiteSpace(jwtSecret))
        {
            var unauthorized = await TryValidateBearer(req, jwtSecret, config);
            return unauthorized; // null means OK; non-null return it
        }

        // No auth configured: allow (local/dev convenience)
        return null;
    }

    private static async Task<HttpResponseData?> TryValidateApiKey(HttpRequestData req, string configuredKey, IConfiguration config)
    {
        if (!req.Headers.TryGetValues(ApiKeyHeader, out var values))
        {
            return await BuildUnauthorized(req, "ApiKey realm=\"Functions\" header=\"x-api-key\"", config);
        }

        var provided = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(provided) || !TimingSafeEquals(provided, configuredKey))
        {
            return await BuildUnauthorized(req, "ApiKey realm=\"Functions\" header=\"x-api-key\"", config);
        }

        return null;
    }

    private static async Task<HttpResponseData?> TryValidateBearer(HttpRequestData req, string secret, IConfiguration config)
    {
        if (!req.Headers.TryGetValues("Authorization", out var values))
        {
            return await BuildUnauthorized(req, "Bearer", config);
        }

        var header = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return await BuildUnauthorized(req, "Bearer", config);
        }

        var token = header.Substring("Bearer ".Length).Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return await BuildUnauthorized(req, "Bearer", config);
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
            return await BuildUnauthorized(req, "Bearer", config);
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

    private static async Task<HttpResponseData> BuildUnauthorized(HttpRequestData req, string scheme, IConfiguration config)
    {
        var res = req.CreateResponse(HttpStatusCode.Unauthorized);

        // CORS headers are added centrally by CorsMiddleware.

        res.Headers.Add("WWW-Authenticate", scheme);
        await res.WriteStringAsync("Unauthorized");
        return res;
    }

    // Constant-time comparison to mitigate timing leaks
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

    // Read allowed origins from configuration similarly to CorsMiddleware
    private static string[] GetAllowedOrigins(IConfiguration config)
    {
        try
        {
            var configured =
                GetConfigValue(config, "Cors:AllowedOrigins") ??
                config["CORS"] ??
                config["Host:CORS"];

            if (!string.IsNullOrWhiteSpace(configured))
            {
                var parts = configured
                    .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToArray();
                if (parts.Length > 0)
                {
                    return parts;
                }
            }
        }
        catch
        {
            // ignore and fallback
        }

        // fallback to common localhost ports
        return new[] { "http://localhost:8104", "http://localhost:4200" };
    }

    // Support both flat keys and Functions "Values:" section
    private static string? GetConfigValue(IConfiguration config, string key)
    {
        return config[key] ?? config[$"Values:{key}"];
    }
}
