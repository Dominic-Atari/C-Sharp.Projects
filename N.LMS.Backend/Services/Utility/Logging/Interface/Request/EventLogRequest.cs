namespace N.LMS.Utility.Logging.Interface.Request;

public sealed record EventLogRequest : LogRequestBase
{
    public required string EventName { get; init; }
    public IDictionary<string, string>? Properties { get; init; }
    public IDictionary<string, double>? Metrics { get; init; }
}
