using Microsoft.AspNetCore.Identity;

namespace TaskManagerLE.Models;

/// The application's Identity user. Intentionally declares no members of its own
public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAtUtc { get; set; }
}