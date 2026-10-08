using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Domain.Abstractions;

/// <summary>Репозиторий всех пользователей с доступом к сервису (учителя, родители, админы).</summary>
public interface IUserRepository : IRepository<User>
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Пользователь вместе с детьми.</summary>
    Task<User?> GetWithChildrenAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Пользователь вместе с группами, которые он ведёт.</summary>
    Task<User?> GetWithGroupsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> ListByChildAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> ListByRoleAsync(UserRole role, CancellationToken cancellationToken = default);
}
