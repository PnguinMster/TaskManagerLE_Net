namespace TaskManagerLE.Extensions;

public static class TimeProviderExtensions
{
    // The one definition of "today" for the whole app. Due dates are calendar dates (DateOnly),
    // so we compare them with the local calendar date, not the UTC one.
    public static DateOnly Today(this TimeProvider timeProvider) =>
        DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

    // Short form for the *AtUtc columns, which are plain UTC DateTimes (not DateTimeOffset).
    public static DateTime UtcNow(this TimeProvider timeProvider) =>
        timeProvider.GetUtcNow().UtcDateTime;
}
