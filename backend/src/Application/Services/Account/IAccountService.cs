using SchoolClubs.Application.Services.Auth;

namespace SchoolClubs.Application.Services.Account;

public interface IAccountService
{
    Task<AccountDto> GetAsync(CancellationToken cancellationToken = default);

    Task<AccountDto> UpdateAsync(UpdateAccountRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Меняет пароль (нужен текущий) или задаёт первый — для входа по email аккаунта.
    /// Остальные сессии завершаются; возвращает новую сессию для текущей.
    /// </summary>
    Task<SessionDto> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>Отвязывает внешний провайдер; последний способ входа отвязать нельзя.</summary>
    Task UnlinkLoginAsync(string provider, CancellationToken cancellationToken = default);

    /// <exception cref="Common.Exceptions.ConflictException">Ник занят другим пользователем.</exception>
    Task<AccountDto> ChangeUsernameAsync(ChangeUsernameRequest request, CancellationToken cancellationToken = default);

    /// <summary>Принимает текущую версию политики ПДн; возвращает обновлённую сессию.</summary>
    Task<SessionDto> AcceptConsentAsync(AcceptConsentRequest request, CancellationToken cancellationToken = default);
}
