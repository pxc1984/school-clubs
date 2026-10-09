using Microsoft.Extensions.DependencyInjection;
using SchoolClubs.Application.Services.Auth;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Infrastructure.IntegrationTests.Auth;

/// <summary>Логика входа через внешний провайдер — без самого OIDC-редиректа.</summary>
public class ExternalLoginTests : TestBase
{
    private static ExternalIdentity Identity(string subject, string? email = null, bool verified = false)
    {
        return new ExternalIdentity("yandex", subject, email, verified, "Анна", "Иванова", null);
    }

    private static Task<SessionDto> LoginAsync(ExternalIdentity identity)
    {
        return InScope(sp => sp.GetRequiredService<IAuthService>().ExternalLoginAsync(identity));
    }

    [Test]
    public async Task NewIdentity_CreatesParentRequiringConsent_ThenReusesLogin()
    {
        var first = await LoginAsync(Identity("ya-1", "anna@yandex.ru", verified: true));
        var second = await LoginAsync(Identity("ya-1"));

        first.Roles.ShouldBe([UserRole.Parent]);
        first.ConsentRequired.ShouldBeTrue();
        second.UserId.ShouldBe(first.UserId);

        var user = await InScope(sp => sp.GetRequiredService<IUserRepository>().FindByIdAsync(first.UserId!.Value));
        user!.FirstName.ShouldBe("Анна");
        user.Email.Address.ShouldBe("anna@yandex.ru");
    }

    [Test]
    public async Task VerifiedEmail_LinksPreCreatedTeacher()
    {
        var teacher = await CreateUserAsync("teacher@school218.ru", [UserRole.Teacher]);

        var session = await LoginAsync(Identity("ya-2", "Teacher@School218.ru", verified: true));

        session.UserId.ShouldBe(teacher.Id);
        session.Roles.ShouldBe([UserRole.Teacher]);
        var logins = await InScope(sp => sp.GetRequiredService<IUserLoginRepository>().ListByUserAsync(teacher.Id));
        logins.Select(l => l.Provider).ShouldBe(["password", "yandex"], ignoreOrder: true);
    }

    [Test]
    public async Task UnverifiedEmail_DoesNotLinkExistingAccount()
    {
        var teacher = await CreateUserAsync("teacher@school218.ru", [UserRole.Teacher]);

        var session = await LoginAsync(Identity("ya-3", "teacher@school218.ru", verified: false));

        session.UserId.ShouldNotBe(teacher.Id);
        session.Roles.ShouldBe([UserRole.Parent]);
    }

    [Test]
    public async Task LinkedAccountWithoutEmail_GetsVerifiedEmailOnNextLogin_UnlessTaken()
    {
        var first = await LoginAsync(Identity("ya-4", "anna@yandex.ru", verified: false));
        await LoginAsync(Identity("ya-4", "anna@yandex.ru", verified: true));
        var filled = await InScope(sp => sp.GetRequiredService<IUserRepository>().FindByIdAsync(first.UserId!.Value));
        filled!.HasEmail.ShouldBeTrue();
        filled.Email.Address.ShouldBe("anna@yandex.ru");

        await CreateUserAsync("taken@school218.ru", [UserRole.Teacher]);
        var other = await LoginAsync(Identity("ya-5"));
        await LoginAsync(Identity("ya-5", "taken@school218.ru", verified: true));
        var untouched = await InScope(sp => sp.GetRequiredService<IUserRepository>().FindByIdAsync(other.UserId!.Value));
        untouched!.HasEmail.ShouldBeFalse();
    }
}
