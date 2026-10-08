using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Domain.Abstractions;

public interface IUserLoginRepository : IRepository<UserLogin>
{
    /// <summary>Ищет способ входа; ключ нормализуется так же, как при создании.</summary>
    Task<UserLogin?> FindAsync(LoginProvider provider, string providerKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserLogin>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
