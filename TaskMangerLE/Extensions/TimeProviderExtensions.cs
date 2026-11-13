namespace TaskMangerLE.Extensions;

public static class TimeProviderExtensions
{
    // The one definition of "today" for the whole app. Due dates are calendar dates (DateOnly),
    // so we compare them with the local calendar date, not the UTC one.
    public static DateOnly Today(this TimeProvider timeProvider) =>
        DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
}
