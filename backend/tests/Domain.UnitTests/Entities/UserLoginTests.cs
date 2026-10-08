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
        login.Provider.ShouldBe(UserLogin.PasswordProvider);
        login.IsExternal.ShouldBeFalse();
        login.ProviderKey.ShouldBe("parent@school218.ru");
        login.PasswordHash.ShouldBe("hash");
    }

    [Test]
    public void ForExternal_NormalizesProvider_KeepsSubjectCase_AndHasNoPassword()
    {
        var login = UserLogin.ForExternal(new User(), " Yandex ", " AbC123 ");

        login.Provider.ShouldBe("yandex");
        login.IsExternal.ShouldBeTrue();
        login.ProviderKey.ShouldBe("AbC123");
        login.PasswordHash.ShouldBeNull();
        Should.Throw<InvalidOperationException>(() => login.ChangePasswordHash("hash"));
    }

    [TestCase("password")]
    [TestCase("ya ndex")]
    [TestCase("-yandex")]
    public void ForExternal_WithInvalidProvider_Throws(string provider)
    {
        Should.Throw<ArgumentException>(() => UserLogin.ForExternal(new User(), provider, "sub"));
    }

    [Test]
    public void ForPassword_WithBlankHash_Throws()
    {
        Should.Throw<ArgumentException>(() => UserLogin.ForPassword(new User(), "a@b.ru", " "));
    }
}
