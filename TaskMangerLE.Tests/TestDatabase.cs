using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskMangerLE.Data;

namespace TaskMangerLE.Tests;

// A real SQLite database that lives in memory for the length of one test.
// An in-memory SQLite database only exists while its connection is open,
// so we open one connection up front and close it in Dispose.
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public TestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Builds the tables from the EF model (not from the SQL Server migrations).
        using ApplicationDbContext context = CreateContext();
        context.Database.EnsureCreated();
    }

    // A new context each time, like each web request gets its own.
    // Use separate contexts for seeding, acting and checking, so a check reads
    // what is really in the database, not objects the context still remembers.
    public ApplicationDbContext CreateContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}
