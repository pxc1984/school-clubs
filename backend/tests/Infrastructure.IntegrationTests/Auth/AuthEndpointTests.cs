using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using SchoolClubs.Application.Services.Account;
using SchoolClubs.Application.Services.Auth;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Users;
using SchoolClubs.Web.Infrastructure.Auth;

namespace SchoolClubs.Infrastructure.IntegrationTests.Auth;

public class AuthEndpointTests : TestBase
{
    private const string Auth = "/api/v1/auth";

    private static RegisterRequest NewRegistration(string email = "parent@school218.ru", bool consent = true)
    {
        return new RegisterRequest(email, DefaultPassword, "Анна", "Иванова", null, null, consent);
    }

    [Test]
    public async Task Register_SignsInAsParentWithConsent()
    {
        var response = await Client.PostAsJsonAsync($"{Auth}/register", NewRegistration());
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var me = await Client.GetFromJsonAsync<MeDto>($"{Auth}/me");

        me.ShouldNotBeNull();
        me.Roles.ShouldBe([UserRole.Parent]);
        me.ConsentRequired.ShouldBeFalse();
        me.Email.ShouldBe("parent@school218.ru");

        var user = await InScope(sp => sp.GetRequiredService<IUserRepository>().FindByEmailAsync("parent@school218.ru"));
        user!.ConsentVersion.ShouldBe(WebApiFactory.ConsentVersion);
    }

    [Test]
    public async Task Register_WithoutConsent_Returns400()
    {
        var response = await Client.PostAsJsonAsync($"{Auth}/register", NewRegistration(consent: false));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("AcceptPersonalDataPolicy");
    }

    [Test]
    public async Task Register_ExistingEmail_Returns409()
    {
        await CreateUserAsync("teacher@school218.ru", [UserRole.Teacher]);

        var response = await Client.PostAsJsonAsync($"{Auth}/register", NewRegistration(" Teacher@School218.ru "));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Login_WrongPassword_Returns401()
    {
        await CreateUserAsync("parent@school218.ru", [UserRole.Parent]);

        var wrong = await Client.PostAsJsonAsync($"{Auth}/login", new LoginRequest("parent@school218.ru", "nope-nope"));
        var unknown = await Client.PostAsJsonAsync($"{Auth}/login", new LoginRequest("nobody@school218.ru", "nope-nope"));

        wrong.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        unknown.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Login_ThenLogout_EndsSession()
    {
        var (client, user) = await LoginAsAsync(UserRole.Teacher, UserRole.Admin);
        using var _ = client;

        var me = await client.GetFromJsonAsync<MeDto>($"{Auth}/me");
        me!.Id.ShouldBe(user.Id);
        me.Roles.ShouldBe([UserRole.Teacher, UserRole.Admin]);

        (await client.PostAsync($"{Auth}/logout", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.GetAsync($"{Auth}/me")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Me_Anonymous_Returns401()
    {
        (await Client.GetAsync($"{Auth}/me")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Login_Blocked_Returns403WithCode()
    {
        var user = await CreateUserAsync("parent@school218.ru", [UserRole.Parent]);
        await BlockAsync(user.Id);

        var response = await Client.PostAsJsonAsync($"{Auth}/login", new LoginRequest("parent@school218.ru", DefaultPassword));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await ReadCodeAsync(response)).ShouldBe("blocked");
    }

    [Test]
    public async Task Session_OfBlockedUser_IsRevoked()
    {
        var (client, user) = await LoginAsAsync(UserRole.Parent);
        using var _ = client;

        await BlockAsync(user.Id);

        (await client.GetAsync($"{Auth}/me")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Mutation_WithoutCsrfHeader_Returns403()
    {
        using var client = FunctionalTestSetup.Factory.CreateClient();

        var response = await client.PostAsJsonAsync($"{Auth}/register", NewRegistration());

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await ReadCodeAsync(response)).ShouldBe("csrf_header_required");
    }

    [Test]
    public async Task Consent_IsRequiredUntilAccepted()
    {
        var user = await CreateUserAsync("parent@school218.ru", [UserRole.Parent], consentAccepted: false);
        (await Client.PostAsJsonAsync($"{Auth}/login", new LoginRequest(user.Email.Address, DefaultPassword)))
            .EnsureSuccessStatusCode();

        (await Client.GetFromJsonAsync<MeDto>($"{Auth}/me"))!.ConsentRequired.ShouldBeTrue();
        var blocked = await Client.GetAsync("/api/v1/account");
        blocked.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await ReadCodeAsync(blocked)).ShouldBe("consent_required");

        (await Client.PostAsJsonAsync("/api/v1/account/consent", new AcceptConsentRequest(false)))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await Client.PostAsJsonAsync("/api/v1/account/consent", new AcceptConsentRequest(true)))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var account = await Client.GetFromJsonAsync<AccountDto>("/api/v1/account");
        account!.LoginProviders.ShouldBe([UserLogin.PasswordProvider]);
    }

    [Test]
    public async Task ServiceAdmin_Login()
    {
        (await Client.PostAsJsonAsync("/api/v1/admin/login", new ServiceAdminLoginRequest("wrong")))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        (await Client.PostAsJsonAsync("/api/v1/admin/login",
                new ServiceAdminLoginRequest(WebApiFactory.ServiceAdminPassword)))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var me = await Client.GetFromJsonAsync<MeDto>($"{Auth}/me");
        me!.IsServiceAdmin.ShouldBeTrue();
        me.Id.ShouldBeNull();
        me.Roles.ShouldBe([UserRole.Admin]);
        // У сервисного админа нет аккаунта.
        (await Client.GetAsync("/api/v1/account")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task External_UnknownProvider_Returns404_OnlyConfiguredProvidersListed()
    {
        (await Client.GetAsync($"{Auth}/external/nope")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var providers = await Client.GetFromJsonAsync<List<JsonElement>>($"{Auth}/providers");
        var yandex = providers.ShouldHaveSingleItem();
        yandex.GetProperty("name").GetString().ShouldBe("yandex");
        yandex.GetProperty("displayName").GetString().ShouldBe("Яндекс ID");
    }

    [Test]
    public async Task External_Yandex_RedirectsToOAuthWithPkce()
    {
        using var client = FunctionalTestSetup.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync($"{Auth}/external/yandex");

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var location = response.Headers.Location!;
        location.GetLeftPart(UriPartial.Path).ShouldBe("https://oauth.yandex.ru/authorize");
        var query = QueryHelpers.ParseQuery(location.Query);
        query["client_id"].ToString().ShouldBe("test-client");
        query["response_type"].ToString().ShouldBe("code");
        query["scope"].ToString().ShouldBe("login:email login:info login:avatar");
        query["redirect_uri"].ToString().ShouldBe("http://localhost/api/v1/auth/external/yandex/callback");
        query["code_challenge_method"].ToString().ShouldBe("S256");
        // Telegram отвергает длинный state: свойства лежат на сервере, в state — одноразовый ключ.
        var state = query["state"].ToString();
        state.Length.ShouldBeLessThanOrEqualTo(32);
        var stateFormat = FunctionalTestSetup.Factory.Services.GetRequiredService<ServerSideStateDataFormat>();
        stateFormat.Unprotect(state).ShouldNotBeNull();
        stateFormat.Unprotect(state).ShouldBeNull();
    }

    private static async Task BlockAsync(Guid userId)
    {
        await InScope(async sp =>
        {
            var user = await sp.GetRequiredService<IUserRepository>().FindByIdAsync(userId);
            user!.Block();
            await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        });
    }

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    [Test]
    public async Task ChangeUsername_SetsNormalized_RejectsTakenAndInvalid_Clears()
    {
        var (other, _) = await LoginAsAsync(UserRole.Parent);
        (await other.PutAsJsonAsync("/api/v1/account/username", new ChangeUsernameRequest("taken")))
            .EnsureSuccessStatusCode();
        var (client, _) = await LoginAsAsync(UserRole.Parent);

        var response = await client.PutAsJsonAsync("/api/v1/account/username", new ChangeUsernameRequest("@Anna"));
        response.EnsureSuccessStatusCode();
        (await response.Content.ReadFromJsonAsync<AccountDto>())!.Username.ShouldBe("anna");
        (await client.PutAsJsonAsync("/api/v1/account/username", new ChangeUsernameRequest("anna")))
            .EnsureSuccessStatusCode();

        (await client.PutAsJsonAsync("/api/v1/account/username", new ChangeUsernameRequest("TAKEN")))
            .StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await client.PutAsJsonAsync("/api/v1/account/username", new ChangeUsernameRequest("a b")))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        (await client.PutAsJsonAsync("/api/v1/account/username", new ChangeUsernameRequest(null)))
            .EnsureSuccessStatusCode();
        (await client.GetFromJsonAsync<AccountDto>("/api/v1/account"))!.Username.ShouldBeNull();
    }
}
