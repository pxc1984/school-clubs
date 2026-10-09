using Ardalis.GuardClauses;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Options;
using SchoolClubs.Application.Common.Exceptions;
using SchoolClubs.Application.Common.Interfaces;
using SchoolClubs.Application.Common.Options;
using SchoolClubs.Application.Services.Auth;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Users;
using ValidationException = SchoolClubs.Application.Common.Exceptions.ValidationException;

namespace SchoolClubs.Application.Services.Account;

public class AccountService(
    IUserRepository users,
    IUserLoginRepository logins,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser,
    IAuthService authService,
    IOptions<AuthOptions> options,
    TimeProvider timeProvider,
    IValidator<AcceptConsentRequest> consentValidator,
    IValidator<UpdateAccountRequest> updateValidator,
    IValidator<ChangePasswordRequest> passwordValidator,
    IValidator<ChangeUsernameRequest> usernameValidator) : IAccountService
{
    public async Task<AccountDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync();
        var providers = (await logins.ListByUserAsync(user.Id, cancellationToken)).Select(l => l.Provider).ToList();

        return new AccountDto(user.Id, user.FirstName, user.MiddleName, user.LastName, user.Email.Address,
            user.Username, user.Phone, user.Roles.Order().ToList(), providers);
    }

    public async Task<AccountDto> UpdateAsync(UpdateAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(updateValidator, request, cancellationToken);

        var user = await GetCurrentUserAsync();
        user.Rename(request.FirstName.Trim(), NullIfBlank(request.MiddleName), request.LastName.Trim());
        user.ChangePhone(NullIfBlank(request.Phone));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetAsync(cancellationToken);
    }

    public async Task<SessionDto> ChangePasswordAsync(ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(passwordValidator, request, cancellationToken);

        var user = await GetCurrentUserAsync();
        var login = (await logins.ListByUserAsync(user.Id, cancellationToken))
            .FirstOrDefault(l => l.Provider == UserLogin.PasswordProvider);
        var hash = passwordHasher.Hash(request.NewPassword);

        if (login is not null)
        {
            if (string.IsNullOrEmpty(request.CurrentPassword)
                || login.PasswordHash is null
                || passwordHasher.Verify(login.PasswordHash, request.CurrentPassword) == PasswordVerification.Failed)
                throw new ValidationException([
                    new ValidationFailure(nameof(ChangePasswordRequest.CurrentPassword), "Неверный текущий пароль."),
                ]);

            login.ChangePasswordHash(hash);
        }
        else
        {
            // Пароль привязан к email: без почты по нему нечем будет войти.
            if (!user.HasEmail)
                throw new ConflictException("Чтобы задать пароль, в профиле нужен email.");
            if (await logins.FindAsync(UserLogin.PasswordProvider, user.Email.Address, cancellationToken) is not null)
                throw new ConflictException("Этот email уже используется для входа другим аккаунтом.");

            await logins.AddAsync(UserLogin.ForPassword(user, user.Email.Address, hash), cancellationToken);
        }

        // Остальные сессии завершаются; текущую Web перевыпускает с новым штампом.
        user.RotateSecurityStamp();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await authService.RefreshSessionAsync(user.Id, cancellationToken)
               ?? throw new UnauthorizedAccessException();
    }

    public async Task UnlinkLoginAsync(string provider, CancellationToken cancellationToken = default)
    {
        var normalized = UserLogin.NormalizeProvider(provider);
        if (normalized == UserLogin.PasswordProvider)
            throw new ArgumentException("password login can't be unlinked", nameof(provider));

        var user = await GetCurrentUserAsync();
        var userLogins = await logins.ListByUserAsync(user.Id, cancellationToken);
        var login = userLogins.FirstOrDefault(l => l.Provider == normalized);
        Guard.Against.NotFound(normalized, login);
        if (userLogins.Count == 1)
            throw new ConflictException("Это единственный способ входа — сначала задайте пароль.");

        logins.Remove(login);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<AccountDto> ChangeUsernameAsync(ChangeUsernameRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(usernameValidator, request, cancellationToken);

        var user = await GetCurrentUserAsync();
        if (User.NormalizeUsername(request.Username) is { } username
            && await users.FindByUsernameAsync(username, cancellationToken) is { } owner
            && owner.Id != user.Id)
            throw new ConflictException("username is already taken");

        user.ChangeUsername(request.Username);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetAsync(cancellationToken);
    }

    public async Task<SessionDto> AcceptConsentAsync(AcceptConsentRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(consentValidator, request, cancellationToken);

        var user = await GetCurrentUserAsync();
        user.AcceptConsent(options.Value.ConsentVersion, timeProvider.GetUtcNow(), currentUser.IpAddress);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await authService.RefreshSessionAsync(user.Id, cancellationToken)
               ?? throw new UnauthorizedAccessException();
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

    /// <exception cref="UnauthorizedAccessException">Нет аккаунта (аноним или сервисный админ).</exception>
    private async Task<User> GetCurrentUserAsync()
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException();

        return await users.FindByIdAsync(userId) ?? throw new UnauthorizedAccessException();
    }
}
