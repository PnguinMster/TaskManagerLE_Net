using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskManagerLE.Data;

namespace TaskManagerLE.IntegrationTests;

// Starts the real app in memory, with SQL Server swapped for an in-memory SQLite database.
// Use it with IClassFixture<TestAppFactory>: xUnit then builds one app per test class.
public sealed class TestAppFactory : WebApplicationFactory<Program>
{
    // An in-memory SQLite database only exists while its connection is open (same idea as TestDatabase).
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TestAppFactory() => _connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Program.cs sees "Testing" and calls EnsureCreatedAsync instead of MigrateAsync.
        builder.UseEnvironment("Testing");

        // Runs after Program.cs registered its services, so these registrations win.
        builder.ConfigureTestServices(services =>
        {
            // Since EF Core 9, AddDbContext stores the UseSqlServer(...) setup here.
            // Removing only DbContextOptions would leave it in place and register two providers.
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));

            // Read the user from TestAuthHandler instead of the Identity cookie. Only the "authenticate"
            // default changes: challenges still go to the cookie scheme, which redirects to /Account/Login.
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            services.Configure<AuthenticationOptions>(options =>
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName);
        });
    }

    // A client whose requests are signed in as this user. Redirects aren't followed, so tests see them.
    public HttpClient CreateClientFor(string userId)
    {
        HttpClient client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
