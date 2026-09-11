
namespace LinkTracker.Tests.DbTests;

[Collection("db")]
public class MigrationsTest : IntegrationTestBase
{
    private readonly PostgresFixture _fixture;

    public MigrationsTest(PostgresFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetDatabase().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Migrations_ShouldApply()
    {
        // Arrange
        await using var conn = new Npgsql.NpgsqlConnection(_fixture.ConnectionString);
        await conn.OpenAsync();

        var cmd = new Npgsql.NpgsqlCommand("SELECT to_regclass('public.chats')::text", conn);

        // Act
        var result = await cmd.ExecuteScalarAsync();

        // Assert
        Assert.NotNull(result);
    }
}
