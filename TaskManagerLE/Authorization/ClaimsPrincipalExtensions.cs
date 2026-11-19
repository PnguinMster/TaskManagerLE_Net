using System.Security.Claims;

namespace TaskManagerLE.Authorization;

public static class ClaimsPrincipalExtensions
{
    // Admins see every user's items; one name for that check instead of repeating the role string everywhere.
    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(Roles.Admin);
}
