namespace TaskMangerLE.Extensions;

public static class UserNameExtensions
{
    // Avatar text: up to two initials from the user name ("jane.doe@x.com" -> "JD", "marco" -> "M").
    // Only the part before '@' is used, so an email-style name doesn't add the domain's letter.
    public static string ToInitials(this string? userName)
    {
        string localPart = (userName ?? "").Split('@', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";

        string initials = string.Concat(
            localPart
                .Split(['.', ' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .Select(part => char.ToUpperInvariant(part[0])));

        return initials.Length > 0 ? initials : "?";
    }
}
