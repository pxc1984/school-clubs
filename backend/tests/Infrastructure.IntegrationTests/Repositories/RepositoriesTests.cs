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
    private Teacher _teacher = null!;
    private Student _alice = null!;
    private Student _bob = null!;
    private Parent _parent = null!;
    private Lesson _lesson = null!;
    private DataEntry _file = null!;

    [SetUp]
    public async Task Seed()
    {
        _group = new Group("Робототехника");
        _teacher = new Teacher("Иван", "Иванович", "Петров", "teacher@school218.ru", linkedGroups: [_group]);
        _alice = new Student("Алиса", lastName: "Андреева");
        _bob = new Student("Борис", lastName: "Борисов");
        _group.AddStudent(_alice);
        _group.AddStudent(_bob);
        _parent = new Parent("Анна", email: "parent@school218.ru", children: [_alice]);

        _lesson = new Lesson(_group, new DateOnly(2026, 10, 1), new TimeOnly(15, 0));
        var task = _lesson.AddTask();
        _lesson.AddTask();
        var laterLesson = new Lesson(_group, new DateOnly(2026, 10, 8), null);

        _file = new DataEntry(new S3Location("gradebooks", "2026/10/01.xlsx"), "01.xlsx", "application/xlsx", 42);
        var gradebook = new LessonGradebook(_lesson, _file);
        gradebook.SetEntry(_alice, task, GradebookEntryTaskStatus.Solved);

        await InScope(async sp =>
        {
            await sp.GetRequiredService<ITeacherRepository>().AddAsync(_teacher);
            await sp.GetRequiredService<IParentRepository>().AddAsync(_parent);
            await sp.GetRequiredService<ILessonRepository>().AddAsync(laterLesson);
            await sp.GetRequiredService<ILessonGradebookRepository>().AddAsync(gradebook);
            await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        });
    }

    [Test]
    public async Task UserRepository_FindByEmail_ReturnsDerivedUser()
    {
        var user = await InScope(sp => sp.GetRequiredService<IUserRepository>().FindByEmailAsync("teacher@school218.ru"));

        user.ShouldBeOfType<Teacher>().Id.ShouldBe(_teacher.Id);
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
    public async Task TeacherRepository_GetWithGroups()
    {
        var teacher = await InScope(sp => sp.GetRequiredService<ITeacherRepository>().GetWithGroupsAsync(_teacher.Id));

        teacher.ShouldNotBeNull();
        teacher.LinkedGroups.ShouldHaveSingleItem().Id.ShouldBe(_group.Id);
    }

    [Test]
    public async Task ParentRepository_Queries()
    {
        var parent = await InScope(sp => sp.GetRequiredService<IParentRepository>().GetWithChildrenAsync(_parent.Id));
        parent.ShouldNotBeNull();
        parent.Children.ShouldHaveSingleItem().Id.ShouldBe(_alice.Id);

        var byChild = await InScope(sp => sp.GetRequiredService<IParentRepository>().ListByChildAsync(_alice.Id));
        byChild.ShouldHaveSingleItem().Id.ShouldBe(_parent.Id);
        (await InScope(sp => sp.GetRequiredService<IParentRepository>().ListByChildAsync(_bob.Id))).ShouldBeEmpty();
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

    private static async Task<T> InScope<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        return await action(scope.ServiceProvider);
    }

    private static async Task InScope(Func<IServiceProvider, Task> action)
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        await action(scope.ServiceProvider);
    }
}
