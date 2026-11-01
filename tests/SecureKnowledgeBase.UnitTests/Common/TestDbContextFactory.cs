using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SecureKnowledgeBase.Infrastructure.Persistence;

namespace SecureKnowledgeBase.UnitTests.Common;

public static class TestDbContextFactory
{
    public static async Task<(AppDbContext Context, SqliteConnection Connection)> CreateInitializedContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        await DbInitializer.InitializeAsync(context);

        return (context, connection);
    }
}
