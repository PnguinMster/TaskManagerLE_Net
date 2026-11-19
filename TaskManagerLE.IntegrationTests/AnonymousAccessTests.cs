using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskManagerLE.IntegrationTests;

// Smoke test: proves the app starts under TestAppFactory and that [Authorize] pages need a login.
public class AnonymousAccessTests(TestAppFactory factory) : IClassFixture<TestAppFactory>
{
    [Fact]
    public async Task ProjectIndex_AnonymousUser_RedirectsToLogin()
    {
        // Arrange: don't follow redirects, so we can check the redirect itself.
        HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Act
        HttpResponseMessage response = await client.GetAsync("/Project");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString());
    }
}
