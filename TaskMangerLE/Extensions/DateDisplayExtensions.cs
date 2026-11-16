namespace TaskMangerLE.Extensions;

// The date formats people see, in one place, so every page writes dates the same way.
// DateTime values are stored in UTC: convert them with ToLocalTime() before formatting.
// (Machine formats like "yyyy-MM-dd" in data-* attributes for site.js are not display formats.)
public static class DateDisplayExtensions
{
    private const string ShortFormat = "MMM d";              // "Nov 20": due dates on cards and rows
    private const string LongFormat = "MMM d, yyyy";         // "Nov 20, 2026": headings, "Created ..." lines
    private const string DateTimeFormat = "MMM d, h:mm tt";  // "Nov 20, 3:45 PM": when something happened

    public static string ToShortDisplay(this DateOnly date) => date.ToString(ShortFormat);

    public static string ToLongDisplay(this DateOnly date) => date.ToString(LongFormat);

    public static string ToLongDisplay(this DateTime dateTime) => dateTime.ToString(LongFormat);

    public static string ToDateTimeDisplay(this DateTime dateTime) => dateTime.ToString(DateTimeFormat);
}
