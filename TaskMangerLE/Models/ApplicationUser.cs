using Microsoft.AspNetCore.Identity;

namespace TaskMangerLE.Models;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}