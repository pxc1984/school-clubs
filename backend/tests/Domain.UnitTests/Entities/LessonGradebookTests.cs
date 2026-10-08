using NUnit.Framework;
using Shouldly;
using SchoolClubs.Domain.Entities.Gradebook;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Domain.UnitTests.Entities;

[TestFixture]
[TestOf(typeof(LessonGradebook))]
public class LessonGradebookTests
{
    private static Lesson NewLesson()
    {
        return new Lesson(new Group("Шахматы"), new DateOnly(2026, 10, 8), null);
    }

    [Test]
    public void SetEntry_Twice_UpdatesExistingEntry()
    {
        var lesson = NewLesson();
        var task = lesson.AddTask();
        var student = new Student("Пётр");
        var gradebook = new LessonGradebook(lesson);

        gradebook.SetEntry(student, task, GradebookEntryTaskStatus.Unsolved);
        var entry = gradebook.SetEntry(student, task, GradebookEntryTaskStatus.Solved);

        gradebook.GradebookEntries.ShouldHaveSingleItem().ShouldBeSameAs(entry);
        entry.Status.ShouldBe(GradebookEntryTaskStatus.Solved);
        entry.IsSolved.ShouldBeTrue();
        task.Entries.ShouldHaveSingleItem();
    }

    [Test]
    public void SetEntry_WithTaskOfAnotherLesson_Throws()
    {
        var gradebook = new LessonGradebook(NewLesson());
        var foreignTask = NewLesson().AddTask();

        Should.Throw<ArgumentException>(
            () => gradebook.SetEntry(new Student("Пётр"), foreignTask, GradebookEntryTaskStatus.Solved));
    }

    [Test]
    public void SetEntry_WithUnknownStatus_Throws()
    {
        var lesson = NewLesson();
        var gradebook = new LessonGradebook(lesson);

        Should.Throw<ArgumentOutOfRangeException>(
            () => gradebook.SetEntry(new Student("Пётр"), lesson.AddTask(), (GradebookEntryTaskStatus)42));
    }

    [Test]
    public void RemoveEntry_DetachesFromTask()
    {
        var lesson = NewLesson();
        var task = lesson.AddTask();
        var student = new Student("Пётр");
        var gradebook = new LessonGradebook(lesson);
        gradebook.SetEntry(student, task, GradebookEntryTaskStatus.Solved);

        gradebook.RemoveEntry(student.Id, task.Id).ShouldBeTrue();

        gradebook.GradebookEntries.ShouldBeEmpty();
        task.Entries.ShouldBeEmpty();
        gradebook.RemoveEntry(student.Id, task.Id).ShouldBeFalse();
    }
}
