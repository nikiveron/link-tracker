using Testcontainers.PostgreSql;

namespace LinkTracker.Tests.DbTests;

public class PostgresFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; }

    public string ConnectionString => Container.GetConnectionString();

    public PostgresFixture()
    {
        Container = new PostgreSqlBuilder("postgres:15")
            .WithDatabase("chats_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
        await ApplyMigrations();
    }

    public async Task DisposeAsync()
    {
        await Container.DisposeAsync();
    }

    private async Task ApplyMigrations()
    {
        await using var conn = new Npgsql.NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        var sql = await File.ReadAllTextAsync("migrations/001_init.sql");

        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ResetDatabase()
    {
        await using var conn = new Npgsql.NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        var sql = @"TRUNCATE TABLE link_tag, tags, links, chats RESTART IDENTITY CASCADE;";

        await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }
}