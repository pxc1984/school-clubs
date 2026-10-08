using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Application.Common.Interfaces;

/// <summary>Кто выполняет запрос. Реализация живёт в Web поверх HttpContext.</summary>
public interface ICurrentUser
{
    /// <summary><c>null</c> для анонима и сервисного админа (у него нет аккаунта).</summary>
    Guid? UserId { get; }

    bool IsAuthenticated { get; }

    /// <summary>Вход по паролю из конфига, без аккаунта.</summary>
    bool IsServiceAdmin { get; }

    IReadOnlyCollection<UserRole> Roles { get; }

    string? IpAddress { get; }

    bool IsInRole(UserRole role);
}
