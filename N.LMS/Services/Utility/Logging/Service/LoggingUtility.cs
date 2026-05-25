using Microsoft.Extensions.Logging;
using N.LMS.Common.Interface.Framework;
using N.LMS.Utility.Logging.Interface;
using N.LMS.Utility.Logging.Interface.Request;

namespace N.LMS.Utility.Logging.Service;

internal sealed class LoggingUtility : ServiceBase, ILoggingUtility
{
    private readonly ILogger<LoggingUtility> _logger;

    public LoggingUtility(ILogger<LoggingUtility> logger) => _logger = logger;

    public void Log(LogRequestBase request)
    {
        switch (request)
        {
            case ExceptionLogRequest ex:
                _logger.LogError(ex.Exception, "{Message}", ex.Message ?? ex.Exception.Message);
                break;
            case TraceLogRequest trace:
                _logger.Log(ToMsLevel(trace.SeverityLevel), "{Message}", trace.Message);
                break;
            case EventLogRequest evt:
                using (_logger.BeginScope(evt.Properties ?? new Dictionary<string, string>()))
                {
                    _logger.LogInformation("Event {EventName}", evt.EventName);
                }
                break;
            default:
                _logger.LogInformation("Unhandled log request: {Type}", request.GetType().Name);
                break;
        }
    }

    private static LogLevel ToMsLevel(SeverityLevel level) => level switch
    {
        SeverityLevel.Trace       => LogLevel.Trace,
        SeverityLevel.Debug       => LogLevel.Debug,
        SeverityLevel.Information => LogLevel.Information,
        SeverityLevel.Warning     => LogLevel.Warning,
        SeverityLevel.Error       => LogLevel.Error,
        SeverityLevel.Critical    => LogLevel.Critical,
        _                         => LogLevel.Information
    };
}
