using N.LMS.Common.Interface.Framework;

namespace N.LMS.Utility.DateTime.Interface;

public interface IDateTimeUtility : IUtility
{
    DateOnly UtcDateNow { get; }
    System.DateTime UtcNow { get; }
    DateTimeOffset UtcNowOffset { get; }
}
