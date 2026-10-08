using Microsoft.EntityFrameworkCore;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Gradebook;

namespace SchoolClubs.Infrastructure.Data.Repositories;

public class GroupRepository(DbContext dbContext) : Repository<Group>(dbContext), IGroupRepository
{
    public async Task<Group?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(g => g.Teachers)
            .Include(g => g.Students)
            .AsSplitQuery()
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Group>> ListByTeacherAsync(Guid teacherId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(g => g.Teachers.Any(t => t.Id == teacherId))
            .OrderBy(g => g.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Group>> ListByStudentAsync(Guid studentId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(g => g.Students.Any(s => s.Id == studentId))
            .OrderBy(g => g.Title)
            .ToListAsync(cancellationToken);
    }
}
