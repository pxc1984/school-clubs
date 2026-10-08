namespace SchoolClubs.Application.Services.Auth;

public interface IAuthService
{
    /// <summary>Регистрация родителя по email и паролю; согласие на обработку ПДн обязательно.</summary>
    Task<SessionDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <exception cref="UnauthorizedAccessException">Неверный email или пароль.</exception>
    Task<SessionDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Вход через внешний провайдер: по привязанному логину, затем по подтверждённому email, иначе — новый родитель.
    /// </summary>
    Task<SessionDto> ExternalLoginAsync(ExternalIdentity identity, CancellationToken cancellationToken = default);

    /// <exception cref="UnauthorizedAccessException">Неверный пароль или сервисный вход выключен.</exception>
    SessionDto LoginServiceAdmin(ServiceAdminLoginRequest request);

    /// <summary>Актуальное состояние сессии пользователя; <c>null</c>, если его больше нельзя пускать.</summary>
    Task<SessionDto?> RefreshSessionAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<MeDto> GetMeAsync(CancellationToken cancellationToken = default);
}
