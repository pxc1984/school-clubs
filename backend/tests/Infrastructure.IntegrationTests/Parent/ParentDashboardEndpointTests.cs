using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SchoolClubs.Application.Services.ParentDashboard;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Gradebook;
using SchoolClubs.Domain.Entities.Users;
using static SchoolClubs.Domain.Entities.Gradebook.GradebookEntryTaskStatus;
using Task = System.Threading.Tasks.Task;

namespace SchoolClubs.Infrastructure.IntegrationTests.Parent;

public class ParentDashboardEndpointTests : TestBase
{
    private const string Children = "/api/v1/parent/children";

    private HttpClient _parent = null!;
    private Group _group = null!;
    private Group _otherGroup = null!;
    private Student _child = null!;
    private Student _stranger = null!;
    private List<Lesson> _lessons = null!;
    private Lesson _draft = null!;

    /// <summary>
    /// Ребёнок и четверо одногруппников, четыре опубликованных занятия по две задачи и черновик.
    /// Ребёнок решил всё на первых трёх и пропустил четвёртое; остальные везде ничего не решили.
    /// </summary>
    [SetUp]
    public async Task Seed()
    {
        User parentUser;
        (_parent, parentUser) = await LoginAsAsync(UserRole.Parent);
        _group = new Group("Робототехника");
        _otherGroup = new Group("Шахматы");
        _child = new Student("Пётр", lastName: "Иванов", grade: "7Б");
        _stranger = new Student("Анна", lastName: "Петрова");
        _group.AddStudent(_child);
        _otherGroup.AddStudent(_stranger);
        var peers = Enumerable.Range(1, 4).Select(i => new Student($"Ученик{i}", lastName: "Сидоров")).ToList();
        foreach (var peer in peers)
            _group.AddStudent(peer);

        _lessons = Enumerable.Range(0, 4)
            .Select(i => new Lesson(_group, new DateOnly(2026, 10, 1).AddDays(7 * i), new TimeOnly(15, 0),
                $"Тема {i + 1}"))
            .ToList();
        _draft = new Lesson(_group, new DateOnly(2026, 10, 29), null, "Черновик");
        var gradebooks = new List<LessonGradebook>();
        foreach (var lesson in _lessons.Append(_draft))
        {
            var first = lesson.AddTask("Разминка");
            var second = lesson.AddTask();
            var gradebook = new LessonGradebook(lesson);
            foreach (var peer in peers)
            {
                gradebook.SetEntry(peer, first, Unsolved);
                gradebook.SetEntry(peer, second, Unsolved);
            }

            if (lesson != _lessons[3])
            {
                gradebook.SetEntry(_child, first, Solved);
                gradebook.SetEntry(_child, second, lesson == _lessons[2] ? SolvedWithRemarks : Solved,
                    lesson == _lessons[2] ? "Не обработан пустой ввод" : null);
            }

            if (lesson != _draft)
                gradebook.Publish(DateTimeOffset.UtcNow);
            gradebooks.Add(gradebook);
        }

        await InScope(async sp =>
        {
            var parent = await sp.GetRequiredService<IUserRepository>().GetWithChildrenAsync(parentUser.Id);
            parent!.AddChild(_child);
            await sp.GetRequiredService<IGroupRepository>().AddAsync(_group);
            await sp.GetRequiredService<IGroupRepository>().AddAsync(_otherGroup);
            foreach (var gradebook in gradebooks)
                await sp.GetRequiredService<ILessonGradebookRepository>().AddAsync(gradebook);
            await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        });
    }

    [TearDown]
    public void DisposeClient()
    {
        _parent.Dispose();
    }

    private string GroupUrl(string tail, Guid? studentId = null, Guid? groupId = null)
    {
        return $"{Children}/{studentId ?? _child.Id}/groups/{groupId ?? _group.Id}/{tail}";
    }

    [Test]
    public async Task Children_CardWithLastPublishedLessonAndPosition()
    {
        var children = await _parent.GetFromJsonAsync<List<ChildDto>>(Children);

        var child = children.ShouldHaveSingleItem();
        child.Id.ShouldBe(_child.Id);
        child.Grade.ShouldBe("7Б");
        var group = child.Groups.ShouldHaveSingleItem();
        group.Title.ShouldBe("Робототехника");
        group.LessonsAttended.ShouldBe(3);
        group.LessonsTotal.ShouldBe(4);
        group.Position.ShouldBe(GroupPosition.Top10);
        // Черновик не виден: последнее — пропущенное четвёртое занятие.
        group.LastLesson.ShouldBe(new LastLessonDto(_lessons[3].Id, _lessons[3].Date, "Тема 4", false, 0, 2, 0));
    }

    [Test]
    public async Task Summary_TotalsStreakAndChart()
    {
        var summary = await _parent.GetFromJsonAsync<ChildSummaryDto>(GroupUrl("summary"));

        summary!.Student.Id.ShouldBe(_child.Id);
        summary.Group.Id.ShouldBe(_group.Id);
        summary.LessonsAttended.ShouldBe(3);
        summary.LessonsTotal.ShouldBe(4);
        summary.TasksSolved.ShouldBe(6);
        summary.TasksTotal.ShouldBe(6);
        summary.Score!.Value.ShouldBe(5.75 / 6, 1e-9);
        summary.Position.ShouldBe(GroupPosition.Top10);
        summary.PerfectStreak.ShouldBe(3);

        summary.Chart.Select(p => p.LessonId).ShouldBe(_lessons.Select(l => l.Id));
        summary.Chart[0].ShouldBe(new ChartPointDto(_lessons[0].Id, _lessons[0].Date, 1, 0, 0, 0));
        // На четвёртом ребёнка не было: точки нет, полосы нет (посетивших четверо), медиана есть.
        summary.Chart[3].ShouldBe(new ChartPointDto(_lessons[3].Id, _lessons[3].Date, null, 0, null, null));
    }

    [Test]
    public async Task Summary_Period_FiltersLessonsAndHidesPosition()
    {
        var summary = await _parent.GetFromJsonAsync<ChildSummaryDto>(
            GroupUrl("summary") + "?from=2026-10-08&to=2026-10-15");

        summary!.LessonsTotal.ShouldBe(2);
        summary.LessonsAttended.ShouldBe(2);
        summary.From.ShouldBe(new DateOnly(2026, 10, 8));
        // Для ранжирования нужно ≥ 3 посещённых занятий в периоде.
        summary.Position.ShouldBeNull();

        (await _parent.GetAsync(GroupUrl("summary") + "?from=2026-10-15&to=2026-10-08"))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Lessons_FeedNewestFirst_WithMarksCommentsAndTaskAggregates()
    {
        var feed = (await _parent.GetFromJsonAsync<List<ChildLessonDto>>(GroupUrl("lessons")))!;

        feed.Select(l => l.LessonId).ShouldBe(_lessons.Select(l => l.Id).Reverse());

        var missed = feed[0];
        missed.Attended.ShouldBeFalse();
        missed.Score.ShouldBeNull();
        missed.Tasks.ShouldAllBe(t => t.Status == null && t.GroupSolved == 0 && t.GroupAttended == 4);

        var third = feed[1];
        third.Topic.ShouldBe("Тема 3");
        third.Time.ShouldBe(new TimeOnly(15, 0));
        third.Attended.ShouldBeTrue();
        third.Solved.ShouldBe(2);
        third.TaskCount.ShouldBe(2);
        third.Score!.Value.ShouldBe(0.875, 1e-9);
        third.GroupAverageSolved.ShouldBe(0.4);
        third.Tasks.ShouldBe([
            new ChildTaskDto(third.Tasks[0].TaskId, 1, "Разминка", Solved, null, 1, 5),
            new ChildTaskDto(third.Tasks[1].TaskId, 2, null, SolvedWithRemarks, "Не обработан пустой ввод", 1, 5),
        ]);
    }

    [Test]
    public async Task Lessons_OtherStudentsNeverListed()
    {
        var raw = await _parent.GetStringAsync(GroupUrl("lessons"));
        var summary = await _parent.GetStringAsync(GroupUrl("summary"));

        raw.ShouldNotContain("Сидоров");
        summary.ShouldNotContain("Сидоров");
    }

    [Test]
    public async Task Access_OnlyOwnChildrenAndTheirGroups()
    {
        (await _parent.GetAsync(GroupUrl("summary", _stranger.Id, _otherGroup.Id)))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await _parent.GetAsync(GroupUrl("lessons", groupId: _otherGroup.Id)))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var (teacher, _) = await LoginAsAsync(UserRole.Teacher);
        using var _ = teacher;
        (await teacher.GetAsync(Children)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Unpublish_HidesLessonFromParent()
    {
        var (teacherClient, teacher) = await LoginAsAsync(UserRole.Teacher);
        using var _ = teacherClient;
        await InScope(async sp =>
        {
            var group = await sp.GetRequiredService<IGroupRepository>().GetWithMembersAsync(_group.Id);
            var user = await sp.GetRequiredService<IUserRepository>().GetWithGroupsAsync(teacher.Id);
            group!.AddTeacher(user!);
            await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        });

        (await teacherClient.PostAsync($"/api/v1/teacher/lessons/{_lessons[3].Id}/gradebook/unpublish", null))
            .EnsureSuccessStatusCode();
        (await teacherClient.PostAsync($"/api/v1/teacher/lessons/{_draft.Id}/gradebook/publish", null))
            .EnsureSuccessStatusCode();

        var feed = (await _parent.GetFromJsonAsync<List<ChildLessonDto>>(GroupUrl("lessons")))!;
        feed.Select(l => l.LessonId).ShouldBe([_draft.Id, _lessons[2].Id, _lessons[1].Id, _lessons[0].Id]);
        feed[0].Attended.ShouldBeTrue();
    }
}
