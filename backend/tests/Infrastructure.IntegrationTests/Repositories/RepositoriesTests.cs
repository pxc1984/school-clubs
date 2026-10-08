using Microsoft.Extensions.DependencyInjection;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Data;
using Task = System.Threading.Tasks.Task;
using SchoolClubs.Domain.Entities.Gradebook;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Infrastructure.IntegrationTests.Repositories;

/// <summary>Проверяет, что запросы специфичных репозиториев транслируются и работают на PostgreSQL.</summary>
public class RepositoriesTests : TestBase
{
    private Group _group = null!;
    private User _teacher = null!;
    private Student _alice = null!;
    private Student _bob = null!;
    private User _parent = null!;
    private Lesson _lesson = null!;
    private DataEntry _file = null!;

    [SetUp]
    public async Task Seed()
    {
        _group = new Group("Робототехника");
        _teacher = new User("Иван", "Иванович", "Петров", "teacher@school218.ru",
            roles: [UserRole.Teacher, UserRole.Admin]);
        _group.AddTeacher(_teacher);
        _alice = new Student("Алиса", lastName: "Андреева");
        _bob = new Student("Борис", lastName: "Борисов");
        _group.AddStudent(_alice);
        _group.AddStudent(_bob);
        _parent = new User("Анна", email: "parent@school218.ru", roles: [UserRole.Parent]);
        _parent.AddChild(_alice);

        _lesson = new Lesson(_group, new DateOnly(2026, 10, 1), new TimeOnly(15, 0));
        var task = _lesson.AddTask();
        _lesson.AddTask();
        var laterLesson = new Lesson(_group, new DateOnly(2026, 10, 8), null);

        _file = new DataEntry(new S3Location("gradebooks", "2026/10/01.xlsx"), "01.xlsx", "application/xlsx", 42);
        var gradebook = new LessonGradebook(_lesson, _file);
        gradebook.SetEntry(_alice, task, GradebookEntryTaskStatus.Solved);

        await InScope(async sp =>
        {
            await sp.GetRequiredService<IUserRepository>().AddAsync(_teacher);
            await sp.GetRequiredService<IUserRepository>().AddAsync(_parent);
            await sp.GetRequiredService<IUserLoginRepository>()
                .AddAsync(UserLogin.ForPassword(_parent, "Parent@School218.ru", "hash"));
            await sp.GetRequiredService<ILessonRepository>().AddAsync(laterLesson);
            await sp.GetRequiredService<ILessonGradebookRepository>().AddAsync(gradebook);
            await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        });
    }

    [Test]
    public async Task UserRepository_FindByEmail_LoadsRolesAndStamp()
    {
        var user = await InScope(sp => sp.GetRequiredService<IUserRepository>().FindByEmailAsync("teacher@school218.ru"));

        user.ShouldNotBeNull().Id.ShouldBe(_teacher.Id);
        user.Roles.ShouldBe([UserRole.Teacher, UserRole.Admin], ignoreOrder: true);
        user.SecurityStamp.ShouldBe(_teacher.SecurityStamp);
    }

    [Test]
    public async Task UserRepository_FindByEmail_InvalidOrUnknown_ReturnsNull()
    {
        var repo = (IServiceProvider sp) => sp.GetRequiredService<IUserRepository>();

        (await InScope(sp => repo(sp).FindByEmailAsync("not an email"))).ShouldBeNull();
        (await InScope(sp => repo(sp).FindByEmailAsync("nobody@school218.ru"))).ShouldBeNull();
    }

    [Test]
    public async Task GroupRepository_Queries()
    {
        var group = await InScope(sp => sp.GetRequiredService<IGroupRepository>().GetWithMembersAsync(_group.Id));
        group.ShouldNotBeNull();
        group.Teachers.ShouldHaveSingleItem().Id.ShouldBe(_teacher.Id);
        group.Students.Count.ShouldBe(2);

        var byTeacher = await InScope(sp => sp.GetRequiredService<IGroupRepository>().ListByTeacherAsync(_teacher.Id));
        byTeacher.ShouldHaveSingleItem().Id.ShouldBe(_group.Id);

        var byStudent = await InScope(sp => sp.GetRequiredService<IGroupRepository>().ListByStudentAsync(_bob.Id));
        byStudent.ShouldHaveSingleItem().Id.ShouldBe(_group.Id);
    }

    [Test]
    public async Task UserRepository_GetWithGroups()
    {
        var teacher = await InScope(sp => sp.GetRequiredService<IUserRepository>().GetWithGroupsAsync(_teacher.Id));

        teacher.ShouldNotBeNull();
        teacher.LinkedGroups.ShouldHaveSingleItem().Id.ShouldBe(_group.Id);
    }

    [Test]
    public async Task UserRepository_ChildrenQueries()
    {
        var repo = (IServiceProvider sp) => sp.GetRequiredService<IUserRepository>();

        var parent = await InScope(sp => repo(sp).GetWithChildrenAsync(_parent.Id));
        parent.ShouldNotBeNull();
        parent.Children.ShouldHaveSingleItem().Id.ShouldBe(_alice.Id);

        var byChild = await InScope(sp => repo(sp).ListByChildAsync(_alice.Id));
        byChild.ShouldHaveSingleItem().Id.ShouldBe(_parent.Id);
        (await InScope(sp => repo(sp).ListByChildAsync(_bob.Id))).ShouldBeEmpty();
    }

    [Test]
    public async Task UserRepository_ListByRole()
    {
        var repo = (IServiceProvider sp) => sp.GetRequiredService<IUserRepository>();

        (await InScope(sp => repo(sp).ListByRoleAsync(UserRole.Admin))).ShouldHaveSingleItem().Id.ShouldBe(_teacher.Id);
        (await InScope(sp => repo(sp).ListByRoleAsync(UserRole.Parent))).ShouldHaveSingleItem().Id.ShouldBe(_parent.Id);
    }

    [Test]
    public async Task UserRepository_RevokeRole_Persists()
    {
        await InScope(async sp =>
        {
            var user = await sp.GetRequiredService<IUserRepository>().FindByIdAsync(_teacher.Id);
            user!.RevokeRole(UserRole.Admin).ShouldBeTrue();
            await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        });

        var reloaded = await InScope(sp => sp.GetRequiredService<IUserRepository>().FindByIdAsync(_teacher.Id));
        reloaded!.Roles.ShouldBe([UserRole.Teacher]);
    }

    [Test]
    public async Task UserLoginRepository_Queries()
    {
        var repo = (IServiceProvider sp) => sp.GetRequiredService<IUserLoginRepository>();

        var login = await InScope(sp => repo(sp).FindAsync(UserLogin.PasswordProvider, " PARENT@school218.ru"));
        login.ShouldNotBeNull().UserId.ShouldBe(_parent.Id);

        (await InScope(sp => repo(sp).FindAsync("yandex", "parent@school218.ru"))).ShouldBeNull();
        (await InScope(sp => repo(sp).ListByUserAsync(_parent.Id))).ShouldHaveSingleItem();
    }

    [Test]
    public async Task StudentRepository_ListByGroup_OrderedByName()
    {
        var students = await InScope(sp => sp.GetRequiredService<IStudentRepository>().ListByGroupAsync(_group.Id));

        students.Select(s => s.Id).ShouldBe([_alice.Id, _bob.Id]);
    }

    [Test]
    public async Task LessonRepository_Queries()
    {
        var lesson = await InScope(sp => sp.GetRequiredService<ILessonRepository>().GetWithTasksAsync(_lesson.Id));
        lesson.ShouldNotBeNull();
        lesson.Tasks.Select(t => t.Order).Order().ShouldBe([1L, 2L]);

        var all = await InScope(sp => sp.GetRequiredService<ILessonRepository>().ListByGroupAsync(_group.Id));
        all.Count.ShouldBe(2);
        all[0].Id.ShouldBe(_lesson.Id);

        var ranged = await InScope(sp => sp.GetRequiredService<ILessonRepository>()
            .ListByGroupAsync(_group.Id, from: new DateOnly(2026, 10, 2), to: new DateOnly(2026, 10, 31)));
        ranged.ShouldHaveSingleItem().Date.ShouldBe(new DateOnly(2026, 10, 8));
    }

    [Test]
    public async Task LessonGradebookRepository_GetByLessonId_LoadsAggregate()
    {
        var gradebook = await InScope(sp =>
            sp.GetRequiredService<ILessonGradebookRepository>().GetByLessonIdAsync(_lesson.Id));

        gradebook.ShouldNotBeNull();
        gradebook.Lesson.Tasks.Count.ShouldBe(2);
        gradebook.StoredFile.ShouldNotBeNull().Location.ShouldBe(_file.Location);
        var entry = gradebook.GradebookEntries.ShouldHaveSingleItem();
        entry.StudentId.ShouldBe(_alice.Id);
        entry.Status.ShouldBe(GradebookEntryTaskStatus.Solved);
        entry.Task.Entries.ShouldContain(entry);
    }

    [Test]
    public async Task LessonGradebookRepository_SetEntry_OnLoadedAggregate_Persists()
    {
        await InScope(async sp =>
        {
            var gradebook = await sp.GetRequiredService<ILessonGradebookRepository>().GetByLessonIdAsync(_lesson.Id);
            var students = await sp.GetRequiredService<IStudentRepository>().ListByGroupAsync(_group.Id);
            var task = gradebook!.Lesson.Tasks.Single(t => t.Order == 2);
            foreach (var student in students)
                gradebook.SetEntry(student, task, GradebookEntryTaskStatus.Unsolved);
            await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        });

        var reloaded = await InScope(sp =>
            sp.GetRequiredService<ILessonGradebookRepository>().GetByLessonIdAsync(_lesson.Id));
        reloaded!.GradebookEntries.Count.ShouldBe(3);
    }

    [Test]
    public async Task DataEntryRepository_FindByLocation()
    {
        var repo = (IServiceProvider sp) => sp.GetRequiredService<IDataEntryRepository>();

        var found = await InScope(sp => repo(sp).FindByLocationAsync(new S3Location("gradebooks", "2026/10/01.xlsx")));
        found.ShouldNotBeNull().Id.ShouldBe(_file.Id);

        (await InScope(sp => repo(sp).FindByLocationAsync(new S3Location("gradebooks", "missing")))).ShouldBeNull();
    }
}
