using NUnit.Framework;
using Shouldly;
using SchoolClubs.Domain.Common.Exceptions;
using SchoolClubs.Domain.Entities.Access;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Domain.UnitTests.Entities;

[TestFixture]
[TestOf(typeof(ChildInvite))]
public class ChildInviteTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    private static ChildInvite NewInvite()
    {
        return new ChildInvite(new Student("Пётр", lastName: "Иванов"), Now);
    }

    [Test]
    public void New_IsActive_WithRandomUrlSafeToken()
    {
        var invite = NewInvite();

        invite.IsActive(Now).ShouldBeTrue();
        invite.ExpiresAt.ShouldBe(Now + ChildInvite.Lifetime);
        invite.Token.Length.ShouldBe(ChildInvite.TokenLength);
        invite.Token.ShouldMatch("^[A-Za-z0-9_-]+$");
        NewInvite().Token.ShouldNotBe(invite.Token);
    }

    [Test]
    public void Accept_IsOneTime()
    {
        var invite = NewInvite();
        var parent = new User(roles: [UserRole.Parent]);

        invite.Accept(parent, Now);

        invite.IsActive(Now).ShouldBeFalse();
        invite.IsAcceptedBy(parent.Id).ShouldBeTrue();
        invite.AcceptedAt.ShouldBe(Now);
        Should.Throw<ChildInviteStateException>(() => invite.Accept(new User(), Now));
        Should.Throw<ChildInviteStateException>(() => invite.Revoke(Now));
    }

    [Test]
    public void Expired_OrRevoked_CantBeAccepted()
    {
        var expired = NewInvite();
        Should.Throw<ChildInviteStateException>(() => expired.Accept(new User(), Now + ChildInvite.Lifetime));

        var revoked = NewInvite();
        revoked.Revoke(Now);
        revoked.IsActive(Now).ShouldBeFalse();
        Should.Throw<ChildInviteStateException>(() => revoked.Accept(new User(), Now));
    }
}
