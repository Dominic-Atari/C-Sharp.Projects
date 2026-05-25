namespace N.LMS.Utility.Logging.Interface.Request;

public sealed record TraceLogRequest : LogRequestBase
{
    public required string Message { get; init; }
    public required SeverityLevel SeverityLevel { get; init; }
}
