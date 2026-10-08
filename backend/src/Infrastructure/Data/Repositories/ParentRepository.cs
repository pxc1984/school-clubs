using Microsoft.EntityFrameworkCore;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Infrastructure.Data.Repositories;

public class ParentRepository(DbContext dbContext) : Repository<Parent>(dbContext), IParentRepository
{
    public async Task<Parent?> GetWithChildrenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Children)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Parent>> ListByChildAsync(Guid studentId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.Children.Any(c => c.Id == studentId))
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync(cancellationToken);
    }
}
