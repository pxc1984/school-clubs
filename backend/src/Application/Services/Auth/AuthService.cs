using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Microsoft.Extensions.Options;
using SchoolClubs.Application.Common.Exceptions;
using SchoolClubs.Application.Common.Interfaces;
using SchoolClubs.Application.Common.Options;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Users;
using ValidationException = SchoolClubs.Application.Common.Exceptions.ValidationException;

namespace SchoolClubs.Application.Services.Auth;

public class AuthService(
    IUserRepository users,
    IUserLoginRepository logins,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser,
    IOptions<AuthOptions> options,
    TimeProvider timeProvider,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator) : IAuthService
{
    // Хеш для несуществующего email: проверка занимает то же время, и по задержке нельзя узнать, есть ли аккаунт.
    private static string? _dummyHash;

    public async Task<SessionDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(registerValidator, request, cancellationToken);

        // Любой существующий аккаунт с этим email — конфликт: в том числе заведённый админом учитель без логина,
        // иначе его аккаунт мог бы занять любой, кто знает адрес (почту мы пока не подтверждаем).
        if (await users.FindByEmailAsync(request.Email, cancellationToken) is not null
            || await logins.FindAsync(UserLogin.PasswordProvider, request.Email, cancellationToken) is not null)
            throw new ConflictException("email is already registered");

        var user = new User(request.FirstName.Trim(),
            NullIfBlank(request.MiddleName),
            request.LastName.Trim(),
            request.Email.Trim(),
            NullIfBlank(request.Phone),
            [UserRole.Parent]);
        user.AcceptConsent(options.Value.ConsentVersion, timeProvider.GetUtcNow(), currentUser.IpAddress);

        var login = UserLogin.ForPassword(user, request.Email, passwordHasher.Hash(request.Password));
        login.MarkUsed(timeProvider.GetUtcNow());

        await users.AddAsync(user, cancellationToken);
        await logins.AddAsync(login, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToSession(user);
    }

    public async Task<SessionDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(loginValidator, request, cancellationToken);

        var login = await logins.FindAsync(UserLogin.PasswordProvider, request.Email, cancellationToken);
        if (login?.PasswordHash is null)
        {
            _dummyHash ??= passwordHasher.Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)));
            passwordHasher.Verify(_dummyHash, request.Password);
            throw new UnauthorizedAccessException();
        }

        var verification = passwordHasher.Verify(login.PasswordHash, request.Password);
        if (verification == PasswordVerification.Failed)
            throw new UnauthorizedAccessException();
        if (verification == PasswordVerification.SuccessRehashNeeded)
            login.ChangePasswordHash(passwordHasher.Hash(request.Password));

        var user = await users.FindByIdAsync(login.UserId) ?? throw new UnauthorizedAccessException();
        return await CompleteLoginAsync(user, login, cancellationToken);
    }

    public async Task<SessionDto> ExternalLoginAsync(ExternalIdentity identity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(identity.Subject);

        var login = await logins.FindAsync(identity.Provider, identity.Subject, cancellationToken);
        if (login is not null)
        {
            var linked = await users.FindByIdAsync(login.UserId) ?? throw new UnauthorizedAccessException();
            await FillMissingEmailAsync(linked, identity, cancellationToken);
            return await CompleteLoginAsync(linked, login, cancellationToken);
        }

        var email = TrustedEmail(identity);
        var user = email is null ? null : await users.FindByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            // Согласие на ПДн не принято — до его принятия пользователь попадает только на /consent.
            user = email is null
                ? new User(NameOrNull(identity.FirstName), NameOrNull(identity.MiddleName),
                    NameOrNull(identity.LastName), roles: [UserRole.Parent])
                : new User(NameOrNull(identity.FirstName), NameOrNull(identity.MiddleName),
                    NameOrNull(identity.LastName), email, roles: [UserRole.Parent]);
            await users.AddAsync(user, cancellationToken);
        }

        login = UserLogin.ForExternal(user, identity.Provider, identity.Subject);
        await logins.AddAsync(login, cancellationToken);
        return await CompleteLoginAsync(user, login, cancellationToken);
    }

    public SessionDto LoginServiceAdmin(ServiceAdminLoginRequest request)
    {
        var expected = options.Value.ServiceAdminPassword;
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(request.Password))
            throw new UnauthorizedAccessException();

        // Сравниваем хеши одинаковой длины, чтобы FixedTimeEquals не выдавал длину пароля.
        var actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(request.Password));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        if (!CryptographicOperations.FixedTimeEquals(actualHash, expectedHash))
            throw new UnauthorizedAccessException();

        return new SessionDto(null, [UserRole.Admin], null, false, true);
    }

    public async Task<SessionDto?> RefreshSessionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null || user.IsBlocked)
            return null;

        return ToSession(user);
    }

    public async Task<MeDto> GetMeAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.IsServiceAdmin)
            return new MeDto(null, null, null, null, null, [UserRole.Admin], false, true);
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException();

        var user = await users.FindByIdAsync(userId) ?? throw new UnauthorizedAccessException();
        return new MeDto(user.Id, user.FirstName, user.MiddleName, user.LastName, user.Email.Address,
            user.Roles.Order().ToList(), IsConsentRequired(user), false);
    }

    /// <summary>Неподтверждённому email не верим: иначе через провайдер с чужим адресом можно войти в чужой аккаунт.</summary>
    private static string? TrustedEmail(ExternalIdentity identity)
    {
        return identity.EmailVerified && IsValidEmail(identity.Email) ? identity.Email!.Trim() : null;
    }

    /// <summary>
    /// Аккаунт без почты получает подтверждённый адрес провайдера при следующем входе — если адрес не занят
    /// другим аккаунтом (чужие аккаунты по-тихому не сливаем).
    /// </summary>
    private async Task FillMissingEmailAsync(User user, ExternalIdentity identity, CancellationToken cancellationToken)
    {
        if (user.HasEmail || TrustedEmail(identity) is not { } email)
            return;
        if (await users.FindByEmailAsync(email, cancellationToken) is not null)
            return;

        user.ChangeEmail(email);
    }

    private async Task<SessionDto> CompleteLoginAsync(User user, UserLogin login, CancellationToken cancellationToken)
    {
        if (user.IsBlocked)
            throw new ForbiddenAccessException(ForbiddenAccessException.Blocked);

        login.MarkUsed(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToSession(user);
    }

    private SessionDto ToSession(User user)
    {
        return new SessionDto(user.Id, user.Roles.Order().ToList(), user.SecurityStamp, IsConsentRequired(user),
            false);
    }

    private bool IsConsentRequired(User user)
    {
        return !user.HasAcceptedConsent(options.Value.ConsentVersion);
    }

    private static bool IsValidEmail(string? email)
    {
        return !string.IsNullOrWhiteSpace(email) && System.Net.Mail.MailAddress.TryCreate(email.Trim(), out _);
    }

    /// <summary>Имя от провайдера берём, только если оно годится для <see cref="Profile.Rename"/>.</summary>
    private static string? NameOrNull(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var trimmed = name.Trim();
        return trimmed.Length > 100 || trimmed.Any(char.IsWhiteSpace) ? null : trimmed;
    }

    private static string? NullIfBlank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid) throw new ValidationException(result.Errors);
    }
}
