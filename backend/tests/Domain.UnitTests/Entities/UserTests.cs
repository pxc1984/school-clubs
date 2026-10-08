using NUnit.Framework;
using Shouldly;
using SchoolClubs.Domain.Common.Exceptions;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Domain.UnitTests.Entities;

[TestFixture]
[TestOf(typeof(User))]
public class UserTests
{
    [Test]
    public void New_WithInvalidEmail_Throws()
    {
        Should.Throw<ArgumentException>(() => new User(email: "not-an-email"));
    }

    [Test]
    public void Rename_WithMultiPartName_Throws()
    {
        var user = new User("Иван");

        Should.Throw<NamePartsException>(() => user.Rename("Иван Петрович"));
    }

    [Test]
    public void ChangeEmail_UpdatesEmail()
    {
        var user = new User();

        user.ChangeEmail("teacher@school218.ru");

        user.Email.Address.ShouldBe("teacher@school218.ru");
    }

    [Test]
    public void Parent_AddChild_Twice_IsNoOp()
    {
        var child = new Student("Пётр");
        var parent = new Parent(children: [child]);

        parent.AddChild(child).ShouldBeFalse();

        parent.Children.ShouldHaveSingleItem();
    }

    [Test]
    public void Parent_WithDuplicateChildren_Throws()
    {
        var child = new Student("Пётр");

        Should.Throw<UniqueListElementsException>(() => new Parent(children: [child, child]));
    }
}
