using Microsoft.EntityFrameworkCore;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Infrastructure.Data.Repositories;

public class UserLoginRepository(DbContext dbContext) : Repository<UserLogin>(dbContext), IUserLoginRepository
{
    public async Task<UserLogin?> FindAsync(string provider, string providerKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(providerKey))
            return null;

        var normalizedProvider = UserLogin.NormalizeProvider(provider);
        var key = UserLogin.NormalizeKey(normalizedProvider, providerKey);
        return await DbSet.FirstOrDefaultAsync(l => l.Provider == normalizedProvider && l.ProviderKey == key,
            cancellationToken);
    }

    public async Task<IReadOnlyList<UserLogin>> ListByUserAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.Provider)
            .ToListAsync(cancellationToken);
    }
}
