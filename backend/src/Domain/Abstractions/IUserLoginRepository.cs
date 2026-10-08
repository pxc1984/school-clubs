using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Domain.Abstractions;

public interface IUserLoginRepository : IRepository<UserLogin>
{
    /// <summary>Ищет способ входа; провайдер и ключ нормализуются так же, как при создании.</summary>
    Task<UserLogin?> FindAsync(string provider, string providerKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserLogin>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
