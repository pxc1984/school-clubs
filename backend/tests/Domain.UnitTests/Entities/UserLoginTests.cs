using NUnit.Framework;
using Shouldly;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Domain.UnitTests.Entities;

[TestFixture]
[TestOf(typeof(UserLogin))]
public class UserLoginTests
{
    [Test]
    public void ForPassword_NormalizesEmail()
    {
        var user = new User();

        var login = UserLogin.ForPassword(user, "  Parent@School218.RU ", "hash");

        login.UserId.ShouldBe(user.Id);
        login.Provider.ShouldBe(LoginProvider.Password);
        login.ProviderKey.ShouldBe("parent@school218.ru");
        login.PasswordHash.ShouldBe("hash");
    }

    [Test]
    public void ForYandex_KeepsIdCase_AndHasNoPassword()
    {
        var login = UserLogin.ForYandex(new User(), " AbC123 ");

        login.ProviderKey.ShouldBe("AbC123");
        login.PasswordHash.ShouldBeNull();
        Should.Throw<InvalidOperationException>(() => login.ChangePasswordHash("hash"));
    }

    [Test]
    public void ForPassword_WithBlankHash_Throws()
    {
        Should.Throw<ArgumentException>(() => UserLogin.ForPassword(new User(), "a@b.ru", " "));
    }
}
