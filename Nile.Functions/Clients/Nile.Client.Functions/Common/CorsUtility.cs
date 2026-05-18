using System;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;

namespace Nile.Client.Functions.Common;

internal static class CorsUtility
{
    private static readonly string[] DefaultAllowedOrigins =
    {
        "http://localhost:8104",
        "http://localhost:4200"
    };

    public static void ApplyCorsHeaders(HttpResponseData? response)
    {
        try
        {
            if (response == null) return;

            // Try reading configuration from environment variables that Functions uses
            var configured = Environment.GetEnvironmentVariable("Cors:AllowedOrigins")
                             ?? Environment.GetEnvironmentVariable("CORS")
                             ?? Environment.GetEnvironmentVariable("Host:CORS");

            string? origin = null;
            string[] allowed = DefaultAllowedOrigins;

            if (!string.IsNullOrWhiteSpace(configured))
            {
                var parts = configured.Split(new[] {',', ';', ' '}, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length > 0)
                {
                    allowed = parts;
                    origin = parts[0];
                }
            }

            // If the response already has ACAO, do nothing.
            if (response.Headers.TryGetValues("Access-Control-Allow-Origin", out _)) return;

            var acao = origin ?? allowed[0];
            if (!string.IsNullOrEmpty(acao))
            {
                response.Headers.Add("Access-Control-Allow-Origin", acao);
                if (!string.Equals(acao, "*", StringComparison.Ordinal))
                {
                    if (!response.Headers.TryGetValues("Vary", out _))
                        response.Headers.Add("Vary", "Origin");

                    if (!response.Headers.TryGetValues("Access-Control-Allow-Credentials", out _))
                        response.Headers.Add("Access-Control-Allow-Credentials", "true");
                }
                else
                {
                    if (!response.Headers.TryGetValues("Access-Control-Allow-Credentials", out _))
                        response.Headers.Add("Access-Control-Allow-Credentials", "false");
                }
            }

            if (!response.Headers.TryGetValues("Access-Control-Allow-Headers", out _))
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, x-api-key");

            if (!response.Headers.TryGetValues("Access-Control-Allow-Methods", out _))
                response.Headers.Add("Access-Control-Allow-Methods", "GET,POST,PUT,PATCH,DELETE,OPTIONS");

            if (!response.Headers.TryGetValues("Access-Control-Max-Age", out _))
                response.Headers.Add("Access-Control-Max-Age", "600");
        }
        catch
        {
            // Swallow any errors here — this helper should not throw during error handling
        }
    }
}
