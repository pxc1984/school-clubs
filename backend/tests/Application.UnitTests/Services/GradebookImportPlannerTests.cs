using NUnit.Framework;
using Shouldly;
using SchoolClubs.Application.Services.GradebookImport;
using SchoolClubs.Domain.Entities.Gradebook;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Application.UnitTests.Services;

[TestFixture]
[TestOf(typeof(GradebookImportPlanner))]
public class GradebookImportPlannerTests
{
    private Group _group = null!;
    private Lesson _lesson = null!;
    private Domain.Entities.Gradebook.Task _task1 = null!;
    private Domain.Entities.Gradebook.Task _task2 = null!;
    private Student _alice = null!;
    private Student _bob = null!;
    private Student _semyon = null!;

    [SetUp]
    public void SetUp()
    {
        _group = new Group("Робототехника");
        _alice = new Student("Алиса", lastName: "Андреева");
        _bob = new Student("Борис", lastName: "Борисов");
        _semyon = new Student("Семён", "Петрович", "Ёлкин");
        foreach (var student in new[] { _alice, _bob, _semyon })
            _group.AddStudent(student);
        _lesson = new Lesson(_group, new DateOnly(2026, 10, 1), null);
        _task1 = _lesson.AddTask();
        _task2 = _lesson.AddTask();
    }

    private ParsedMeta OwnMeta()
    {
        return new ParsedMeta(1, _lesson.Id, _group.Id, DateTimeOffset.UtcNow);
    }

    private ParsedColumn[] OwnColumns()
    {
        return [new ParsedColumn("C", _task1.Id, 1, "Задача 1"), new ParsedColumn("D", _task2.Id, 2, "Задача 2")];
    }

    private static ParsedRow Row(int row, Guid? id, string name, params (string Column, string Text)[] cells)
    {
        return new ParsedRow(row, id, name, cells.Select(c => new ParsedCell(c.Column, $"{c.Column}{row}", c.Text)).ToList());
    }

    [Test]
    public void ReorderedRows_MatchById_NewChangedUnchanged()
    {
        var gradebook = new LessonGradebook(_lesson);
        gradebook.SetEntry(_alice, _task1, GradebookEntryTaskStatus.Solved);
        gradebook.SetEntry(_bob, _task1, GradebookEntryTaskStatus.Unsolved);
        var file = new ParsedGradebook(OwnMeta(), OwnColumns(),
        [
            Row(3, _bob.Id, "Борисов Борис", ("C", "+")),
            Row(4, _alice.Id, "Андреева Алиса", ("C", "+"), ("D", "-.")),
        ]);

        var plan = GradebookImportPlanner.Plan(file, _lesson, _group, gradebook);

        plan.Changes.Select(c => (c.StudentId, c.TaskNumber, c.Kind)).ShouldBe(
        [
            (_bob.Id, 1L, ImportChangeKind.Changed),
            (_alice.Id, 1L, ImportChangeKind.Unchanged),
            (_alice.Id, 2L, ImportChangeKind.New),
        ]);
        plan.MissingStudents.ShouldHaveSingleItem().Id.ShouldBe(_semyon.Id);
        plan.ForeignFile.ShouldBeFalse();
        plan.LessonMismatch.ShouldBeFalse();
    }

    [Test]
    public void ForeignFile_MatchesByNormalizedName_AndTaskNumber()
    {
        var file = new ParsedGradebook(null,
            [new ParsedColumn("B", null, 1, "Задача 1"), new ParsedColumn("C", null, 5, "Задача 5"), new ParsedColumn("D", null, null, "Итого")],
            [
                Row(2, null, "  алиса   АНДРЕЕВА ", ("B", "да"), ("C", "+"), ("D", "2")),
                Row(3, null, "Елкин Семен Петрович", ("B", "решил почти")),
                Row(4, null, "Неизвестный Ученик", ("B", "+")),
            ]);

        var plan = GradebookImportPlanner.Plan(file, _lesson, _group, null);

        plan.ForeignFile.ShouldBeTrue();
        plan.Changes.Select(c => (c.StudentId, c.TaskId, c.TaskNumber)).ShouldBe(
            [(_alice.Id, _task1.Id, 1L), (_alice.Id, (Guid?) null, 5L)]);
        plan.MissingTaskNumbers.ShouldBe([5L]);
        plan.CellErrors.ShouldBe(["B3: «решил почти»"]);
        plan.Warnings.ShouldHaveSingleItem().ShouldContain("Итого");
        plan.UnrecognizedRows.ShouldHaveSingleItem().Row.ShouldBe(4);
        plan.MissingStudents.Select(s => s.Id).ShouldBe([_bob.Id]);
    }

    [Test]
    public void AmbiguousName_IsUnrecognizedWithCandidates_ManualMappingResolves()
    {
        var twin = new Student("Борис", lastName: "Борисов");
        _group.AddStudent(twin);
        var file = new ParsedGradebook(null, [new ParsedColumn("B", null, 1, "1")],
            [Row(2, null, "Борис Борисов", ("B", "+"))]);

        var plan = GradebookImportPlanner.Plan(file, _lesson, _group, null);
        var row = plan.UnrecognizedRows.ShouldHaveSingleItem();
        row.Candidates.Count.ShouldBe(2);
        plan.Changes.ShouldBeEmpty();

        var mapped = GradebookImportPlanner.Plan(file, _lesson, _group, null, new Dictionary<int, Guid?> { [2] = twin.Id });
        mapped.Changes.ShouldHaveSingleItem().StudentId.ShouldBe(twin.Id);

        var skipped = GradebookImportPlanner.Plan(file, _lesson, _group, null, new Dictionary<int, Guid?> { [2] = null });
        skipped.UnrecognizedRows.ShouldBeEmpty();
        skipped.Changes.ShouldBeEmpty();
    }

    [Test]
    public void DuplicateStudent_IsBlockingError()
    {
        var file = new ParsedGradebook(OwnMeta(), OwnColumns(),
            [Row(3, _alice.Id, "Андреева Алиса", ("C", "+")), Row(7, null, "Андреева Алиса", ("D", "+"))]);

        var plan = GradebookImportPlanner.Plan(file, _lesson, _group, null);

        plan.Errors.ShouldHaveSingleItem().ShouldContain("строках 3 и 7");
    }

    [Test]
    public void DeletedTaskColumn_IsIgnoredWithWarning()
    {
        var deleted = Guid.NewGuid();
        var file = new ParsedGradebook(OwnMeta(), [.. OwnColumns(), new ParsedColumn("E", deleted, 3, "Задача 3")],
            [Row(3, _alice.Id, "Андреева Алиса", ("E", "+"))]);

        var plan = GradebookImportPlanner.Plan(file, _lesson, _group, null);

        plan.Changes.ShouldBeEmpty();
        plan.MissingTaskNumbers.ShouldBeEmpty();
        plan.Warnings.ShouldHaveSingleItem().ShouldContain("удалена");
    }

    [Test]
    public void FileOfAnotherLesson_IsMismatch_TasksFallBackToNumbers()
    {
        var otherLesson = Guid.NewGuid();
        var file = new ParsedGradebook(new ParsedMeta(1, otherLesson, _group.Id, null),
            [new ParsedColumn("C", Guid.NewGuid(), 2, "Задача 2")],
            [Row(3, _alice.Id, "Андреева Алиса", ("C", "-"))]);

        var plan = GradebookImportPlanner.Plan(file, _lesson, _group, null);

        plan.LessonMismatch.ShouldBeTrue();
        plan.OtherLessonId.ShouldBe(otherLesson);
        plan.Changes.ShouldHaveSingleItem().TaskId.ShouldBe(_task2.Id);
    }

    [Test]
    public void GroupTemplateWithoutLesson_IsNotMismatch()
    {
        var file = new ParsedGradebook(new ParsedMeta(1, null, _group.Id, null),
            [new ParsedColumn("C", null, 1, "Задача 1")],
            [Row(3, _bob.Id, "Борисов Борис", ("C", "+"))]);

        var plan = GradebookImportPlanner.Plan(file, _lesson, _group, null);

        plan.LessonMismatch.ShouldBeFalse();
        plan.Changes.ShouldHaveSingleItem().TaskId.ShouldBe(_task1.Id);
    }
}
