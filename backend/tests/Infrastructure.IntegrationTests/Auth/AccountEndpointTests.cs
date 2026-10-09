using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using SchoolClubs.Application.Services.Account;
using SchoolClubs.Application.Services.Auth;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Users;
using SchoolClubs.Web.Infrastructure.Auth;

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
    public async Task Update_SetsAndClearsGender()
    {
        var (client, _) = await LoginAsAsync(UserRole.Parent);

        var set = await client.PutAsJsonAsync(Account,
            new UpdateAccountRequest("Мария", "Петрова", null, null, Gender.Female));
        (await set.Content.ReadFromJsonAsync<AccountDto>())!.Gender.ShouldBe(Gender.Female);
        (await client.GetFromJsonAsync<AccountDto>(Account))!.Gender.ShouldBe(Gender.Female);

        var cleared = await client.PutAsJsonAsync(Account, new UpdateAccountRequest("Мария", "Петрова", null, null));
        (await cleared.Content.ReadFromJsonAsync<AccountDto>())!.Gender.ShouldBeNull();
    }

    [Test]
    public async Task Update_UnreadableBody_IsBadRequestProblem()
    {
        var (client, _) = await LoginAsAsync(UserRole.Parent);

        var unknownEnum = await client.PutAsJsonAsync(Account,
            new { firstName = "Мария", lastName = "Петрова", gender = "Unknown" });
        var malformed = await client.PutAsync(Account,
            new StringContent("{\"firstName\":", System.Text.Encoding.UTF8, "application/json"));

        foreach (var response in new[] { unknownEnum, malformed })
        {
            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
            problem!.Status.ShouldBe(400);
            problem.Title.ShouldBe("Bad request");
        }
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

    [Test]
    public async Task LinkLogin_ChallengesProviderOnBehalfOfCurrentUser()
    {
        var (_, user) = await LoginAsAsync(UserRole.Parent);
        using var client = FunctionalTestSetup.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Add("X-Requested-With", "fetch");
        (await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(user.Email.Address, DefaultPassword)))
            .EnsureSuccessStatusCode();

        var response = await client.GetAsync($"{Account}/logins/yandex/link");

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var location = response.Headers.Location!;
        location.GetLeftPart(UriPartial.Path).ShouldBe("https://oauth.yandex.ru/authorize");
        var state = QueryHelpers.ParseQuery(location.Query)["state"].ToString();
        var properties = FunctionalTestSetup.Factory.Services.GetRequiredService<ServerSideStateDataFormat>()
            .Unprotect(state);
        properties!.Items[AuthSchemes.LinkUserItem].ShouldBe(user.Id.ToString());

        (await client.GetAsync($"{Account}/logins/nope/link")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await CreateClient().GetAsync($"{Account}/logins/yandex/link")).StatusCode
            .ShouldBe(HttpStatusCode.Unauthorized);
    }
}
