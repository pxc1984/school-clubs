using FluentValidation;
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
    ICurrentUser currentUser,
    IAuthService authService,
    IOptions<AuthOptions> options,
    TimeProvider timeProvider,
    IValidator<AcceptConsentRequest> consentValidator,
    IValidator<ChangeUsernameRequest> usernameValidator) : IAccountService
{
    public async Task<AccountDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync();
        var providers = (await logins.ListByUserAsync(user.Id, cancellationToken)).Select(l => l.Provider).ToList();

        return new AccountDto(user.Id, user.FirstName, user.MiddleName, user.LastName, user.Email.Address,
            user.Username, user.Phone, user.Roles.Order().ToList(), providers);
    }

    public async Task<AccountDto> ChangeUsernameAsync(ChangeUsernameRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await usernameValidator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid) throw new ValidationException(result.Errors);

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
        var result = await consentValidator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid) throw new ValidationException(result.Errors);

        var user = await GetCurrentUserAsync();
        user.AcceptConsent(options.Value.ConsentVersion, timeProvider.GetUtcNow(), currentUser.IpAddress);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await authService.RefreshSessionAsync(user.Id, cancellationToken)
               ?? throw new UnauthorizedAccessException();
    }

    /// <exception cref="UnauthorizedAccessException">Нет аккаунта (аноним или сервисный админ).</exception>
    private async Task<User> GetCurrentUserAsync()
    {
        if (currentUser.UserId is not { } userId)
            throw new UnauthorizedAccessException();

        return await users.FindByIdAsync(userId) ?? throw new UnauthorizedAccessException();
    }
}
