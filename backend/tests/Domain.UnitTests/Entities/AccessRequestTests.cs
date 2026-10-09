using NUnit.Framework;
using Shouldly;
using SchoolClubs.Domain.Common.Exceptions;
using SchoolClubs.Domain.Entities.Access;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Domain.UnitTests.Entities;

[TestFixture]
[TestOf(typeof(AccessRequest))]
public class AccessRequestTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

    private static AccessRequest NewRequest()
    {
        return new AccessRequest(new User(roles: [UserRole.Parent]), Guid.NewGuid(), " Иванов ", "Пётр", null,
            "7 б", ParentRelation.Mother, "  ", Now);
    }

    [Test]
    public void New_IsPending_Normalized()
    {
        var request = NewRequest();

        request.Status.ShouldBe(AccessRequestStatus.Pending);
        request.ChildLastName.ShouldBe("Иванов");
        request.Grade.ShouldBe("7Б");
        request.Comment.ShouldBeNull();
        request.ConsentAcceptedAt.ShouldBe(Now);
    }

    [Test]
    public void Approve_StoresStudent_ThenFinal()
    {
        var request = NewRequest();
        var student = new Student("Пётр", lastName: "Иванов");

        request.Approve(student, Now);

        request.Status.ShouldBe(AccessRequestStatus.Approved);
        request.StudentId.ShouldBe(student.Id);
        request.DecidedAt.ShouldBe(Now);
        Should.Throw<AccessRequestStateException>(() => request.Reject("поздно", Now));
        Should.Throw<AccessRequestStateException>(() => request.Cancel(Now));
    }

    [Test]
    public void Reject_RequiresReason()
    {
        var request = NewRequest();

        Should.Throw<ArgumentException>(() => request.Reject(" ", Now));
        request.Reject(" Нет такого ученика ", Now);

        request.RejectReason.ShouldBe("Нет такого ученика");
        Should.Throw<AccessRequestStateException>(() => request.Approve(new Student("Пётр"), Now));
    }

    [Test]
    public void Cancel_IsFinal()
    {
        var request = NewRequest();

        request.Cancel(Now);

        request.Status.ShouldBe(AccessRequestStatus.Cancelled);
        Should.Throw<AccessRequestStateException>(() => request.Cancel(Now));
    }

    [Test]
    public void IsSameChild_IgnoresCaseAndYo()
    {
        var request = new AccessRequest(new User(), Guid.Parse("11111111-1111-1111-1111-111111111111"), "Ёлкин",
            "Семён", null, null, ParentRelation.Father, null, Now);

        request.IsSameChild(Guid.Parse("11111111-1111-1111-1111-111111111111"), "елкин", " СЕМЕН ").ShouldBeTrue();
        request.IsSameChild(Guid.NewGuid(), "Ёлкин", "Семён").ShouldBeFalse();
    }
}
