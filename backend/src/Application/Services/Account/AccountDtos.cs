using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Application.Services.Account;

public record AccountDto(
    Guid Id,
    string FirstName,
    string MiddleName,
    string LastName,
    string Email,
    string? Username,
    string? Phone,
    Guid? AvatarId,
    IReadOnlyList<UserRole> Roles,
    IReadOnlyList<string> LoginProviders);

/// <param name="Accept">Чекбокс «Согласен с политикой обработки персональных данных».</param>
public record AcceptConsentRequest(bool Accept);

public record UpdateAccountRequest(string FirstName, string LastName, string? MiddleName, string? Phone);

/// <param name="CurrentPassword"><c>null</c>, если пароля ещё нет (вход только через внешний провайдер).</param>
public record ChangePasswordRequest(string? CurrentPassword, string NewPassword);

/// <param name="Username">Ник; ведущий <c>@</c> и регистр не важны. <c>null</c> или пусто — убрать.</param>
public record ChangeUsernameRequest(string? Username);
