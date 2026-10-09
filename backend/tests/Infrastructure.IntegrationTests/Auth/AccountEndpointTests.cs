using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SchoolClubs.Application.Services.Account;
using SchoolClubs.Application.Services.Auth;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Infrastructure.IntegrationTests.Auth;

public class AccountEndpointTests : TestBase
{
    private const string Account = "/api/v1/account";

    [Test]
    public async Task Update_ChangesNameAndPhone_RejectsInvalid()
    {
        var (client, _) = await LoginAsAsync(UserRole.Parent);

        var response = await client.PutAsJsonAsync(Account,
            new UpdateAccountRequest(" Мария ", "Петрова", "Ивановна", "+7 900 123-45-67"));
        response.EnsureSuccessStatusCode();
        var account = await response.Content.ReadFromJsonAsync<AccountDto>();
        account!.FirstName.ShouldBe("Мария");
        account.MiddleName.ShouldBe("Ивановна");
        account.Phone.ShouldBe("+7 900 123-45-67");

        (await client.PutAsJsonAsync(Account, new UpdateAccountRequest("Мария Анна", "Петрова", null, null)))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.PutAsJsonAsync(Account, new UpdateAccountRequest("Мария", "Петрова", null, "позвоните")))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ChangePassword_RequiresCurrent_KeepsCurrentSession_EndsOthers()
    {
        var (client, user) = await LoginAsAsync(UserRole.Parent);
        var other = CreateClient();
        (await other.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(user.Email.Address, DefaultPassword)))
            .EnsureSuccessStatusCode();

        var wrong = await client.PostAsJsonAsync($"{Account}/password",
            new ChangePasswordRequest("wrong-password", "new-password-1"));
        wrong.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await wrong.Content.ReadAsStringAsync()).ShouldContain("CurrentPassword");

        (await client.PostAsJsonAsync($"{Account}/password",
                new ChangePasswordRequest(DefaultPassword, "new-password-1")))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await client.GetAsync(Account)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await other.GetAsync(Account)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await CreateClient().PostAsJsonAsync("/api/v1/auth/login",
                new LoginRequest(user.Email.Address, "new-password-1")))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task ChangePassword_WithoutPasswordLogin_SetsFirstPassword()
    {
        var (client, user) = await LoginAsAsync(UserRole.Parent);
        await RemovePasswordLoginAsync(user);

        (await client.PostAsJsonAsync($"{Account}/password", new ChangePasswordRequest(null, "first-password")))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await client.GetFromJsonAsync<AccountDto>(Account))!.LoginProviders.ShouldBe([UserLogin.PasswordProvider]);
        (await CreateClient().PostAsJsonAsync("/api/v1/auth/login",
                new LoginRequest(user.Email.Address, "first-password")))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task UnlinkLogin_RemovesProvider_ButNotTheLastLogin()
    {
        var (client, user) = await LoginAsAsync(UserRole.Parent);
        await AddExternalLoginAsync(user, "yandex");

        (await client.DeleteAsync($"{Account}/logins/yandex")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.DeleteAsync($"{Account}/logins/yandex")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.DeleteAsync($"{Account}/logins/password")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        await AddExternalLoginAsync(user, "telegram");
        await RemovePasswordLoginAsync(user);
        (await client.DeleteAsync($"{Account}/logins/telegram")).StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    private static Task AddExternalLoginAsync(User user, string provider)
    {
        return InScope(async sp =>
        {
            await sp.GetRequiredService<IUserLoginRepository>()
                .AddAsync(UserLogin.ForExternal(user, provider, $"{provider}-{user.Id:N}"));
            await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        });
    }

    private static Task RemovePasswordLoginAsync(User user)
    {
        return InScope(async sp =>
        {
            var logins = sp.GetRequiredService<IUserLoginRepository>();
            var login = (await logins.ListByUserAsync(user.Id)).Single(l => l.Provider == UserLogin.PasswordProvider);
            logins.Remove(login);
            await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        });
    }
}
