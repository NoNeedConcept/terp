namespace Terp;

/// <summary>
/// Predicate helpers on <see cref="DateTime"/>.
/// </summary>
/// <remarks>
/// Methods that need the current time take an optional <see cref="TimeProvider"/> (default
/// <see cref="TimeProvider.System"/>). <see cref="DateTimeKind"/> is honoured as follows:
/// <see cref="DateTimeKind.Utc"/> values are UTC, <see cref="DateTimeKind.Local"/> values are local time of the
/// system, and <see cref="DateTimeKind.Unspecified"/> values are interpreted as local time of the provider's
/// <see cref="TimeProvider.LocalTimeZone"/>. Calendar predicates (<see cref="IsWeekend"/>, <see cref="IsWorkday"/>)
/// and <see cref="IsBetween"/> look at the value as given and ignore the kind.
/// </remarks>
public static class DateTimeExtensions
{
    /// <summary>
    /// Whether the value falls on the current local calendar day of the provider's time zone.
    /// UTC values are converted to local time first; other kinds are compared as wall-clock time.
    /// </summary>
    /// <param name="dateTime">The value to test.</param>
    /// <param name="timeProvider">The time source, or <see langword="null"/> for <see cref="TimeProvider.System"/>.</param>
    /// <returns><see langword="true"/> when the value is today; otherwise <see langword="false"/>.</returns>
    public static bool IsToday(this DateTime dateTime, TimeProvider? timeProvider = null)
    {
        timeProvider ??= TimeProvider.System;
        var local = dateTime.Kind == DateTimeKind.Utc
            ? TimeZoneInfo.ConvertTimeFromUtc(dateTime, timeProvider.LocalTimeZone)
            : dateTime;
        return local.Date == timeProvider.GetLocalNow().Date;
    }

    /// <summary>
    /// Whether the value lies within the inclusive range; the bounds may be given in either order.
    /// The values are compared as-is, <see cref="DateTime.Kind"/> is ignored.
    /// </summary>
    /// <param name="dateTime">The value to test.</param>
    /// <param name="lowerBound">One bound of the range.</param>
    /// <param name="upperBound">The other bound of the range.</param>
    /// <returns><see langword="true"/> when the value lies within the range; otherwise <see langword="false"/>.</returns>
    public static bool IsBetween(this DateTime dateTime, DateTime lowerBound, DateTime upperBound)
    {
        if (lowerBound > upperBound)
        {
            (lowerBound, upperBound) = (upperBound, lowerBound);
        }

        return lowerBound <= dateTime && dateTime <= upperBound;
    }

    /// <summary>
    /// Whether the value falls on Monday to Friday. Public holidays are not considered.
    /// </summary>
    /// <param name="dateTime">The value to test.</param>
    /// <returns><see langword="true"/> when the value falls on a weekday; otherwise <see langword="false"/>.</returns>
    public static bool IsWorkday(this DateTime dateTime)
    {
        return !dateTime.IsWeekend();
    }

    /// <summary>
    /// Whether the value falls on Saturday or Sunday, evaluated on the value as given.
    /// </summary>
    /// <param name="dateTime">The value to test.</param>
    /// <returns><see langword="true"/> when the value falls on Saturday or Sunday; otherwise <see langword="false"/>.</returns>
    public static bool IsWeekend(this DateTime dateTime)
    {
        return dateTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    }

    /// <summary>
    /// Whether the instant is strictly after the current time.
    /// </summary>
    /// <param name="dateTime">The value to test; see the type remarks for <see cref="DateTimeKind"/> handling.</param>
    /// <param name="timeProvider">The time source, or <see langword="null"/> for <see cref="TimeProvider.System"/>.</param>
    /// <returns><see langword="true"/> when the instant is after the current time; otherwise <see langword="false"/>.</returns>
    public static bool IsInFuture(this DateTime dateTime, TimeProvider? timeProvider = null)
    {
        timeProvider ??= TimeProvider.System;
        return ToUtc(dateTime, timeProvider) > timeProvider.GetUtcNow().UtcDateTime;
    }

    /// <summary>
    /// Whether the instant is strictly before the current time.
    /// </summary>
    /// <param name="dateTime">The value to test; see the type remarks for <see cref="DateTimeKind"/> handling.</param>
    /// <param name="timeProvider">The time source, or <see langword="null"/> for <see cref="TimeProvider.System"/>.</param>
    /// <returns><see langword="true"/> when the instant is before the current time; otherwise <see langword="false"/>.</returns>
    public static bool IsPast(this DateTime dateTime, TimeProvider? timeProvider = null)
    {
        timeProvider ??= TimeProvider.System;
        return ToUtc(dateTime, timeProvider) < timeProvider.GetUtcNow().UtcDateTime;
    }

    private static DateTime ToUtc(DateTime dateTime, TimeProvider timeProvider)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => new DateTimeOffset(dateTime, timeProvider.LocalTimeZone.GetUtcOffset(dateTime)).UtcDateTime,
        };
    }
}
