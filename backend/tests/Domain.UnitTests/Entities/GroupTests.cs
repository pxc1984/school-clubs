using NUnit.Framework;
using Shouldly;
using SchoolClubs.Domain.Entities.Gradebook;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Domain.UnitTests.Entities;

[TestFixture]
[TestOf(typeof(Group))]
public class GroupTests
{
    [Test]
    public void New_WithBlankTitle_Throws()
    {
        Should.Throw<ArgumentException>(() => new Group("  "));
    }

    [Test]
    public void AddTeacher_LinksBothSides()
    {
        var group = new Group("Робототехника");
        var teacher = new Teacher("Иван");

        group.AddTeacher(teacher).ShouldBeTrue();

        group.Teachers.ShouldContain(teacher);
        teacher.LinkedGroups.ShouldContain(group);
    }

    [Test]
    public void AddTeacher_Twice_IsNoOp()
    {
        var group = new Group("Робототехника");
        var teacher = new Teacher("Иван");
        group.AddTeacher(teacher);

        group.AddTeacher(teacher).ShouldBeFalse();

        group.Teachers.Count.ShouldBe(1);
        teacher.LinkedGroups.Count.ShouldBe(1);
    }

    [Test]
    public void RemoveTeacher_UnlinksBothSides()
    {
        var group = new Group("Робототехника");
        var teacher = new Teacher("Иван", linkedGroups: [group]);

        group.RemoveTeacher(teacher).ShouldBeTrue();

        group.Teachers.ShouldBeEmpty();
        teacher.LinkedGroups.ShouldBeEmpty();
    }

    [Test]
    public void AddStudent_Twice_IsNoOp()
    {
        var group = new Group("Робототехника");
        var student = new Student("Пётр");

        group.AddStudent(student).ShouldBeTrue();
        group.AddStudent(student).ShouldBeFalse();

        group.Students.Count.ShouldBe(1);
        group.HasStudent(student.Id).ShouldBeTrue();
    }
}
