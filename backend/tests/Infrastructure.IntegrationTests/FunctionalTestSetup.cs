using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
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
    private static IContainer? _s3;

    internal const string S3AccessKey = "testadmin";
    internal const string S3SecretKey = "testsecret";
    private const int S3Port = 9000;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _container = new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("schoolclubs_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        // S3-совместимое хранилище для файлов журналов (как в docker-compose).
        _s3 = new ContainerBuilder("rustfs/rustfs:1.0.1")
            .WithEnvironment("RUSTFS_ACCESS_KEY", S3AccessKey)
            .WithEnvironment("RUSTFS_SECRET_KEY", S3SecretKey)
            .WithPortBinding(S3Port, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(request => request.ForPort(S3Port).ForPath("/health")))
            .WithCleanUp(true)
            .Build();

        await Task.WhenAll(_container.StartAsync(), _s3.StartAsync());

        // создание клиента стартует хост, а с ним и ApplicationDbContextInitialiser (создание схемы и бакета)
        var s3Url = $"http://{_s3.Hostname}:{_s3.GetMappedPublicPort(S3Port)}";
        Factory = new WebApiFactory(_container.GetConnectionString(), s3Url);
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
        if (_s3 is not null) await _s3.DisposeAsync();
    }
}
