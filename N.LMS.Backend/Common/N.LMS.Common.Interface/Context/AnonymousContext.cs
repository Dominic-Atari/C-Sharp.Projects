namespace N.LMS.Common.Interface.Context;

public sealed record AnonymousContext
{
    public DateTime ArrivedUtc { get; init; } = DateTime.UtcNow;
}
