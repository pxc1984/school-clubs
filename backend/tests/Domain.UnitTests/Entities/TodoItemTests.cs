using NUnit.Framework;
using Shouldly;
using SchoolClubs.Domain.Entities;

namespace SchoolClubs.Domain.UnitTests.Entities;

[TestFixture]
[TestOf(typeof(TodoItem))]
public class TodoItemTests
{
    [Test]
    public void New_IsNotDone()
    {
        new TodoItem().IsDone.ShouldBeFalse();
    }

    [Test]
    public void Toggle_FlipsIsDone()
    {
        var item = new TodoItem();

        item.Toggle();

        item.IsDone.ShouldBeTrue();
    }
}
