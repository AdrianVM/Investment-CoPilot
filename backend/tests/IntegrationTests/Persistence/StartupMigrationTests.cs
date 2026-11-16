using InvestmentCoPilot.IntegrationTests.Infrastructure;
using Npgsql;

namespace InvestmentCoPilot.IntegrationTests.Persistence;

[Collection(IntegrationTestCollection.Name)]
public class StartupMigrationTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Development_startup_creates_every_module_schema_with_its_own_migrations_history()
    {
        var connectionString = await postgres.CreateEmptyDatabaseAsync();
        await using var factory = new ApiFactory(connectionString);
        _ = factory.Services; // starts the host, which applies migrations in Development

        Assert.Equal(["assessment", "identity", "market_data"], await MigrationHistorySchemasAsync(connectionString));
    }

    [Fact]
    public async Task Non_development_startup_does_not_apply_migrations()
    {
        var connectionString = await postgres.CreateEmptyDatabaseAsync();
        await using var factory = new ApiFactory(connectionString, environment: "Production");
        _ = factory.Services;

        Assert.Empty(await MigrationHistorySchemasAsync(connectionString));
    }

    private static async Task<List<string>> MigrationHistorySchemasAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT table_schema FROM information_schema.tables
            WHERE table_name = '__EFMigrationsHistory'
            ORDER BY table_schema
            """,
            connection);

        var schemas = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            schemas.Add(reader.GetString(0));
        }

        return schemas;
    }
}
