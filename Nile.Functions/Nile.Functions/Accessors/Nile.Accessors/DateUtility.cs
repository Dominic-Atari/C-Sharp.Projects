using Microsoft.Extensions.Logging;
using Nile.Utilities;
using Nile.Utilities.AzureSdk;

namespace Nile.Accessors;

public class DateUtility : UtilityBase, IDateUtility
{
    public DateUtility(ILogger<DateUtility> logger) : base(logger)
    {
    }

    public DateOnly CetralTimeNow => ToCentralDateOnly(DateTime.UtcNow);

    public DateTime UtcNow => DateTime.UtcNow;
    public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;

    private DateOnly ToCentralDateOnly(DateTime utcNow)
    {
        try
        {
            // Windows: "Central Standard Time"; Linux often uses "America/Chicago"
            // Prefer Windows ID; fallback to IANA if needed.
            TimeZoneInfo? cst = null;
            try
            {
                cst = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                cst = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
            }

            var centralTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, cst!);
            return DateOnly.FromDateTime(centralTime);
        }
        catch
        {
            // Fallback to UTC date if timezone not found
            return DateOnly.FromDateTime(utcNow);
        }
    }
}
