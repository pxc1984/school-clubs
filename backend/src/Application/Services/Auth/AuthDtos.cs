using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Application.Services.Auth;

/// <summary>Всё, что нужно Web, чтобы выпустить cookie сессии.</summary>
/// <param name="UserId"><c>null</c> у сервисного админа.</param>
/// <param name="SecurityStamp">Сравнивается при периодической проверке сессии.</param>
public record SessionDto(
    Guid? UserId,
    IReadOnlyList<UserRole> Roles,
    string? SecurityStamp,
    bool ConsentRequired,
    bool IsServiceAdmin);

public record MeDto(
    Guid? Id,
    string? FirstName,
    string? MiddleName,
    string? LastName,
    string? Email,
    IReadOnlyList<UserRole> Roles,
    bool ConsentRequired,
    bool IsServiceAdmin);

/// <param name="AcceptPersonalDataPolicy">Чекбокс «Согласен с политикой обработки персональных данных».</param>
public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? MiddleName,
    string? Phone,
    bool AcceptPersonalDataPolicy);

public record LoginRequest(string Email, string Password);

public record ServiceAdminLoginRequest(string Password);

/// <summary>Пользователь, которого подтвердил внешний провайдер (OIDC). Не зависит от конкретного провайдера.</summary>
/// <param name="Provider">Имя провайдера из конфига (<c>yandex</c>).</param>
/// <param name="Subject">Claim <c>sub</c>.</param>
/// <param name="EmailVerified">Провайдер ручается за email — только тогда по нему можно найти существующий аккаунт.</param>
public record ExternalIdentity(
    string Provider,
    string Subject,
    string? Email,
    bool EmailVerified,
    string? FirstName,
    string? LastName,
    string? MiddleName);
