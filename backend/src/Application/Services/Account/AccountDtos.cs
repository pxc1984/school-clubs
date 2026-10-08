using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Application.Services.Account;

public record AccountDto(
    Guid Id,
    string FirstName,
    string MiddleName,
    string LastName,
    string Email,
    string? Phone,
    IReadOnlyList<UserRole> Roles,
    IReadOnlyList<string> LoginProviders);

/// <param name="Accept">Чекбокс «Согласен с политикой обработки персональных данных».</param>
public record AcceptConsentRequest(bool Accept);
