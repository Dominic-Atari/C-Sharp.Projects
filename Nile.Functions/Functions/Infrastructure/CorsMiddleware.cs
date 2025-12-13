using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Nile.Functions.Functions.Infrastructure;

/// <summary>
/// Robust CORS middleware for local/dev to ensure all HTTP responses
/// (including error responses) include Access-Control-Allow-* headers.
/// Adds lightweight debug logging when an ILogger is available.
/// </summary>
public class CorsMiddleware : IFunctionsWorkerMiddleware
{
    // Fallback origins if none configured
    private static readonly string[] FallbackAllowedOrigins =
    {
        "http://localhost:8104",
        "http://localhost:4200"
    };

    private readonly string[] _allowedOrigins;
    private readonly ILogger<CorsMiddleware>? _logger;

    public CorsMiddleware(IConfiguration config, ILogger<CorsMiddleware>? logger = null)
    {
        _logger = logger;
        _allowedOrigins = GetAllowedOrigins(config);
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        // Try to read request if available (in most cases it will be present)
        var req = await context.GetHttpRequestDataAsync();

        // Handle preflight quickly when we have a request
        if (req != null && string.Equals(req.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            var preflight = req.CreateResponse(HttpStatusCode.OK);
            AddCorsHeaders(preflight, req.Headers.TryGetValues("Origin", out var o) ? o.FirstOrDefault() : null, _allowedOrigins);
            context.GetInvocationResult().Value = preflight;
            _logger?.LogDebug("CorsMiddleware: handled preflight for Origin={Origin}", req.Headers.TryGetValues("Origin", out var ox) ? ox.FirstOrDefault() : null);
            return;
        }

        // Let the function execute
        await next(context);

        // Try to get the response from the pipeline; if null, check invocation result
        var res = context.GetHttpResponseData() ?? context.GetInvocationResult().Value as HttpResponseData;
        if (res == null)
        {
            _logger?.LogDebug("CorsMiddleware: no HttpResponseData to add CORS headers to (InvocationId={InvocationId})", context.InvocationId);
            return;
        }

        // Determine origin: prefer actual request origin if present, otherwise use first allowed origin as fallback
        string? origin = null;
        if (req != null && req.Headers.TryGetValues("Origin", out var origins)) origin = origins.FirstOrDefault();
        if (string.IsNullOrEmpty(origin)) origin = _allowedOrigins.FirstOrDefault();

        AddCorsHeaders(res, origin, _allowedOrigins);
        _logger?.LogDebug("CorsMiddleware: added CORS headers for Origin={Origin} (InvocationId={InvocationId})", origin, context.InvocationId);
    }

    private static void AddCorsHeaders(HttpResponseData res, string? origin, string[] allowedOrigins)
    {
        var allowed = allowedOrigins ?? Array.Empty<string>();

        // Determine Access-Control-Allow-Origin value:
        // - If the request Origin is explicitly allowed, echo it.
        // - Otherwise, if allowedOrigins contains "*", allow any origin (dev only).
        // - If neither, don't set ACAO (browser will block cross-origin requests).
        string? acao = null;
        if (!string.IsNullOrEmpty(origin) && allowed.Any(o => string.Equals(o, origin, StringComparison.OrdinalIgnoreCase)))
        {
            acao = origin;
        }
        else if (allowed.Any(o => o == "*"))
        {
            acao = "*";
        }

        if (acao != null && !res.Headers.TryGetValues("Access-Control-Allow-Origin", out _))
        {
            res.Headers.Add("Access-Control-Allow-Origin", acao);
            // When echoing a specific origin, include Vary and allow credentials.
            if (!string.Equals(acao, "*", StringComparison.Ordinal))
            {
                if (!res.Headers.TryGetValues("Vary", out _))
                    res.Headers.Add("Vary", "Origin");

                if (!res.Headers.TryGetValues("Access-Control-Allow-Credentials", out _))
                    res.Headers.Add("Access-Control-Allow-Credentials", "true");
            }
            else
            {
                // If permissive wildcard is used, do not advertise support for credentials.
                if (!res.Headers.TryGetValues("Access-Control-Allow-Credentials", out _))
                    res.Headers.Add("Access-Control-Allow-Credentials", "false");
            }
        }

        // Always advertise the headers/methods we support (these are safe defaults)
        if (!res.Headers.TryGetValues("Access-Control-Allow-Headers", out _))
            res.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, x-api-key");

        if (!res.Headers.TryGetValues("Access-Control-Allow-Methods", out _))
            res.Headers.Add("Access-Control-Allow-Methods", "GET,POST,PUT,PATCH,DELETE,OPTIONS");

        if (!res.Headers.TryGetValues("Access-Control-Max-Age", out _))
            res.Headers.Add("Access-Control-Max-Age", "600");
    }

    private static string[] GetAllowedOrigins(IConfiguration config)
    {
        try
        {
            var configured = config["Cors:AllowedOrigins"] ?? config["CORS"] ?? config["Host:CORS"];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                var parts = configured
                    .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToArray();
                if (parts.Length > 0) return parts;
            }
        }
        catch
        {
            // ignore and fallback
        }

        return FallbackAllowedOrigins;
    }
}
