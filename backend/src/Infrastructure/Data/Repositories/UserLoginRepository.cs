using Microsoft.EntityFrameworkCore;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Infrastructure.Data.Repositories;

public class UserLoginRepository(DbContext dbContext) : Repository<UserLogin>(dbContext), IUserLoginRepository
{
    public async Task<UserLogin?> FindAsync(LoginProvider provider, string providerKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerKey))
            return null;

        var key = UserLogin.NormalizeKey(provider, providerKey);
        return await DbSet.FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderKey == key,
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
