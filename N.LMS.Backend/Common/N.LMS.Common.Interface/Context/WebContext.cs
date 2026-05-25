using System.Security.Claims;

namespace N.LMS.Common.Interface.Context;

public sealed record WebContext
{
    public Claim[] Claims { get; init; } = Array.Empty<Claim>();

    public string? FindClaim(string type) =>
        Claims.FirstOrDefault(c => c.Type == type)?.Value;
}
