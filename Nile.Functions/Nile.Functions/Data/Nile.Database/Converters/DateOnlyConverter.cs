using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Nile.Database.Converters;

/// <summary>
/// Converts DateOnly to DateTime for EF Core storage and back.
/// Required because some database providers don't natively support DateOnly type.
/// </summary>
public class DateOnlyConverter : ValueConverter<DateOnly, DateTime>
{
    public DateOnlyConverter() : base(
        dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
        dateTime => DateOnly.FromDateTime(dateTime))
    {
    }
}
