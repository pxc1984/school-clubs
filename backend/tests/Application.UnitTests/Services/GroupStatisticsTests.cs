using NUnit.Framework;
using Shouldly;
using SchoolClubs.Application.Common.Options;
using SchoolClubs.Application.Services.ParentDashboard;
using SchoolClubs.Domain.Entities.Gradebook;
using static SchoolClubs.Domain.Entities.Gradebook.GradebookEntryTaskStatus;

namespace SchoolClubs.Application.UnitTests.Services;

[TestFixture]
[TestOf(typeof(GroupStatistics))]
public class GroupStatisticsTests
{
    private static readonly StatisticsWeights Weights = new();

    private readonly List<PublishedLessonRow> _lessons = [];
    private readonly List<MarkRow> _marks = [];
    private readonly List<AttendanceRow> _attendance = [];

    [SetUp]
    public void Reset()
    {
        _lessons.Clear();
        _marks.Clear();
        _attendance.Clear();
    }

    private PublishedLessonRow AddLesson(int day, int tasks)
    {
        var lesson = new PublishedLessonRow(Guid.NewGuid(), new DateOnly(2026, 10, day), null, null,
            Enumerable.Range(1, tasks).Select(i => new LessonTaskRow(Guid.NewGuid(), i.ToString(), null)).ToList());
        _lessons.Add(lesson);
        return lesson;
    }

    private void Mark(Guid student, PublishedLessonRow lesson, params GradebookEntryTaskStatus[] statuses)
    {
        for (var i = 0; i < statuses.Length; i++)
            _marks.Add(new MarkRow(student, lesson.Id, lesson.Tasks[i].Id, statuses[i]));
    }

    private GroupStatistics Compute()
    {
        return GroupStatistics.Compute(new GroupPeriodData(_lessons, _marks, _attendance), Weights);
    }

    private static List<Guid> Students(int count)
    {
        return Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList();
    }

    [Test]
    public void LessonScore_UsesWeights_MissingMarksOfAttendeeAreZero()
    {
        var lesson = AddLesson(1, 5);
        var child = Guid.NewGuid();
        Mark(child, lesson, Solved, SolvedWithRemarks, UnsolvedWithRemarks, Unsolved);

        var result = Compute().Lessons.ShouldHaveSingleItem().ResultOf(child)!;

        result.Solved.ShouldBe(2);
        result.TaskCount.ShouldBe(5);
        result.Score.ShouldBe((1 + 0.75 + 0.25) / 5, 1e-9);
        result.SolvedAll.ShouldBeFalse();
    }

    [Test]
    public void Absence_IsNotZero_PeriodCountsOnlyAttendedLessons()
    {
        var first = AddLesson(1, 2);
        AddLesson(2, 3);
        var third = AddLesson(3, 2);
        var child = Guid.NewGuid();
        Mark(child, first, Solved, Solved);
        Mark(child, third, Solved, Unsolved);

        var period = Compute().PeriodOf(child);

        period.ShouldBe(new StudentPeriodResult(2, 3, 3, 4));
        period.Score.ShouldBe(0.75);
        new StudentPeriodResult(0, 0, 0, 0).Score.ShouldBeNull();
    }

    [Test]
    public void LessonsWithoutTasks_AreIgnored_AndOrderedByDate()
    {
        AddLesson(5, 1);
        AddLesson(3, 0);
        AddLesson(1, 1);

        Compute().Lessons.Select(l => l.Lesson.Date.Day).ShouldBe([1, 5]);
    }

    [Test]
    public void AverageAndMedian_NeedThreePeersBesidesChild()
    {
        var lesson = AddLesson(1, 2);
        var child = Guid.NewGuid();
        var peers = Students(3);
        Mark(child, lesson, Solved, Solved);
        Mark(peers[0], lesson, Solved, Unsolved);
        Mark(peers[1], lesson, Unsolved, Unsolved);

        var stats = Compute().Lessons[0];
        stats.AverageSolved(child).ShouldBeNull();
        stats.MedianScore(child).ShouldBeNull();
        // Для того, кто не был, «кроме ребёнка» — все трое посетивших.
        stats.AverageSolved(Guid.NewGuid()).ShouldBe(1);

        Mark(peers[2], lesson, Solved, SolvedWithRemarks);
        stats = Compute().Lessons[0];
        stats.AverageSolved(child).ShouldBe(1.25);
        stats.MedianScore(child)!.Value.ShouldBe((0.5 + 0.875) / 2, 1e-9);
    }

    [Test]
    public void TaskSolvedBy_NeedsThreeAttendees()
    {
        var lesson = AddLesson(1, 2);
        var students = Students(3);
        Mark(students[0], lesson, Solved, Unsolved);
        Mark(students[1], lesson, SolvedWithRemarks);

        Compute().Lessons[0].TaskSolvedBy(lesson.Tasks[0].Id).ShouldBeNull();

        // Посетил, если есть хоть одна отметка: второй отмечен только по первой задаче.
        Mark(students[2], lesson, Unsolved, Unsolved);
        var stats = Compute().Lessons[0];
        stats.TaskSolvedBy(lesson.Tasks[0].Id).ShouldBe((2, 3));
        stats.TaskSolvedBy(lesson.Tasks[1].Id).ShouldBe((0, 3));
    }

    [Test]
    public void ScoreBand_NeedsFiveAttendees()
    {
        var lesson = AddLesson(1, 4);
        var students = Students(5);
        var statuses = new[] { Unsolved, Solved, Solved, Solved };
        for (var i = 0; i < 4; i++)
            Mark(students[i], lesson, statuses[..(i + 1)]);

        Compute().Lessons[0].ScoreBand().ShouldBeNull();

        Mark(students[4], lesson, Solved, Solved, Solved, Solved);
        // Баллы 0, 0.25, 0.5, 0.75, 1.
        Compute().Lessons[0].ScoreBand().ShouldBe((0.25, 0.75));
    }

    [Test]
    public void Percentile_MidRank_AmongStudentsWithThreeLessons()
    {
        var lessons = Enumerable.Range(1, 3).Select(d => AddLesson(d, 1)).ToList();
        var students = Students(6);
        // Ученики 0..4 посетили все три занятия, решили 0..3 задачи (у 3 и 4 — ничья по 3).
        int[] solved = [0, 1, 2, 3, 3];
        for (var s = 0; s < 5; s++)
        {
            for (var l = 0; l < 3; l++)
                Mark(students[s], lessons[l], l < solved[s] ? Solved : Unsolved);
        }

        // Шестой был на двух занятиях — в ранжирование не попадает.
        Mark(students[5], lessons[0], Solved);
        Mark(students[5], lessons[1], Solved);

        var stats = Compute();
        stats.PercentileOf(students[0]).ShouldBe(0.1);
        stats.PercentileOf(students[3]).ShouldBe(0.8);
        stats.PercentileOf(students[5]).ShouldBeNull();
        stats.PositionOf(students[3]).ShouldBe(GroupPosition.Top25);
        stats.PositionOf(students[2]).ShouldBeNull();
    }

    [TestCase(0, null)]
    [TestCase(4, null)]
    [TestCase(5, GroupPosition.AboveHalf)]
    [TestCase(7, GroupPosition.Top25)]
    [TestCase(8, GroupPosition.Top25)]
    [TestCase(9, GroupPosition.Top10)]
    public void Position_Buckets(int rank, GroupPosition? expected)
    {
        var lessons = Enumerable.Range(1, 3).Select(d => AddLesson(d, 10)).ToList();
        var students = Students(10);
        // Ученик i решает i задач на каждом занятии: перцентиль = (i + 0.5) / 10.
        for (var s = 0; s < 10; s++)
        {
            foreach (var lesson in lessons)
                Mark(students[s], lesson, Enumerable.Range(0, 10).Select(t => t < s ? Solved : Unsolved).ToArray());
        }

        Compute().PositionOf(students[rank]).ShouldBe(expected);
    }

    [Test]
    public void Position_NeedsFiveRanked()
    {
        var lessons = Enumerable.Range(1, 3).Select(d => AddLesson(d, 1)).ToList();
        var students = Students(4);
        foreach (var student in students)
        {
            foreach (var lesson in lessons)
                Mark(student, lesson, Solved);
        }

        Compute().PercentileOf(students[0]).ShouldBeNull();
    }

    [Test]
    public void PerfectStreak_CountsLatestAttended_SkipsAbsences()
    {
        var lessons = Enumerable.Range(1, 5).Select(d => AddLesson(d, 2)).ToList();
        var child = Guid.NewGuid();
        Mark(child, lessons[0], Solved, Solved);
        Mark(child, lessons[1], Solved, Unsolved);
        Mark(child, lessons[2], Solved, SolvedWithRemarks);
        Mark(child, lessons[4], Solved, Solved);

        Compute().PerfectStreakOf(child).ShouldBe(2);

        Mark(child, lessons[3], Solved);
        Compute().PerfectStreakOf(child).ShouldBeNull();
    }

    [Test]
    public void Attendance_AbsentExcludesMarks_LateCountsAsAttended()
    {
        var lesson = AddLesson(1, 2);
        var (absent, late, lateWithoutMarks) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Mark(absent, lesson, Solved, Solved);
        Mark(late, lesson, Solved);
        _attendance.Add(new AttendanceRow(absent, lesson.Id, AttendanceStatus.Absent));
        _attendance.Add(new AttendanceRow(late, lesson.Id, AttendanceStatus.Late));
        _attendance.Add(new AttendanceRow(lateWithoutMarks, lesson.Id, AttendanceStatus.Late));

        var stats = Compute().Lessons.ShouldHaveSingleItem();

        stats.ResultOf(absent).ShouldBeNull();
        stats.AttendanceOf(absent).ShouldBe(AttendanceStatus.Absent);
        stats.ResultOf(late)!.Score.ShouldBe(0.5);
        stats.AttendanceOf(late).ShouldBe(AttendanceStatus.Late);
        stats.ResultOf(lateWithoutMarks)!.Solved.ShouldBe(0);
        stats.AttendanceOf(Guid.NewGuid()).ShouldBeNull();
        stats.TaskSolvedBy(lesson.Tasks[0].Id).ShouldBeNull();
    }

    [Test]
    public void Weights_AreConfigurable()
    {
        var lesson = AddLesson(1, 2);
        var child = Guid.NewGuid();
        Mark(child, lesson, SolvedWithRemarks, UnsolvedWithRemarks);
        var strict = new StatisticsWeights { SolvedWithRemarks = 0.5, UnsolvedWithRemarks = 0 };

        GroupStatistics.Compute(new GroupPeriodData(_lessons, _marks, _attendance), strict).Lessons[0].ResultOf(child)!.Score
            .ShouldBe(0.25);
    }

    [TestCase(0, 1)]
    [TestCase(0.5, 2.5)]
    [TestCase(0.25, 1.75)]
    [TestCase(1, 4)]
    public void Quantile_LinearInterpolation(double p, double expected)
    {
        GroupStatistics.Quantile([1, 2, 3, 4], p).ShouldBe(expected);
    }
}
