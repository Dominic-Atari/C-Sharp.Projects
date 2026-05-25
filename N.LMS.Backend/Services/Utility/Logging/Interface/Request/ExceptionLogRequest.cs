namespace N.LMS.Utility.Logging.Interface.Request;

public sealed record ExceptionLogRequest : LogRequestBase
{
    public required Exception Exception { get; init; }
    public string? Message { get; init; }
}
