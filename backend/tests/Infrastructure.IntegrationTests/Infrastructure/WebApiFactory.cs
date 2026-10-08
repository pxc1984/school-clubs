using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SchoolClubs.Infrastructure.IntegrationTests.Infrastructure;

public class WebApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    public const string ServiceAdminPassword = "service-admin-secret";
    public const string ConsentVersion = "test-1";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:SchoolClubsDb", connectionString);
        builder.UseSetting("Auth:ServiceAdminPassword", ServiceAdminPassword);
        builder.UseSetting("Auth:ConsentVersion", ConsentVersion);
        // Все тесты ходят с одного «IP»; лимит сервисного входа проверять здесь не нужно.
        builder.UseSetting("Auth:ServiceAdminLoginAttemptsPerMinute", "1000");
        // Сверять сессию с БД на каждом запросе, чтобы блокировка была видна сразу.
        builder.UseSetting("Auth:SessionValidationInterval", "00:00:00");
    }
}
