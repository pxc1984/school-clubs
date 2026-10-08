using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Domain.Abstractions;

/// <summary>Репозиторий всех пользователей с доступом к сервису (учителя, родители).</summary>
public interface IUserRepository : IRepository<User>
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
}
