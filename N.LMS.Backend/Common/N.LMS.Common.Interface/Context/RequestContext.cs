namespace N.LMS.Common.Interface.Context;

public sealed record RequestContext
{
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
    public Guid? UserId { get; init; }
    public string? UserEmail { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
