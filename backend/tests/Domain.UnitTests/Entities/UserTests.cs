using NUnit.Framework;
using Shouldly;
using SchoolClubs.Domain.Common.Exceptions;
using SchoolClubs.Domain.Entities.Gradebook;
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
    public void New_WithRoles_CombinesThem()
    {
        var user = new User(roles: [UserRole.Parent, UserRole.Teacher, UserRole.Admin]);

        user.Roles.ShouldBe([UserRole.Parent, UserRole.Teacher, UserRole.Admin], ignoreOrder: true);
    }

    [Test]
    public void New_WithDuplicateRoles_Throws()
    {
        Should.Throw<UniqueListElementsException>(() => new User(roles: [UserRole.Parent, UserRole.Parent]));
    }

    [Test]
    public void GrantRole_Twice_IsNoOp()
    {
        var user = new User();

        user.GrantRole(UserRole.Teacher).ShouldBeTrue();
        user.GrantRole(UserRole.Teacher).ShouldBeFalse();

        user.Roles.ShouldHaveSingleItem().ShouldBe(UserRole.Teacher);
    }

    [Test]
    public void GrantAndRevokeRole_RotateSecurityStamp()
    {
        var user = new User();
        var initial = user.SecurityStamp;

        user.GrantRole(UserRole.Admin);
        var afterGrant = user.SecurityStamp;
        user.RevokeRole(UserRole.Admin).ShouldBeTrue();

        afterGrant.ShouldNotBe(initial);
        user.SecurityStamp.ShouldNotBe(afterGrant);
        user.HasRole(UserRole.Admin).ShouldBeFalse();
    }

    [Test]
    public void GrantRole_Existing_KeepsSecurityStamp()
    {
        var user = new User(roles: [UserRole.Parent]);
        var stamp = user.SecurityStamp;

        user.GrantRole(UserRole.Parent);

        user.SecurityStamp.ShouldBe(stamp);
    }

    [Test]
    public void RevokeRole_Missing_ReturnsFalse()
    {
        new User().RevokeRole(UserRole.Admin).ShouldBeFalse();
    }

    [Test]
    public void RevokeTeacher_WithLinkedGroups_Throws()
    {
        var user = new User(roles: [UserRole.Teacher]);
        new Group("Робототехника").AddTeacher(user);

        Should.Throw<UserRoleException>(() => user.RevokeRole(UserRole.Teacher));

        user.HasRole(UserRole.Teacher).ShouldBeTrue();
    }

    [Test]
    public void RevokeParent_WithChildren_Throws()
    {
        var user = new User(roles: [UserRole.Parent]);
        user.AddChild(new Student("Пётр"));

        Should.Throw<UserRoleException>(() => user.RevokeRole(UserRole.Parent));
    }

    [Test]
    public void AddChild_WithoutParentRole_Throws()
    {
        var user = new User(roles: [UserRole.Teacher]);

        Should.Throw<UserRoleException>(() => user.AddChild(new Student("Пётр")));
    }

    [Test]
    public void AddChild_Twice_IsNoOp()
    {
        var child = new Student("Пётр");
        var parent = new User(roles: [UserRole.Parent]);
        parent.AddChild(child);

        parent.AddChild(child).ShouldBeFalse();

        parent.Children.ShouldHaveSingleItem();
    }

    [Test]
    public void RemoveChild_ThenRevokeParent_Succeeds()
    {
        var child = new Student("Пётр");
        var parent = new User(roles: [UserRole.Parent]);
        parent.AddChild(child);

        parent.RemoveChild(child).ShouldBeTrue();

        parent.RevokeRole(UserRole.Parent).ShouldBeTrue();
    }

    [Test]
    public void Block_RotatesSecurityStampOnce()
    {
        var user = new User();
        var initial = user.SecurityStamp;

        user.Block();
        var afterBlock = user.SecurityStamp;
        user.Block();

        user.IsBlocked.ShouldBeTrue();
        afterBlock.ShouldNotBe(initial);
        user.SecurityStamp.ShouldBe(afterBlock);

        user.Unblock();
        user.IsBlocked.ShouldBeFalse();
        user.SecurityStamp.ShouldNotBe(afterBlock);
    }

    [Test]
    public void AcceptConsent_IsCheckedAgainstCurrentVersion()
    {
        var user = new User();
        var acceptedAt = new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

        user.HasAcceptedConsent("2026-10").ShouldBeFalse();
        user.AcceptConsent("2026-10", acceptedAt, "10.0.0.1");

        user.HasAcceptedConsent("2026-10").ShouldBeTrue();
        user.HasAcceptedConsent("2027-01").ShouldBeFalse();
        user.ConsentAcceptedAt.ShouldBe(acceptedAt);
        user.ConsentIp.ShouldBe("10.0.0.1");
    }
}
