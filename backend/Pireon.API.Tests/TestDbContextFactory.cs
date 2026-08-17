using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Pireon.API.Data;

namespace Pireon.API.Tests;

internal static class TestDbContextFactory
{
    public static SqliteConnection CreateInMemoryConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    public static AppDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
