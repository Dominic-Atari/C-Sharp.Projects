namespace N.LMS.Common.Interface.Result;

public abstract record HealthCheckResultBase : ResultBase
{
    public bool Healthy { get; init; }
    public string? Message { get; init; }
    public DateTime CheckedAtUtc { get; init; } = DateTime.UtcNow;
}
