using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Application.Services.Admin;

public interface IAdminUserService
{
    Task<IReadOnlyList<UserSummaryDto>> SearchAsync(string? query,
        UserRole? role,
        CancellationToken cancellationToken = default);

    Task<UserDetailsDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Приводит набор ролей к заданному; снять Admin с самого себя нельзя.</summary>
    Task<UserDetailsDto> SetRolesAsync(Guid id, SetRolesRequest request, CancellationToken cancellationToken = default);

    Task<UserDetailsDto> BlockAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserDetailsDto> UnblockAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Новый пользователь с ролью Teacher или выдача роли существующему с этим email.</summary>
    Task<UserDetailsDto> InviteTeacherAsync(InviteTeacherRequest request, CancellationToken cancellationToken = default);

    /// <summary>Задаёт временный пароль (заводит вход по паролю, если его не было) и завершает сессии.</summary>
    Task<TemporaryPasswordDto> ResetPasswordAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Привязывает ребёнка; роль Parent выдаётся автоматически.</summary>
    Task<UserDetailsDto> LinkChildAsync(Guid parentId, Guid studentId, CancellationToken cancellationToken = default);

    Task<UserDetailsDto> UnlinkChildAsync(Guid parentId, Guid studentId, CancellationToken cancellationToken = default);
}
