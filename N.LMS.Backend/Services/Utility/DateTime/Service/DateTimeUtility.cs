using N.LMS.Common.Interface.Framework;
using N.LMS.Utility.DateTime.Interface;

namespace N.LMS.Utility.DateTime.Service;

internal sealed class DateTimeUtility : ServiceBase, IDateTimeUtility
{
    public DateOnly UtcDateNow => DateOnly.FromDateTime(System.DateTime.UtcNow);
    public System.DateTime UtcNow => System.DateTime.UtcNow;
    public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;
}
