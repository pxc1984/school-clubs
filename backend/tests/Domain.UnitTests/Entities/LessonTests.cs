using NUnit.Framework;
using Shouldly;
using SchoolClubs.Domain.Common.Exceptions;
using SchoolClubs.Domain.Entities.Gradebook;
using Task = SchoolClubs.Domain.Entities.Gradebook.Task;

namespace SchoolClubs.Domain.UnitTests.Entities;

[TestFixture]
[TestOf(typeof(Lesson))]
public class LessonTests
{
    private static readonly DateOnly Date = new(2026, 10, 8);

    [TestCase(6, 59)]
    [TestCase(22, 1)]
    public void New_WithTimeOutOfRange_Throws(int hour, int minute)
    {
        Should.Throw<ArgumentException>(() => new Lesson(new Group("Шахматы"), Date, new TimeOnly(hour, minute)));
    }

    [Test]
    public void New_WithoutTime_IsAllowed()
    {
        new Lesson(new Group("Шахматы"), Date, null).Time.ShouldBeNull();
    }

    [Test]
    public void AddTask_AssignsNextNumber()
    {
        var lesson = new Lesson(new Group("Шахматы"), Date, null);

        var first = lesson.AddTask();
        var second = lesson.AddTask();

        first.Number.ShouldBe("1");
        second.Number.ShouldBe("2");
        second.Order.ShouldBe(2);
        second.LessonId.ShouldBe(lesson.Id);
    }

    [Test]
    public void AddTask_AfterSubtasks_TakesNextWholeNumber()
    {
        var lesson = new Lesson(new Group("Шахматы"), Date, null);
        lesson.AddTask(number: "1.1");
        lesson.AddTask(number: "1.2");

        lesson.AddTask().Number.ShouldBe("2");
    }

    [Test]
    public void AddTask_DuplicateOrInvalidNumber_Throws()
    {
        var lesson = new Lesson(new Group("Шахматы"), Date, null);
        lesson.AddTask(number: "2а");

        Should.Throw<DuplicateTaskNumberException>(() => lesson.AddTask(number: " 2А "));
        Should.Throw<ArgumentException>(() => lesson.AddTask(number: "1 2"));
        Should.Throw<ArgumentException>(() => lesson.AddTask(number: "1:2"));
        Should.Throw<ArgumentException>(() => lesson.AddTask(number: new string('1', TaskNumber.MaxLength + 1)));
        lesson.Tasks.Count.ShouldBe(1);
    }

    [Test]
    public void RenumberTask_ReordersByNaturalNumberOrder()
    {
        var lesson = new Lesson(new Group("Шахматы"), Date, null);
        var first = lesson.AddTask();
        var second = lesson.AddTask();
        var third = lesson.AddTask();

        lesson.RenumberTask(third, "1.1");
        lesson.RenumberTask(second, "10");

        lesson.Tasks.OrderBy(t => t.Order).Select(t => t.Number).ShouldBe(["1", "1.1", "10"]);
        (first.Order, third.Order, second.Order).ShouldBe((1, 2, 3));
        Should.Throw<DuplicateTaskNumberException>(() => lesson.RenumberTask(second, "1"));
        lesson.RenumberTask(second, "10");
    }

    [Test]
    public void RemoveTask_KeepsNumbers_CompactsOrders()
    {
        var lesson = new Lesson(new Group("Шахматы"), Date, null);
        var first = lesson.AddTask();
        var second = lesson.AddTask();
        var third = lesson.AddTask();

        lesson.RemoveTask(second).ShouldBeTrue();

        lesson.Tasks.ShouldBe([first, third]);
        third.Number.ShouldBe("3");
        third.Order.ShouldBe(2);
    }

    [Test]
    public void Topic_IsTrimmed_BlankBecomesNull_TooLongThrows()
    {
        var lesson = new Lesson(new Group("Шахматы"), Date, null, "  Дебюты ");
        lesson.Topic.ShouldBe("Дебюты");

        lesson.ChangeTopic("   ");
        lesson.Topic.ShouldBeNull();

        Should.Throw<ArgumentException>(() => lesson.ChangeTopic(new string('a', Lesson.TopicMaxLength + 1)));
    }

    [Test]
    public void Task_Rename()
    {
        var task = new Lesson(new Group("Шахматы"), Date, null).AddTask();

        task.Rename(" Вилка ");
        task.Title.ShouldBe("Вилка");

        Should.Throw<ArgumentException>(() => task.Rename(new string('a', Domain.Entities.Gradebook.Task.TitleMaxLength + 1)));
    }
}
