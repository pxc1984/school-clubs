using Microsoft.EntityFrameworkCore;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Infrastructure.Data.Repositories;

public class TeacherRepository(DbContext dbContext) : Repository<Teacher>(dbContext), ITeacherRepository
{
    public async Task<Teacher?> GetWithGroupsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(t => t.LinkedGroups)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }
}
