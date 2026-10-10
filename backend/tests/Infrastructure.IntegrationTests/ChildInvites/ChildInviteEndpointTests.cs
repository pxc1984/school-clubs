using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SchoolClubs.Application.Services.Auth;
using SchoolClubs.Application.Services.ChildInvites;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Access;
using SchoolClubs.Domain.Entities.Users;
using Task = System.Threading.Tasks.Task;

namespace SchoolClubs.Infrastructure.IntegrationTests.ChildInvites;

public class ChildInviteEndpointTests : TestBase
{
    private HttpClient _admin = null!;
    private Student _petr = null!;

    [SetUp]
    public async Task Seed()
    {
        (_admin, _) = await LoginAsAsync(UserRole.Admin);
        _petr = new Student("Пётр", lastName: "Иванов", grade: "7Б");
        await InScope(async sp =>
        {
            await sp.GetRequiredService<IStudentRepository>().AddAsync(_petr);
            await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        });
    }

    [TearDown]
    public void DisposeClients()
    {
        _admin.Dispose();
    }

    private string Invites
    {
        get { return $"/api/v1/admin/students/{_petr.Id}/invites"; }
    }

    private async Task<ChildInviteDto> CreateInviteAsync()
    {
        var response = await _admin.PostAsync(Invites, null);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ChildInviteDto>())!;
    }

    private static Task<HttpResponseMessage> AcceptAsync(HttpClient client, string token)
    {
        return client.PostAsync($"/api/v1/child-invites/{token}/accept", null);
    }

    private static async Task<IReadOnlyList<Guid>> ChildrenOfAsync(Guid userId)
    {
        return await InScope(async sp =>
        {
            var user = await sp.GetRequiredService<IUserRepository>().GetWithChildrenAsync(userId);
            return (IReadOnlyList<Guid>) user!.Children.Select(c => c.Id).ToList();
        });
    }

    [Test]
    public async Task Admin_CreatesListsAndRevokes()
    {
        var first = await CreateInviteAsync();
        var second = await CreateInviteAsync();

        var active = await _admin.GetFromJsonAsync<List<ChildInviteDto>>(Invites);
        active!.Select(i => i.Id).ShouldBe([second.Id, first.Id]);

        (await _admin.DeleteAsync($"{Invites}/{first.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _admin.GetFromJsonAsync<List<ChildInviteDto>>(Invites))!.ShouldHaveSingleItem().Id.ShouldBe(second.Id);

        // Отозванная ссылка больше не работает.
        var (parent, _) = await LoginAsAsync(UserRole.Parent);
        (await AcceptAsync(parent, first.Token)).StatusCode.ShouldBe(HttpStatusCode.Gone);
    }

    [Test]
    public async Task OnlyAdmin_ManagesInvites()
    {
        var (parent, _) = await LoginAsAsync(UserRole.Parent);

        (await parent.PostAsync(Invites, null)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await _admin.PostAsync($"/api/v1/admin/students/{Guid.NewGuid()}/invites", null))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Accept_LinksChildToExistingParent()
    {
        var invite = await CreateInviteAsync();
        var (parent, user) = await LoginAsAsync(UserRole.Parent);

        var response = await AcceptAsync(parent, invite.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var accepted = (await response.Content.ReadFromJsonAsync<AcceptedChildInviteDto>())!;
        accepted.Student.Id.ShouldBe(_petr.Id);
        accepted.AlreadyLinked.ShouldBeFalse();
        (await ChildrenOfAsync(user.Id)).ShouldBe([_petr.Id]);
        (await _admin.GetFromJsonAsync<List<ChildInviteDto>>(Invites))!.ShouldBeEmpty();

        // Повторный переход тем же родителем — не ошибка.
        var again = await AcceptAsync(parent, invite.Token);
        again.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await again.Content.ReadFromJsonAsync<AcceptedChildInviteDto>())!.AlreadyLinked.ShouldBeTrue();
    }

    [Test]
    public async Task Accept_IsOneTime()
    {
        var invite = await CreateInviteAsync();
        var (first, _) = await LoginAsAsync(UserRole.Parent);
        var (second, secondUser) = await LoginAsAsync(UserRole.Parent);

        (await AcceptAsync(first, invite.Token)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await AcceptAsync(second, invite.Token)).StatusCode.ShouldBe(HttpStatusCode.Gone);
        (await ChildrenOfAsync(secondUser.Id)).ShouldBeEmpty();
    }

    [Test]
    public async Task Accept_GrantsParentRole_AndRefreshesSession()
    {
        var invite = await CreateInviteAsync();
        var (teacher, _) = await LoginAsAsync(UserRole.Teacher);

        (await AcceptAsync(teacher, invite.Token)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var me = await teacher.GetFromJsonAsync<MeDto>("/api/v1/auth/me");
        me!.Roles.ShouldContain(UserRole.Parent);
        (await teacher.GetAsync("/api/v1/parent/children")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Accept_RequiresLoginAndValidToken()
    {
        var invite = await CreateInviteAsync();
        using var anonymous = CreateClient();
        var (parent, _) = await LoginAsAsync(UserRole.Parent);

        (await AcceptAsync(anonymous, invite.Token)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await AcceptAsync(parent, "unknown")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await AcceptAsync(parent, new string('a', 43))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Register_ThenAccept_LinksChildToNewAccount()
    {
        var invite = await CreateInviteAsync();
        using var client = CreateClient();
        (await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest("maria@example.com",
            DefaultPassword, "Мария", "Иванова", null, null, true))).EnsureSuccessStatusCode();

        (await AcceptAsync(client, invite.Token)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var children = await client.GetStringAsync("/api/v1/parent/children");
        children.ShouldContain(_petr.Id.ToString());
    }

    [Test]
    public async Task Accept_IsRateLimitedPerIp()
    {
        // Отдельный хост: у общей фабрики лимит поднят, а счётчик лимитера живёт в памяти приложения.
        await using var factory = FunctionalTestSetup.Factory.WithWebHostBuilder(builder =>
            builder.UseSetting("Auth:ChildInviteAcceptsPerMinute", "2"));
        var user = await CreateUserAsync($"{Guid.NewGuid():N}@school218.ru", [UserRole.Parent]);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "fetch");
        (await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(user.Email.Address, DefaultPassword)))
            .EnsureSuccessStatusCode();
        var invite = await CreateInviteAsync();

        // Попытки с неверным токеном тоже считаются — иначе лимит не мешал бы перебору.
        (await AcceptAsync(client, new string('a', ChildInvite.TokenLength))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await AcceptAsync(client, new string('b', ChildInvite.TokenLength))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await AcceptAsync(client, invite.Token)).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        (await ChildrenOfAsync(user.Id)).ShouldBeEmpty();
    }
}
