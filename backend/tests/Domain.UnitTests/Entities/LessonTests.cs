using NUnit.Framework;
using Shouldly;
using SchoolClubs.Domain.Entities.Gradebook;

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
    public void AddTask_AssignsSequentialOrder()
    {
        var lesson = new Lesson(new Group("Шахматы"), Date, null);

        var first = lesson.AddTask();
        var second = lesson.AddTask();

        first.Order.ShouldBe(1);
        second.Order.ShouldBe(2);
        second.LessonId.ShouldBe(lesson.Id);
    }

    [Test]
    public void RemoveTask_ShiftsFollowingOrders()
    {
        var lesson = new Lesson(new Group("Шахматы"), Date, null);
        var first = lesson.AddTask();
        var second = lesson.AddTask();
        var third = lesson.AddTask();

        lesson.RemoveTask(second).ShouldBeTrue();

        lesson.Tasks.ShouldBe([first, third]);
        third.Order.ShouldBe(2);
    }
}
