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

    public async Task<IReadOnlyList<Group>> ListWithMembersAsync(bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        var groups = DbSet.AsQueryable();
        if (!includeArchived)
            groups = groups.Where(g => !g.IsArchived);

        return await groups
            .Include(g => g.Teachers)
            .Include(g => g.Students)
            .AsSplitQuery()
            .OrderBy(g => g.Title)
            .ToListAsync(cancellationToken);
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

    public async Task<IReadOnlyList<Group>> ListWithStudentsByStudentsAsync(IReadOnlyCollection<Guid> studentIds,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(g => g.Students)
            .Where(g => g.Students.Any(s => studentIds.Contains(s.Id)))
            .OrderBy(g => g.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasLessonsAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<Lesson>().AnyAsync(l => l.ParentGroupId == groupId, cancellationToken);
    }
}
