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

    /// <summary>Пользователь с детьми и группами — нужно для доменных проверок при снятии ролей.</summary>
    Task<User?> GetWithLinksAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Родители ученика вместе с их детьми.</summary>
    Task<IReadOnlyList<User>> ListByChildAsync(Guid studentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> ListByRoleAsync(UserRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Поиск по словам: каждое слово — подстрока фамилии, имени или отчества; слово с <c>@</c> — точный email.
    /// </summary>
    Task<IReadOnlyList<User>> SearchAsync(string? query,
        UserRole? role,
        int limit,
        CancellationToken cancellationToken = default);
}
