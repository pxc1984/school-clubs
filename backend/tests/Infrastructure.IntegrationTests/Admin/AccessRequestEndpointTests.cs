using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SchoolClubs.Application.Services.AccessRequests;
using SchoolClubs.Application.Services.Admin;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Access;
using SchoolClubs.Domain.Entities.Gradebook;
using SchoolClubs.Domain.Entities.Users;
using Task = System.Threading.Tasks.Task;

namespace SchoolClubs.Infrastructure.IntegrationTests.Admin;

public class AccessRequestEndpointTests : TestBase
{
    private const string Parent = "/api/v1/parent";
    private const string Admin = "/api/v1/admin/access-requests";

    private HttpClient _parent = null!;
    private User _parentUser = null!;
    private HttpClient _admin = null!;
    private Group _group = null!;
    private Group _archived = null!;
    private Student _petr = null!;

    [SetUp]
    public async Task Seed()
    {
        (_parent, _parentUser) = await LoginAsAsync(UserRole.Parent);
        (_admin, _) = await LoginAsAsync(UserRole.Admin);
        _group = new Group("Робототехника", "5–7 классы");
        _archived = new Group("Шахматы 2025");
        _archived.Archive();
        _petr = new Student("Пётр", lastName: "Иванов", grade: "7Б");
        _group.AddStudent(_petr);
        _group.AddStudent(new Student("Анна", lastName: "Иванова"));
        await InScope(async sp =>
        {
            await sp.GetRequiredService<IGroupRepository>().AddAsync(_group);
            await sp.GetRequiredService<IGroupRepository>().AddAsync(_archived);
            await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        });
    }

    [TearDown]
    public void DisposeClients()
    {
        _parent.Dispose();
        _admin.Dispose();
    }

    private static CreateAccessRequestRequest Request(Guid groupId, string lastName = "Иванов",
        string firstName = "Пётр", bool consent = true)
    {
        return new CreateAccessRequestRequest(groupId, lastName, firstName, null, "7б", ParentRelation.Mother,
            "Ходит по вторникам", consent);
    }

    private async Task<AccessRequestDto> CreateAsync(CreateAccessRequestRequest request)
    {
        var response = await _parent.PostAsJsonAsync($"{Parent}/access-requests", request);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<AccessRequestDto>())!;
    }

    [Test]
    public async Task Parent_SeesOnlyActiveGroupsWithoutMembers()
    {
        var raw = await _parent.GetStringAsync($"{Parent}/groups");
        var groups = await _parent.GetFromJsonAsync<List<ParentGroupDto>>($"{Parent}/groups");

        groups.ShouldHaveSingleItem().ShouldBe(new ParentGroupDto(_group.Id, "Робототехника", "5–7 классы"));
        raw.ShouldNotContain("Иванов");
    }

    [Test]
    public async Task Create_Rules()
    {
        (await _parent.PostAsJsonAsync($"{Parent}/access-requests", Request(_group.Id, consent: false)))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await _parent.PostAsJsonAsync($"{Parent}/access-requests", Request(_archived.Id)))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var created = await CreateAsync(Request(_group.Id));
        created.Status.ShouldBe(AccessRequestStatus.Pending);
        created.Grade.ShouldBe("7Б");
        created.Group.Title.ShouldBe("Робототехника");

        // Тот же ребёнок в той же группе, пока запрос ждёт, — 400.
        (await _parent.PostAsJsonAsync($"{Parent}/access-requests", Request(_group.Id, "иванов", "петр")))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        for (var i = 0; i < AccessRequestService.MaxPendingPerParent - 1; i++)
            await CreateAsync(Request(_group.Id, "Ребёнок", $"Имя{i}"));
        (await _parent.PostAsJsonAsync($"{Parent}/access-requests", Request(_group.Id, "Ещё", "Один")))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Cancel_OwnPendingOnly()
    {
        var created = await CreateAsync(Request(_group.Id));
        var (stranger, _) = await LoginAsAsync(UserRole.Parent);
        using var _ = stranger;

        (await stranger.DeleteAsync($"{Parent}/access-requests/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var cancelled = await (await _parent.DeleteAsync($"{Parent}/access-requests/{created.Id}"))
            .Content.ReadFromJsonAsync<AccessRequestDto>();
        cancelled!.Status.ShouldBe(AccessRequestStatus.Cancelled);
        (await _parent.DeleteAsync($"{Parent}/access-requests/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.Conflict);

        // После отзыва можно подать заново.
        await CreateAsync(Request(_group.Id));
    }

    [Test]
    public async Task Admin_Queue_SuggestsAndApprovesExistingStudent()
    {
        var first = await CreateAsync(Request(_group.Id));
        await CreateAsync(Request(_group.Id, "Петров", "Пётр"));

        (await _admin.GetFromJsonAsync<PendingCountDto>($"{Admin}/pending-count"))!.Count.ShouldBe(2);
        var queue = (await _admin.GetFromJsonAsync<List<AdminAccessRequestDto>>($"{Admin}?status=Pending"))!;
        queue.Select(r => r.Id).First().ShouldBe(first.Id);
        var card = queue[0];
        card.Parent.Id.ShouldBe(_parentUser.Id);
        card.Parent.Email.ShouldBe(_parentUser.Email.Address);
        card.Suggestions.ShouldHaveSingleItem().Id.ShouldBe(_petr.Id);

        var approved = await (await _admin.PostAsJsonAsync($"{Admin}/{first.Id}/approve",
                new ApproveAccessRequestRequest(_petr.Id, null)))
            .Content.ReadFromJsonAsync<AdminAccessRequestDto>();
        approved!.Status.ShouldBe(AccessRequestStatus.Approved);
        approved.Student!.Id.ShouldBe(_petr.Id);
        approved.Parent.Children.ShouldHaveSingleItem().Id.ShouldBe(_petr.Id);

        // Повторная обработка — конфликт.
        (await _admin.PostAsJsonAsync($"{Admin}/{first.Id}/reject", new RejectAccessRequestRequest("передумали")))
            .StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await _admin.GetFromJsonAsync<PendingCountDto>($"{Admin}/pending-count"))!.Count.ShouldBe(1);
    }

    [Test]
    public async Task Admin_ApprovesWithNewStudent_AddsToGroup_GrantsParentRole()
    {
        var teacher = await CreateUserAsync("teacher@school218.ru", [UserRole.Teacher]);
        using var teacherClient = CreateClient();
        await teacherClient.PostAsJsonAsync("/api/v1/auth/login",
            new Application.Services.Auth.LoginRequest(teacher.Email.Address, DefaultPassword));
        // Учитель без роли Parent подать запрос не может — запрос от его имени заводим напрямую.
        var request = new AccessRequest(teacher, _group.Id, "Сидорова", "Вера", null, null, ParentRelation.Mother,
            null, DateTimeOffset.UtcNow);
        await InScope(async sp =>
        {
            await sp.GetRequiredService<IAccessRequestRepository>().AddAsync(request);
            await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        });

        (await _admin.PostAsJsonAsync($"{Admin}/{request.Id}/approve", new ApproveAccessRequestRequest(null, null)))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var approved = await (await _admin.PostAsJsonAsync($"{Admin}/{request.Id}/approve",
                new ApproveAccessRequestRequest(null, new NewStudentRequest("Сидорова", "Вера", null, "5а"))))
            .Content.ReadFromJsonAsync<AdminAccessRequestDto>();

        approved!.Student!.Grade.ShouldBe("5А");
        var group = await _admin.GetFromJsonAsync<GroupDetailsDto>($"/api/v1/admin/groups/{_group.Id}");
        group!.Students.ShouldContain(s => s.Id == approved.Student!.Id);
        var user = await _admin.GetFromJsonAsync<UserDetailsDto>($"/api/v1/admin/users/{teacher.Id}");
        user!.Roles.ShouldBe([UserRole.Parent, UserRole.Teacher]);
        // Сессия учителя перевыпущена с ролью Parent.
        (await teacherClient.GetAsync($"{Parent}/access-requests")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Admin_Rejects_ParentSeesReason()
    {
        var created = await CreateAsync(Request(_group.Id));

        (await _admin.PostAsJsonAsync($"{Admin}/{created.Id}/reject", new RejectAccessRequestRequest(" ")))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await _admin.PostAsJsonAsync($"{Admin}/{created.Id}/reject",
            new RejectAccessRequestRequest("Ребёнок не записан в группу"))).EnsureSuccessStatusCode();

        var mine = await _parent.GetFromJsonAsync<List<AccessRequestDto>>($"{Parent}/access-requests");
        var rejected = mine.ShouldHaveSingleItem();
        rejected.Status.ShouldBe(AccessRequestStatus.Rejected);
        rejected.RejectReason.ShouldBe("Ребёнок не записан в группу");
        rejected.DecidedAt.ShouldNotBeNull();
    }

    [Test]
    public async Task ParentEndpoints_RequireParentRole_AdminQueueRequiresAdmin()
    {
        (await _admin.GetAsync($"{Parent}/groups")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await _parent.GetAsync(Admin)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
