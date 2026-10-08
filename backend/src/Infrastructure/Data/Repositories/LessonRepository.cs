using Microsoft.EntityFrameworkCore;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Gradebook;

namespace SchoolClubs.Infrastructure.Data.Repositories;

public class LessonRepository(DbContext dbContext) : Repository<Lesson>(dbContext), ILessonRepository
{
    public async Task<Lesson?> GetWithTasksAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(l => l.Tasks)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Lesson>> ListWithTasksByGroupAsync(Guid groupId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(l => l.Tasks)
            .Where(l => l.ParentGroupId == groupId)
            .OrderBy(l => l.Date)
            .ThenBy(l => l.Time)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Lesson>> ListByGroupAsync(
        Guid groupId,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(l => l.ParentGroupId == groupId);
        if (from is not null) query = query.Where(l => l.Date >= from);
        if (to is not null) query = query.Where(l => l.Date <= to);

        return await query
            .OrderBy(l => l.Date)
            .ThenBy(l => l.Time)
            .ToListAsync(cancellationToken);
    }
}
