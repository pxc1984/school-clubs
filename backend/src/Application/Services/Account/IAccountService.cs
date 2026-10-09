using SchoolClubs.Application.Services.Auth;

namespace SchoolClubs.Application.Services.Account;

public interface IAccountService
{
    Task<AccountDto> GetAsync(CancellationToken cancellationToken = default);

    /// <exception cref="Common.Exceptions.ConflictException">Ник занят другим пользователем.</exception>
    Task<AccountDto> ChangeUsernameAsync(ChangeUsernameRequest request, CancellationToken cancellationToken = default);

    /// <summary>Принимает текущую версию политики ПДн; возвращает обновлённую сессию.</summary>
    Task<SessionDto> AcceptConsentAsync(AcceptConsentRequest request, CancellationToken cancellationToken = default);
}
