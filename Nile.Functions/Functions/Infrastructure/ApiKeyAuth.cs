using System.Net;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;

namespace Nile.Functions.Functions.Infrastructure;

public static class ApiKeyAuth
{
    private const string HeaderName = "x-api-key";

    public static bool IsAuthorized(HttpRequestData req, IConfiguration config, out HttpResponseData unauthorized)
    {
        unauthorized = null!;

        var configuredKey = config["ApiKey"];
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            // If no key is configured, treat as open for local/dev
            return true;
        }

        if (!req.Headers.TryGetValues(HeaderName, out var values))
        {
            unauthorized = BuildUnauthorized(req);
            return false;
        }

        var provided = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(provided) || !TimingSafeEquals(provided, configuredKey))
        {
            unauthorized = BuildUnauthorized(req);
            return false;
        }

        return true;
    }

    private static HttpResponseData BuildUnauthorized(HttpRequestData req)
    {
        var res = req.CreateResponse(HttpStatusCode.Unauthorized);
        res.Headers.Add("WWW-Authenticate", "ApiKey realm=\"Functions\" header=\"x-api-key\"");
        res.WriteString("Unauthorized");
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
}
