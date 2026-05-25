using N.LMS.Common.Interface.Framework;
using N.LMS.Utility.Logging.Interface.Request;

namespace N.LMS.Utility.Logging.Interface;

public interface ILoggingUtility : IUtility
{
    void Log(LogRequestBase request);
}
