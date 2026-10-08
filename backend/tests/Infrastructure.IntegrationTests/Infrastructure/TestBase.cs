using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SchoolClubs.Application.Common.Interfaces;
using SchoolClubs.Application.Services.Auth;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Infrastructure.IntegrationTests.Infrastructure;

public abstract class TestBase
{
    protected const string DefaultPassword = "correct-horse-battery";

    protected HttpClient Client { get; private set; } = null!;

    [SetUp]
    public async Task SetUp()
    {
        if (FunctionalTestSetup.DbResetter is not null) await FunctionalTestSetup.DbResetter.ResetAsync();
        Client = CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        Client.Dispose();
    }

    /// <summary>Клиент со своей cookie-сессией и CSRF-заголовком, как у axios на фронте.</summary>
    protected static HttpClient CreateClient()
    {
        var client = FunctionalTestSetup.Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "fetch");
        return client;
    }

    /// <summary>Заводит пользователя с паролем <see cref="DefaultPassword"/> прямо в БД.</summary>
    protected static async Task<User> CreateUserAsync(string email,
        UserRole[] roles,
        bool consentAccepted = true)
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        var services = scope.ServiceProvider;

        var user = new User("Тест", lastName: "Тестов", email: email, roles: roles);
        if (consentAccepted)
            user.AcceptConsent(WebApiFactory.ConsentVersion, DateTimeOffset.UtcNow);
        var hash = services.GetRequiredService<IPasswordHasher>().Hash(DefaultPassword);

        await services.GetRequiredService<IUserRepository>().AddAsync(user);
        await services.GetRequiredService<IUserLoginRepository>().AddAsync(UserLogin.ForPassword(user, email, hash));
        await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        return user;
    }

    /// <summary>Войти с ролями …: новый пользователь и клиент с его сессией.</summary>
    protected static async Task<(HttpClient Client, User User)> LoginAsAsync(params UserRole[] roles)
    {
        var user = await CreateUserAsync($"{Guid.NewGuid():N}@school218.ru", roles);
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(user.Email.Address, DefaultPassword));
        response.EnsureSuccessStatusCode();
        return (client, user);
    }

    protected static async Task<T> InScope<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        return await action(scope.ServiceProvider);
    }

    protected static async Task InScope(Func<IServiceProvider, Task> action)
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        await action(scope.ServiceProvider);
    }
}
