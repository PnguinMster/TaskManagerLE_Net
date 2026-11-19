using Microsoft.AspNetCore.Identity;

namespace TaskManagerLE.Models;

// Shared by the Register and Profile forms, so both accept the same usernames and emails.
public static class UserConstraints
{
    public const int UserNameMinLength = 3;
    public const int UserNameMaxLength = 30;

    // Identity's own column limit for Email (and UserName) in the database.
    public const int EmailMaxLength = 256;
}

/// The application's Identity user. Intentionally declares no members of its own
public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAtUtc { get; set; }
}
