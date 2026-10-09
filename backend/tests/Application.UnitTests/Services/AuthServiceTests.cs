using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shouldly;
using SchoolClubs.Application.Common.Exceptions;
using SchoolClubs.Application.Common.Interfaces;
using SchoolClubs.Application.Common.Options;
using SchoolClubs.Application.Services.Auth;
using SchoolClubs.Application.Services.Avatars;
using SchoolClubs.Application.Validators;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Application.UnitTests.Services;

[TestFixture]
[TestOf(typeof(AuthService))]
public class AuthServiceTests
{
    private Mock<IUserRepository> _users = null!;
    private Mock<IUserLoginRepository> _logins = null!;
    private Mock<IPasswordHasher> _hasher = null!;
    private AuthOptions _options = null!;
    private AuthService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _users = new Mock<IUserRepository>();
        _logins = new Mock<IUserLoginRepository>();
        _hasher = new Mock<IPasswordHasher>();
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns<string>(p => "hash:" + p);
        _options = new AuthOptions { ConsentVersion = "1", ServiceAdminPassword = "secret" };
        _service = new AuthService(_users.Object,
            _logins.Object,
            Mock.Of<IUnitOfWork>(),
            _hasher.Object,
            Mock.Of<IAvatarService>(),
            Mock.Of<ICurrentUser>(),
            Options.Create(_options),
            TimeProvider.System,
            new RegisterRequestValidator(),
            new LoginRequestValidator());
    }

    [Test]
    public async Task Login_UnknownEmail_StillVerifiesHash_AndThrows()
    {
        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            _service.LoginAsync(new LoginRequest("nobody@school218.ru", "password")));

        // Время ответа не должно выдавать, что аккаунта нет.
        _hasher.Verify(h => h.Verify(It.IsAny<string>(), "password"), Times.Once);
    }

    [Test]
    public async Task Login_RehashNeeded_UpdatesHash()
    {
        var user = new User(email: "a@school218.ru", roles: [UserRole.Parent]);
        user.AcceptConsent("1", DateTimeOffset.UtcNow);
        var login = UserLogin.ForPassword(user, "a@school218.ru", "old");
        _logins.Setup(l => l.FindAsync(UserLogin.PasswordProvider, "a@school218.ru", It.IsAny<CancellationToken>()))
            .ReturnsAsync(login);
        _users.Setup(u => u.FindByIdAsync(It.IsAny<object[]>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("old", "password")).Returns(PasswordVerification.SuccessRehashNeeded);

        var session = await _service.LoginAsync(new LoginRequest("a@school218.ru", "password"));

        session.UserId.ShouldBe(user.Id);
        session.ConsentRequired.ShouldBeFalse();
        login.PasswordHash.ShouldBe("hash:password");
        login.LastUsedAt.ShouldNotBeNull();
    }

    [Test]
    public async Task Register_PreCreatedEmail_ThrowsConflict()
    {
        _users.Setup(u => u.FindByEmailAsync("t@school218.ru", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User(email: "t@school218.ru", roles: [UserRole.Teacher]));

        await Should.ThrowAsync<ConflictException>(() => _service.RegisterAsync(
            new RegisterRequest("t@school218.ru", "long-password", "Анна", "Иванова", null, null, true)));
    }

    [Test]
    public async Task Register_NameWithSpaces_ThrowsValidation()
    {
        var ex = await Should.ThrowAsync<ValidationException>(() => _service.RegisterAsync(
            new RegisterRequest("a@school218.ru", "long-password", "Анна Мария", "Иванова", null, null, true)));

        ex.Errors.ShouldContainKey(nameof(RegisterRequest.FirstName));
    }

    [TestCase(null)]
    [TestCase("")]
    public void ServiceAdmin_Disabled_Throws(string? configured)
    {
        _options.ServiceAdminPassword = configured;

        Should.Throw<UnauthorizedAccessException>(() => _service.LoginServiceAdmin(new ServiceAdminLoginRequest("")));
    }

    [Test]
    public void ServiceAdmin_ChecksPassword()
    {
        Should.Throw<UnauthorizedAccessException>(() => _service.LoginServiceAdmin(new ServiceAdminLoginRequest("secreT")));

        var session = _service.LoginServiceAdmin(new ServiceAdminLoginRequest("secret"));
        session.IsServiceAdmin.ShouldBeTrue();
        session.UserId.ShouldBeNull();
        session.Roles.ShouldBe([UserRole.Admin]);
    }
}
