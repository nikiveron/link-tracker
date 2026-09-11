using LinkTracker.Scrapper.Application.Interfaces;
using LinkTracker.Scrapper.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace LinkTracker.Tests.DbTests;

[Collection("db")]
public class AccessTypeTest : IntegrationTestBase
{
    private readonly PostgresFixture _fixture;

    public AccessTypeTest(PostgresFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetDatabase().GetAwaiter().GetResult();
    }

    public static ILinkRepository Create(string type, string conn)
    {
        return type switch
        {
            "sql" => new SqlLinkRepository(conn),
            "orm" => new OrmLinkRepository(
                new ChatsContext(
                    new DbContextOptionsBuilder<ChatsContext>()
                        .UseNpgsql(conn)
                        .Options)),
            _ => throw new ArgumentException("")
        };
    }

    [Fact]
    public void ShouldSwitchRepositoryImplementation()
    {
        // Arrange & Act
        var sqlRepo = Create("sql", _fixture.ConnectionString);
        var ormRepo = Create("orm", _fixture.ConnectionString);

        // Assert
        Assert.IsType<SqlLinkRepository>(sqlRepo);
        Assert.IsType<OrmLinkRepository>(ormRepo);
    }
}
