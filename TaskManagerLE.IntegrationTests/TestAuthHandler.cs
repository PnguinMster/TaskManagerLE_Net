using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TaskManagerLE.IntegrationTests;

// A fake login for tests: a request that carries the UserIdHeader is signed in as that user.
// This skips the real login form (and its antiforgery token), which isn't what these tests are about.
public class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string UserIdHeader = "X-Test-UserId";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? userId = Request.Headers[UserIdHeader];

        // No header: an anonymous request, so [Authorize] still redirects to the login page.
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(AuthenticateResult.NoResult());

        // NameIdentifier is the claim UserManager.GetUserId and ProjectOwnerHandler read.
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
