using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SchoolClubs.Application.Services.Admin;
using SchoolClubs.Application.Services.Auth;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Gradebook;
using SchoolClubs.Domain.Entities.Users;
using Task = System.Threading.Tasks.Task;

namespace SchoolClubs.Infrastructure.IntegrationTests.Admin;

public class AdminEndpointTests : TestBase
{
    private const string Admin = "/api/v1/admin";

    private HttpClient _admin = null!;
    private User _adminUser = null!;

    [SetUp]
    public async Task LoginAdmin()
    {
        (_admin, _adminUser) = await LoginAsAsync(UserRole.Admin);
    }

    [TearDown]
    public void DisposeAdmin()
    {
        _admin.Dispose();
    }

    [Test]
    public async Task AdminEndpoints_RequireAdminRole()
    {
        var (teacher, _) = await LoginAsAsync(UserRole.Teacher);
        using var _ = teacher;

        (await Client.GetAsync($"{Admin}/groups")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await teacher.GetAsync($"{Admin}/groups")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await _admin.GetAsync($"{Admin}/groups")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task ServiceAdmin_HasAccess()
    {
        (await Client.PostAsJsonAsync($"{Admin}/login", new ServiceAdminLoginRequest(WebApiFactory.ServiceAdminPassword)))
            .EnsureSuccessStatusCode();

        (await Client.GetAsync($"{Admin}/users")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Groups_Lifecycle()
    {
        var teacher = await CreateUserAsync("teacher@school218.ru", [UserRole.Teacher]);
        var parent = await CreateUserAsync("parent@school218.ru", [UserRole.Parent]);

        var created = await (await _admin.PostAsJsonAsync($"{Admin}/groups",
                new CreateGroupRequest("Робототехника", "5–7 классы")))
            .Content.ReadFromJsonAsync<GroupDetailsDto>();
        var url = $"{Admin}/groups/{created!.Id}";

        (await _admin.PostAsync($"{url}/teachers/{teacher.Id}", null)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await _admin.PostAsync($"{url}/teachers/{parent.Id}", null)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var withStudents = await (await _admin.PostAsJsonAsync($"{url}/students", new AddGroupStudentsRequest(null,
            [new NewStudentRequest("Иванов", "Пётр", null, "7б"), new NewStudentRequest("Петрова", "Анна", null, null)])))
            .Content.ReadFromJsonAsync<GroupDetailsDto>();
        withStudents!.Teachers.ShouldHaveSingleItem().Id.ShouldBe(teacher.Id);
        withStudents.Students.Select(s => s.LastName).ShouldBe(["Иванов", "Петрова"]);
        withStudents.Students[0].Grade.ShouldBe("7Б");

        var removed = await (await _admin.DeleteAsync($"{url}/students/{withStudents.Students[0].Id}"))
            .Content.ReadFromJsonAsync<GroupDetailsDto>();
        removed!.Students.ShouldHaveSingleItem().LastName.ShouldBe("Петрова");

        await _admin.PutAsJsonAsync(url, new UpdateGroupRequest("Робототехника 2025", null, true));
        (await _admin.GetFromJsonAsync<List<GroupSummaryDto>>($"{Admin}/groups"))!.ShouldBeEmpty();
        var archived = await _admin.GetFromJsonAsync<List<GroupSummaryDto>>($"{Admin}/groups?includeArchived=true");
        archived.ShouldHaveSingleItem().Title.ShouldBe("Робототехника 2025");
        archived[0].StudentCount.ShouldBe(1);
    }

    [Test]
    public async Task DeleteGroup_WithLessons_Returns409()
    {
        var group = new Group("Шахматы");
        await InScope(async sp =>
        {
            await sp.GetRequiredService<IGroupRepository>().AddAsync(group);
            await sp.GetRequiredService<ILessonRepository>().AddAsync(new Lesson(group, new DateOnly(2026, 10, 1), null));
            await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        });

        (await _admin.DeleteAsync($"{Admin}/groups/{group.Id}")).StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Users_SearchByNameAndEmail()
    {
        var teacher = await CreateUserAsync("teacher@school218.ru", [UserRole.Teacher]);

        var byName = await _admin.GetFromJsonAsync<List<UserSummaryDto>>($"{Admin}/users?q=тест&role=Teacher");
        var byEmail = await _admin.GetFromJsonAsync<List<UserSummaryDto>>($"{Admin}/users?q=Teacher@School218.ru");

        byName.ShouldHaveSingleItem().Id.ShouldBe(teacher.Id);
        byEmail.ShouldHaveSingleItem().Id.ShouldBe(teacher.Id);
    }

    [Test]
    public async Task SetRoles_ReissuesSessionClaims()
    {
        var (teacher, teacherUser) = await LoginAsAsync(UserRole.Teacher);
        using var _ = teacher;
        (await teacher.GetAsync($"{Admin}/groups")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var details = await (await _admin.PutAsJsonAsync($"{Admin}/users/{teacherUser.Id}/roles",
                new SetRolesRequest([UserRole.Teacher, UserRole.Admin])))
            .Content.ReadFromJsonAsync<UserDetailsDto>();

        details!.Roles.ShouldBe([UserRole.Teacher, UserRole.Admin]);
        (await teacher.GetAsync($"{Admin}/groups")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task SetRoles_DomainRules_Return400()
    {
        var teacher = await CreateUserAsync("teacher@school218.ru", [UserRole.Teacher]);
        var group = await (await _admin.PostAsJsonAsync($"{Admin}/groups", new CreateGroupRequest("Шахматы", null)))
            .Content.ReadFromJsonAsync<GroupDetailsDto>();
        await _admin.PostAsync($"{Admin}/groups/{group!.Id}/teachers/{teacher.Id}", null);

        (await _admin.PutAsJsonAsync($"{Admin}/users/{teacher.Id}/roles", new SetRolesRequest([])))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await _admin.PutAsJsonAsync($"{Admin}/users/{_adminUser.Id}/roles", new SetRolesRequest([UserRole.Parent])))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await _admin.PostAsync($"{Admin}/users/{_adminUser.Id}/block", null))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Block_EndsSessions_Unblock_Restores()
    {
        var (parent, parentUser) = await LoginAsAsync(UserRole.Parent);
        using var _ = parent;

        (await _admin.PostAsync($"{Admin}/users/{parentUser.Id}/block", null)).EnsureSuccessStatusCode();
        (await parent.GetAsync("/api/v1/auth/me")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        (await _admin.PostAsync($"{Admin}/users/{parentUser.Id}/unblock", null)).EnsureSuccessStatusCode();
        (await parent.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(parentUser.Email.Address, DefaultPassword)))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task InviteTeacher_ThenResetPassword_AllowsLogin()
    {
        var invited = await (await _admin.PostAsJsonAsync($"{Admin}/teachers/invite",
                new InviteTeacherRequest("New.Teacher@school218.ru", "Сидоров", "Иван", "Петрович")))
            .Content.ReadFromJsonAsync<UserDetailsDto>();
        invited!.Roles.ShouldBe([UserRole.Teacher]);
        invited.LoginProviders.ShouldBeEmpty();

        var temporary = await (await _admin.PostAsync($"{Admin}/users/{invited.Id}/reset-password", null))
            .Content.ReadFromJsonAsync<TemporaryPasswordDto>();
        temporary!.Password.Length.ShouldBe(12);

        (await Client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("new.teacher@school218.ru", temporary.Password)))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Повторный сброс меняет пароль и завершает выданную сессию.
        (await _admin.PostAsync($"{Admin}/users/{invited.Id}/reset-password", null)).EnsureSuccessStatusCode();
        (await Client.GetAsync("/api/v1/auth/me")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task InviteTeacher_ExistingEmail_GrantsRole()
    {
        var parent = await CreateUserAsync("parent@school218.ru", [UserRole.Parent]);

        var details = await (await _admin.PostAsJsonAsync($"{Admin}/teachers/invite",
                new InviteTeacherRequest("parent@school218.ru", "Тестов", "Тест", null)))
            .Content.ReadFromJsonAsync<UserDetailsDto>();

        details!.Id.ShouldBe(parent.Id);
        details.Roles.ShouldBe([UserRole.Parent, UserRole.Teacher]);
    }

    [Test]
    public async Task LinkChild_GrantsParentRole_UnlinkRemoves()
    {
        var teacher = await CreateUserAsync("teacher@school218.ru", [UserRole.Teacher]);
        var student = await (await _admin.PostAsJsonAsync($"{Admin}/students",
                new CreateStudentRequest("Иванов", "Пётр", null, null, null)))
            .Content.ReadFromJsonAsync<StudentDetailsDto>();

        var linked = await (await _admin.PostAsync($"{Admin}/parents/{teacher.Id}/children/{student!.Id}", null))
            .Content.ReadFromJsonAsync<UserDetailsDto>();
        linked!.Roles.ShouldBe([UserRole.Parent, UserRole.Teacher]);
        linked.Children.ShouldHaveSingleItem().Id.ShouldBe(student.Id);

        var unlinked = await (await _admin.DeleteAsync($"{Admin}/parents/{teacher.Id}/children/{student.Id}"))
            .Content.ReadFromJsonAsync<UserDetailsDto>();
        unlinked!.Children.ShouldBeEmpty();
    }

    [Test]
    public async Task Students_CreateSearchUpdateDelete()
    {
        var group = await (await _admin.PostAsJsonAsync($"{Admin}/groups", new CreateGroupRequest("Шахматы", null)))
            .Content.ReadFromJsonAsync<GroupDetailsDto>();
        var created = await (await _admin.PostAsJsonAsync($"{Admin}/students",
                new CreateStudentRequest("Иванов", "Пётр", null, "7Б", group!.Id)))
            .Content.ReadFromJsonAsync<StudentDetailsDto>();
        created!.Groups.ShouldHaveSingleItem().Id.ShouldBe(group.Id);

        var found = await _admin.GetFromJsonAsync<List<StudentListItemDto>>($"{Admin}/students?q=иван");
        found.ShouldHaveSingleItem().Groups.ShouldHaveSingleItem().Title.ShouldBe("Шахматы");
        (await _admin.GetFromJsonAsync<List<StudentListItemDto>>($"{Admin}/students?q=петрова")).ShouldBeEmpty();

        var updated = await (await _admin.PutAsJsonAsync($"{Admin}/students/{created.Id}",
                new UpdateStudentRequest("Иванов", "Павел", "Сергеевич", "8б")))
            .Content.ReadFromJsonAsync<StudentDetailsDto>();
        updated!.FirstName.ShouldBe("Павел");
        updated.Grade.ShouldBe("8Б");

        (await _admin.DeleteAsync($"{Admin}/students/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _admin.GetAsync($"{Admin}/students/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Merge_MovesMarksGroupsAndParents_DeletesDuplicate()
    {
        var group = new Group("Робототехника");
        var original = new Student("Пётр", lastName: "Иванов");
        var duplicate = new Student("Пётр", lastName: "Иванов", grade: "7Б");
        group.AddStudent(duplicate);
        var lesson = new Lesson(group, new DateOnly(2026, 10, 1), null);
        var first = lesson.AddTask();
        var second = lesson.AddTask();
        var gradebook = new LessonGradebook(lesson);
        gradebook.SetEntry(duplicate, first, GradebookEntryTaskStatus.Solved);
        gradebook.SetEntry(duplicate, second, GradebookEntryTaskStatus.Solved);
        gradebook.SetEntry(original, second, GradebookEntryTaskStatus.Unsolved);
        var parent = await CreateUserAsync("parent@school218.ru", [UserRole.Parent]);
        await InScope(async sp =>
        {
            var users = sp.GetRequiredService<IUserRepository>();
            var loaded = await users.GetWithChildrenAsync(parent.Id);
            loaded!.AddChild(duplicate);
            await sp.GetRequiredService<IStudentRepository>().AddAsync(original);
            await sp.GetRequiredService<ILessonGradebookRepository>().AddAsync(gradebook);
            await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        });

        (await _admin.DeleteAsync($"{Admin}/students/{duplicate.Id}")).StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var result = await (await _admin.PostAsync($"{Admin}/students/{duplicate.Id}/merge-into/{original.Id}", null))
            .Content.ReadFromJsonAsync<MergeStudentsResultDto>();

        result.ShouldBe(new MergeStudentsResultDto(original.Id, 1, 1));
        (await _admin.GetAsync($"{Admin}/students/{duplicate.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var merged = await _admin.GetFromJsonAsync<StudentDetailsDto>($"{Admin}/students/{original.Id}");
        merged!.GradebookEntryCount.ShouldBe(2);
        merged.Groups.ShouldHaveSingleItem().Id.ShouldBe(group.Id);
        merged.Parents.ShouldHaveSingleItem().Id.ShouldBe(parent.Id);

        var reloaded = await InScope(sp => sp.GetRequiredService<ILessonGradebookRepository>().GetByLessonIdAsync(lesson.Id));
        reloaded!.FindEntry(original.Id, first.Id)!.Status.ShouldBe(GradebookEntryTaskStatus.Solved);
        reloaded.FindEntry(original.Id, second.Id)!.Status.ShouldBe(GradebookEntryTaskStatus.Unsolved);
    }

    [Test]
    public async Task ParseNames_SuggestsExistingStudents()
    {
        var existing = await (await _admin.PostAsJsonAsync($"{Admin}/students",
                new CreateStudentRequest("Иванов", "Пётр", null, null, null)))
            .Content.ReadFromJsonAsync<StudentDetailsDto>();

        var parsed = await (await _admin.PostAsJsonAsync($"{Admin}/students/parse-names",
                new ParseNamesRequest("иванов пётр\nПетрова Анна 7Б\nкто-то")))
            .Content.ReadFromJsonAsync<List<ParsedNameDto>>();

        parsed!.Count.ShouldBe(3);
        parsed[0].Matches.ShouldHaveSingleItem().Id.ShouldBe(existing!.Id);
        parsed[1].Matches.ShouldBeEmpty();
        parsed[1].Student!.Grade.ShouldBe("7Б");
        parsed[2].Error.ShouldNotBeNull();
    }
}
