using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Nile.Functions.Functions.Infrastructure;

/// <summary>
/// Fallback handler for browser CORS preflight (OPTIONS) requests so we never return 404 on preflight.
/// </summary>
public class CorsPreflightFunction
{
    private static readonly string[] AllowedOrigins =
    {
        "http://localhost:8104",
        "http://localhost:4200"
    };

    [Function("CorsPreflight")]
    public HttpResponseData Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "options", Route = "{*any}")] HttpRequestData req)
    {
        var res = req.CreateResponse(HttpStatusCode.OK);

        if (req.Headers.TryGetValues("Origin", out var origins))
        {
            var origin = origins.FirstOrDefault();
            if (origin != null && AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
            {
                res.Headers.Add("Access-Control-Allow-Origin", origin);
                res.Headers.Add("Vary", "Origin");
            }
        }

        res.Headers.Add("Access-Control-Allow-Credentials", "true");

        if (req.Headers.TryGetValues("Access-Control-Request-Headers", out var requestedHeaders))
        {
            res.Headers.Add("Access-Control-Allow-Headers", string.Join(", ", requestedHeaders));
        }
        else
        {
            res.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, x-api-key");
        }

        res.Headers.Add("Access-Control-Allow-Methods", "GET,POST,PUT,PATCH,DELETE,OPTIONS");

        return res;
    }
}
