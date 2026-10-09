using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SchoolClubs.Application.Common.Interfaces;

namespace SchoolClubs.Infrastructure.IntegrationTests.Infrastructure;

public class WebApiFactory(string connectionString, string s3Url) : WebApplicationFactory<Program>
{
    public const string ServiceAdminPassword = "service-admin-secret";
    public const string ConsentVersion = "test-1";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:SchoolClubsDb", connectionString);
        builder.UseSetting("Storage:S3:ServiceUrl", s3Url);
        builder.UseSetting("Storage:S3:AccessKey", FunctionalTestSetup.S3AccessKey);
        builder.UseSetting("Storage:S3:SecretKey", FunctionalTestSetup.S3SecretKey);
        builder.UseSetting("Storage:S3:Bucket", "schoolclubs-test");
        builder.UseSetting("Auth:ServiceAdminPassword", ServiceAdminPassword);
        builder.UseSetting("Auth:ConsentVersion", ConsentVersion);
        // Все тесты ходят с одного «IP»; лимит сервисного входа проверять здесь не нужно.
        builder.UseSetting("Auth:ServiceAdminLoginAttemptsPerMinute", "1000");
        // Сверять сессию с БД на каждом запросе, чтобы блокировка была видна сразу.
        builder.UseSetting("Auth:SessionValidationInterval", "00:00:00");
        // Яндекс ID (OAuth 2.0 из appsettings.json) включён фиктивным клиентом: проверяем редирект, без сети.
        builder.UseSetting("Auth:Oidc:yandex:ClientId", "test-client");
        builder.UseSetting("Auth:Oidc:yandex:ClientSecret", "test-secret");
        builder.ConfigureTestServices(services =>
            services.AddSingleton<IRemoteFileDownloader, FakeRemoteFileDownloader>());
    }
}
