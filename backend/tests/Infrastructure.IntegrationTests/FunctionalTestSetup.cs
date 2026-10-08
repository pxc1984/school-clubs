using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace SchoolClubs.Infrastructure.IntegrationTests;

[SetUpFixture]
public class FunctionalTestSetup
{
    internal static IServiceScopeFactory ScopeFactory { get; private set; } = null!;
    internal static WebApiFactory Factory { get; private set; } = null!;
    internal static DatabaseResetter? DbResetter { get; private set; }

    private static PostgreSqlContainer? _container;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _container = new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("schoolclubs_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        // создание клиента стартует хост, а с ним и ApplicationDbContextInitialiser (создание схемы)
        Factory = new WebApiFactory(_container.GetConnectionString());
        Factory.CreateClient().Dispose();
        ScopeFactory = Factory.Services.GetRequiredService<IServiceScopeFactory>();

        DbResetter = await DatabaseResetter.CreateAsync(_container.GetConnectionString());
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (DbResetter is not null) await DbResetter.DisposeAsync();
        await Factory.DisposeAsync();
        if (_container is not null) await _container.DisposeAsync();
    }
}
