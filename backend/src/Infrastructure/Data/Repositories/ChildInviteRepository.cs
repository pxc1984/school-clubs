using Microsoft.EntityFrameworkCore;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Access;

namespace SchoolClubs.Infrastructure.Data.Repositories;

public class ChildInviteRepository(DbContext dbContext) : Repository<ChildInvite>(dbContext), IChildInviteRepository
{
    public async Task<ChildInvite?> FindByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(i => i.Token == token, cancellationToken);
    }

    public async Task<IReadOnlyList<ChildInvite>> ListActiveByStudentAsync(Guid studentId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(i => i.StudentId == studentId && i.AcceptedAt == null && i.RevokedAt == null && i.ExpiresAt > now)
            .OrderByDescending(i => i.Created)
            .ToListAsync(cancellationToken);
    }
}
