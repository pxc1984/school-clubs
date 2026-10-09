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

    [Test]
    public void MoveEntries_MovesToTarget_KeepsTargetOnConflict()
    {
        var lesson = NewLesson();
        var first = lesson.AddTask();
        var second = lesson.AddTask();
        var gradebook = new LessonGradebook(lesson);
        var duplicate = new Student("Пётр");
        var original = new Student("Пётр");
        gradebook.SetEntry(duplicate, first, GradebookEntryTaskStatus.Solved);
        gradebook.SetEntry(duplicate, second, GradebookEntryTaskStatus.Solved);
        gradebook.SetEntry(original, second, GradebookEntryTaskStatus.Unsolved);

        gradebook.MoveEntries(duplicate, original).ShouldBe(1);

        gradebook.EntriesOf(duplicate.Id).ShouldBeEmpty();
        gradebook.FindEntry(original.Id, first.Id)!.Status.ShouldBe(GradebookEntryTaskStatus.Solved);
        gradebook.FindEntry(original.Id, second.Id)!.Status.ShouldBe(GradebookEntryTaskStatus.Unsolved);
        second.Entries.ShouldHaveSingleItem();
    }

    [Test]
    public void MoveEntries_MovesAttendance_KeepsTargetOnConflict()
    {
        var gradebook = new LessonGradebook(NewLesson());
        var duplicate = new Student("Пётр");
        var original = new Student("Пётр");
        var otherDuplicate = new Student("Анна");
        var otherOriginal = new Student("Анна");
        gradebook.SetAttendance(duplicate, AttendanceStatus.Late);
        gradebook.SetAttendance(otherDuplicate, AttendanceStatus.Late);
        gradebook.SetAttendance(otherOriginal, AttendanceStatus.Absent);

        gradebook.MoveEntries(duplicate, original);
        gradebook.MoveEntries(otherDuplicate, otherOriginal);

        gradebook.FindAttendance(duplicate.Id).ShouldBeNull();
        gradebook.FindAttendance(original.Id)!.Status.ShouldBe(AttendanceStatus.Late);
        gradebook.FindAttendance(otherOriginal.Id)!.Status.ShouldBe(AttendanceStatus.Absent);
        gradebook.Attendance.Count.ShouldBe(2);
    }

    [Test]
    public void MoveEntries_ToSameStudent_Throws()
    {
        var gradebook = new LessonGradebook(NewLesson());
        var student = new Student("Пётр");

        Should.Throw<ArgumentException>(() => gradebook.MoveEntries(student, student));
    }

    [Test]
    public void SetEntry_StoresAndOverwritesComment()
    {
        var lesson = NewLesson();
        var task = lesson.AddTask("Рекурсия");
        var gradebook = new LessonGradebook(lesson);
        var student = new Student("Пётр");

        gradebook.SetEntry(student, task, GradebookEntryTaskStatus.SolvedWithRemarks, "  забыл базу рекурсии ");
        gradebook.FindEntry(student.Id, task.Id)!.Comment.ShouldBe("забыл базу рекурсии");

        gradebook.SetEntry(student, task, GradebookEntryTaskStatus.Solved);
        gradebook.FindEntry(student.Id, task.Id)!.Comment.ShouldBeNull();
        task.Title.ShouldBe("Рекурсия");
    }

    [Test]
    public void SetEntry_TooLongComment_Throws()
    {
        var lesson = NewLesson();
        var task = lesson.AddTask();
        var gradebook = new LessonGradebook(lesson);

        Should.Throw<ArgumentException>(() => gradebook.SetEntry(new Student("Пётр"), task,
            GradebookEntryTaskStatus.Solved, new string('a', GradebookEntry.CommentMaxLength + 1)));
    }

    [Test]
    public void Publish_KeepsFirstDate_UnpublishClears()
    {
        var gradebook = new LessonGradebook(NewLesson());
        var first = new DateTimeOffset(2026, 10, 8, 18, 0, 0, TimeSpan.Zero);

        gradebook.IsPublished.ShouldBeFalse();
        gradebook.Publish(first);
        gradebook.Publish(first.AddHours(1));
        gradebook.PublishedAt.ShouldBe(first);

        gradebook.Unpublish();
        gradebook.IsPublished.ShouldBeFalse();
    }

    [Test]
    public void MoveEntries_CarriesComment()
    {
        var lesson = NewLesson();
        var task = lesson.AddTask();
        var gradebook = new LessonGradebook(lesson);
        var duplicate = new Student("Пётр");
        var original = new Student("Пётр");
        gradebook.SetEntry(duplicate, task, GradebookEntryTaskStatus.UnsolvedWithRemarks, "почти");

        gradebook.MoveEntries(duplicate, original);

        gradebook.FindEntry(original.Id, task.Id)!.Comment.ShouldBe("почти");
    }

    [Test]
    public void SetAttendance_Twice_UpdatesAndKeepsEntries()
    {
        var lesson = NewLesson();
        var task = lesson.AddTask();
        var student = new Student("Пётр");
        var gradebook = new LessonGradebook(lesson);
        gradebook.SetEntry(student, task, GradebookEntryTaskStatus.Solved);

        gradebook.SetAttendance(student, AttendanceStatus.Absent);
        var attendance = gradebook.SetAttendance(student, AttendanceStatus.Late);

        gradebook.Attendance.ShouldHaveSingleItem().ShouldBeSameAs(attendance);
        attendance.Status.ShouldBe(AttendanceStatus.Late);
        attendance.LessonId.ShouldBe(lesson.Id);
        gradebook.GradebookEntries.ShouldHaveSingleItem();
    }

    [Test]
    public void RemoveAttendance_ReturnsWhetherRemoved()
    {
        var student = new Student("Пётр");
        var gradebook = new LessonGradebook(NewLesson());
        gradebook.SetAttendance(student, AttendanceStatus.Absent);

        gradebook.RemoveAttendance(student.Id).ShouldBeTrue();
        gradebook.RemoveAttendance(student.Id).ShouldBeFalse();
        gradebook.Attendance.ShouldBeEmpty();
    }

    [Test]
    public void SetAttendance_WithUnknownStatus_Throws()
    {
        var gradebook = new LessonGradebook(NewLesson());

        Should.Throw<ArgumentOutOfRangeException>(
            () => gradebook.SetAttendance(new Student("Пётр"), (AttendanceStatus)42));
    }
}
