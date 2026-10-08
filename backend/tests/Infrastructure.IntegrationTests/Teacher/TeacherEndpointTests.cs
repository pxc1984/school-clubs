using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SchoolClubs.Application.Common.Options;
using SchoolClubs.Application.Services.Admin;
using SchoolClubs.Application.Services.Common;
using SchoolClubs.Application.Services.Gradebook;
using SchoolClubs.Application.Services.Lessons;
using SchoolClubs.Application.Services.TeacherGroups;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Gradebook;
using SchoolClubs.Domain.Entities.Users;
using Task = System.Threading.Tasks.Task;

namespace SchoolClubs.Infrastructure.IntegrationTests.Teacher;

public class TeacherEndpointTests : TestBase
{
    private const string Teacher = "/api/v1/teacher";

    private HttpClient _teacher = null!;
    private Group _group = null!;
    private Student _alice = null!;
    private Student _bob = null!;

    [SetUp]
    public async Task Seed()
    {
        User teacherUser;
        (_teacher, teacherUser) = await LoginAsAsync(UserRole.Teacher);
        _group = new Group("Робототехника");
        _alice = new Student("Алиса", lastName: "Андреева");
        _bob = new Student("Борис", lastName: "Борисов");
        _group.AddStudent(_alice);
        _group.AddStudent(_bob);

        await InScope(async sp =>
        {
            var teacher = await sp.GetRequiredService<IUserRepository>().GetWithGroupsAsync(teacherUser.Id);
            _group.AddTeacher(teacher!);
            await sp.GetRequiredService<IGroupRepository>().AddAsync(_group);
            await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        });
    }

    [TearDown]
    public void DisposeTeacher()
    {
        _teacher.Dispose();
    }

    private static DateOnly Today()
    {
        return TimeProvider.System.Today(new SchoolOptions());
    }

    private async Task<LessonDto> CreateLessonAsync(DateOnly date, int? taskCount = 2, string? topic = null)
    {
        var response = await _teacher.PostAsJsonAsync($"{Teacher}/groups/{_group.Id}/lessons",
            new CreateLessonRequest(date, null, topic, taskCount));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<LessonDto>())!;
    }

    private async Task<HttpResponseMessage> SaveAsync(Guid lessonId, params GradebookCellChange[] changes)
    {
        return await _teacher.PutAsJsonAsync($"{Teacher}/lessons/{lessonId}/gradebook",
            new SaveGradebookRequest(changes));
    }

    [Test]
    public async Task ForeignGroup_Returns403_Missing404_ParentRole403()
    {
        var (other, _) = await LoginAsAsync(UserRole.Teacher);
        using var _ = other;
        var (parent, _) = await LoginAsAsync(UserRole.Parent);
        using var __ = parent;

        (await other.GetAsync($"{Teacher}/groups/{_group.Id}")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await other.GetAsync($"{Teacher}/groups/{Guid.NewGuid()}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await parent.GetAsync($"{Teacher}/groups")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await other.GetFromJsonAsync<List<TeacherGroupCardDto>>($"{Teacher}/groups")).ShouldBeEmpty();
    }

    [Test]
    public async Task CreateLesson_DefaultTaskCount_FollowsPreviousLesson()
    {
        var first = await CreateLessonAsync(Today(), taskCount: null, topic: "Датчики");
        var second = await CreateLessonAsync(Today().AddDays(7), taskCount: 4);
        var third = await CreateLessonAsync(Today().AddDays(14), taskCount: null);

        first.Tasks.Count.ShouldBe(1);
        first.Topic.ShouldBe("Датчики");
        second.Tasks.Select(t => t.Order).ShouldBe([1L, 2L, 3L, 4L]);
        third.Tasks.Count.ShouldBe(4);
    }

    [Test]
    public async Task Gradebook_SaveGetPublish()
    {
        var lesson = await CreateLessonAsync(Today());
        var (task1, task2) = (lesson.Tasks[0].Id, lesson.Tasks[1].Id);

        var empty = await _teacher.GetFromJsonAsync<GradebookDto>($"{Teacher}/lessons/{lesson.Id}/gradebook");
        empty!.Entries.ShouldBeEmpty();
        empty.Students.Select(s => s.Id).ShouldBe([_alice.Id, _bob.Id]);
        empty.PublishedAt.ShouldBeNull();

        var saved = await (await SaveAsync(lesson.Id,
                new GradebookCellChange(_alice.Id, task1, GradebookEntryTaskStatus.Solved, null),
                new GradebookCellChange(_alice.Id, task2, GradebookEntryTaskStatus.SolvedWithRemarks, "без проверки"),
                new GradebookCellChange(_bob.Id, task1, GradebookEntryTaskStatus.Unsolved, null)))
            .Content.ReadFromJsonAsync<GradebookDto>();
        saved!.Entries.Count.ShouldBe(3);
        saved.Entries.Single(e => e.TaskId == task2).Comment.ShouldBe("без проверки");

        // Пустой статус очищает ячейку.
        var cleared = await (await SaveAsync(lesson.Id, new GradebookCellChange(_bob.Id, task1, null, null)))
            .Content.ReadFromJsonAsync<GradebookDto>();
        cleared!.Entries.Count.ShouldBe(2);

        var raw = await _teacher.GetStringAsync($"{Teacher}/lessons/{lesson.Id}/gradebook");
        raw.ShouldContain("\"SolvedWithRemarks\"");

        var published = await (await _teacher.PostAsync($"{Teacher}/lessons/{lesson.Id}/gradebook/publish", null))
            .Content.ReadFromJsonAsync<GradebookDto>();
        published!.PublishedAt.ShouldNotBeNull();

        var lessons = await _teacher.GetFromJsonAsync<List<LessonSummaryDto>>($"{Teacher}/groups/{_group.Id}/lessons");
        var summary = lessons.ShouldHaveSingleItem();
        summary.IsPublished.ShouldBeTrue();
        summary.FilledPercent.ShouldBe(50);

        var unpublished = await (await _teacher.PostAsync($"{Teacher}/lessons/{lesson.Id}/gradebook/unpublish", null))
            .Content.ReadFromJsonAsync<GradebookDto>();
        unpublished!.PublishedAt.ShouldBeNull();
    }

    [Test]
    public async Task Gradebook_RejectsStrangersForeignTasksAndDuplicates()
    {
        var lesson = await CreateLessonAsync(Today());
        var other = await CreateLessonAsync(Today().AddDays(1));
        var stranger = Guid.NewGuid();
        var task = lesson.Tasks[0].Id;

        (await SaveAsync(lesson.Id, new GradebookCellChange(stranger, task, GradebookEntryTaskStatus.Solved, null)))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await SaveAsync(lesson.Id,
                new GradebookCellChange(_alice.Id, other.Tasks[0].Id, GradebookEntryTaskStatus.Solved, null)))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await SaveAsync(lesson.Id,
                new GradebookCellChange(_alice.Id, task, GradebookEntryTaskStatus.Solved, null),
                new GradebookCellChange(_alice.Id, task, GradebookEntryTaskStatus.Unsolved, null)))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Отказ — без частичной записи.
        (await _teacher.GetFromJsonAsync<GradebookDto>($"{Teacher}/lessons/{lesson.Id}/gradebook"))!
            .Entries.ShouldBeEmpty();
    }

    [Test]
    public async Task Tasks_AddRenameDelete_CascadesMarks()
    {
        var lesson = await CreateLessonAsync(Today());
        await SaveAsync(lesson.Id,
            new GradebookCellChange(_alice.Id, lesson.Tasks[0].Id, GradebookEntryTaskStatus.Solved, null),
            new GradebookCellChange(_alice.Id, lesson.Tasks[1].Id, GradebookEntryTaskStatus.Solved, null));

        var added = await (await _teacher.PostAsJsonAsync($"{Teacher}/lessons/{lesson.Id}/tasks",
            new TaskTitleRequest("Бонус"))).Content.ReadFromJsonAsync<LessonDto>();
        added!.Tasks.Select(t => t.Title).ShouldBe([null, null, "Бонус"]);
        added.Tasks[0].EntryCount.ShouldBe(1);

        var renamed = await (await _teacher.PutAsJsonAsync($"{Teacher}/lessons/{lesson.Id}/tasks/{lesson.Tasks[0].Id}",
            new TaskTitleRequest("Разминка"))).Content.ReadFromJsonAsync<LessonDto>();
        renamed!.Tasks[0].Title.ShouldBe("Разминка");

        var afterDelete = await (await _teacher.DeleteAsync($"{Teacher}/lessons/{lesson.Id}/tasks/{lesson.Tasks[0].Id}"))
            .Content.ReadFromJsonAsync<LessonDto>();
        afterDelete!.Tasks.Select(t => (t.Order, t.Title)).ShouldBe([(1L, null), (2L, "Бонус")]);

        var gradebook = await _teacher.GetFromJsonAsync<GradebookDto>($"{Teacher}/lessons/{lesson.Id}/gradebook");
        gradebook!.Entries.ShouldHaveSingleItem().TaskId.ShouldBe(lesson.Tasks[1].Id);
    }

    [Test]
    public async Task DeleteLesson_OnlyWithoutMarks()
    {
        var withMarks = await CreateLessonAsync(Today());
        var empty = await CreateLessonAsync(Today().AddDays(7));
        await SaveAsync(withMarks.Id,
            new GradebookCellChange(_alice.Id, withMarks.Tasks[0].Id, GradebookEntryTaskStatus.Solved, null));

        (await _teacher.DeleteAsync($"{Teacher}/lessons/{withMarks.Id}")).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await _teacher.DeleteAsync($"{Teacher}/lessons/{empty.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task UpdateLesson_ChangesDateAndTopic()
    {
        var lesson = await CreateLessonAsync(Today());

        var updated = await (await _teacher.PutAsJsonAsync($"{Teacher}/lessons/{lesson.Id}",
                new UpdateLessonRequest(Today().AddDays(1), new TimeOnly(15, 30), "Сервоприводы")))
            .Content.ReadFromJsonAsync<LessonDto>();

        updated!.Date.ShouldBe(Today().AddDays(1));
        updated.Time.ShouldBe(new TimeOnly(15, 30));
        updated.Topic.ShouldBe("Сервоприводы");
        (await _teacher.PutAsJsonAsync($"{Teacher}/lessons/{lesson.Id}",
                new UpdateLessonRequest(Today(), new TimeOnly(23, 0), null)))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task RemovedStudent_KeepsMarks_ShownAsFormer()
    {
        var lesson = await CreateLessonAsync(Today());
        await SaveAsync(lesson.Id,
            new GradebookCellChange(_bob.Id, lesson.Tasks[0].Id, GradebookEntryTaskStatus.Solved, null));

        var added = await (await _teacher.PostAsJsonAsync($"{Teacher}/groups/{_group.Id}/students",
            new NewStudentRequest("Васильева", "Вера", null, "6а"))).Content.ReadFromJsonAsync<StudentDto>();
        added!.Grade.ShouldBe("6А");
        (await _teacher.DeleteAsync($"{Teacher}/groups/{_group.Id}/students/{_bob.Id}"))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var gradebook = await _teacher.GetFromJsonAsync<GradebookDto>($"{Teacher}/lessons/{lesson.Id}/gradebook");
        gradebook!.Students.Select(s => (s.LastName, s.InGroup))
            .ShouldBe([("Андреева", true), ("Васильева", true), ("Борисов", false)]);
        gradebook.Entries.ShouldHaveSingleItem().StudentId.ShouldBe(_bob.Id);

        // Выбывшему отметку больше не поставить, но можно очистить.
        (await SaveAsync(lesson.Id,
                new GradebookCellChange(_bob.Id, lesson.Tasks[1].Id, GradebookEntryTaskStatus.Solved, null)))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await SaveAsync(lesson.Id, new GradebookCellChange(_bob.Id, lesson.Tasks[0].Id, null, null)))
            .StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GroupCardsAndSummary()
    {
        var past = await CreateLessonAsync(Today().AddDays(-7));
        var filled = await CreateLessonAsync(Today(), taskCount: 1);
        await CreateLessonAsync(Today().AddDays(7));
        await SaveAsync(filled.Id,
            new GradebookCellChange(_alice.Id, filled.Tasks[0].Id, GradebookEntryTaskStatus.Solved, null),
            new GradebookCellChange(_bob.Id, filled.Tasks[0].Id, GradebookEntryTaskStatus.UnsolvedWithRemarks, "x"));
        await SaveAsync(past.Id,
            new GradebookCellChange(_alice.Id, past.Tasks[0].Id, GradebookEntryTaskStatus.SolvedWithRemarks, null),
            new GradebookCellChange(_alice.Id, past.Tasks[1].Id, GradebookEntryTaskStatus.Unsolved, null));

        var card = (await _teacher.GetFromJsonAsync<List<TeacherGroupCardDto>>($"{Teacher}/groups"))
            .ShouldHaveSingleItem();
        card.StudentCount.ShouldBe(2);
        card.LastLessonDate.ShouldBe(Today());
        card.NextLessonDate.ShouldBe(Today().AddDays(7));
        card.UnfilledLessonCount.ShouldBe(1);

        var matrix = await _teacher.GetFromJsonAsync<GroupMatrixDto>($"{Teacher}/groups/{_group.Id}/summary");
        matrix!.Lessons.Count.ShouldBe(3);
        matrix.Students.Select(s => s.Id).ShouldBe([_alice.Id, _bob.Id]);
        matrix.Cells.ShouldContain(new SummaryCellDto(_alice.Id, past.Id, 2, 1));
        matrix.Cells.ShouldContain(new SummaryCellDto(_bob.Id, filled.Id, 1, 0));
        matrix.Cells.Count.ShouldBe(3);
    }
}
