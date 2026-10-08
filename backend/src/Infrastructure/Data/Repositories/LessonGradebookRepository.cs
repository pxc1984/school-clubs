using Microsoft.EntityFrameworkCore;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Gradebook;

namespace SchoolClubs.Infrastructure.Data.Repositories;

public class LessonGradebookRepository(DbContext dbContext)
    : Repository<LessonGradebook>(dbContext), ILessonGradebookRepository
{
    public async Task<LessonGradebook?> GetByLessonIdAsync(Guid lessonId,
        CancellationToken cancellationToken = default)
    {
        // Task.Entries и GradebookEntry.Task/Lesson заполняются fix-up'ом трекера из загруженных коллекций.
        return await DbSet
            .Include(g => g.Lesson)
            .ThenInclude(l => l.Tasks)
            .Include(g => g.GradebookEntries)
            .Include(g => g.StoredFile)
            .AsSplitQuery()
            .FirstOrDefaultAsync(g => g.LessonId == lessonId, cancellationToken);
    }

    public async Task<IReadOnlyList<LessonGradebook>> ListByGroupAsync(Guid groupId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(g => g.GradebookEntries)
            .Where(g => g.Lesson.ParentGroupId == groupId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LessonGradebook>> ListByStudentAsync(Guid studentId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(g => g.Lesson)
            .ThenInclude(l => l.Tasks)
            .Include(g => g.GradebookEntries)
            .AsSplitQuery()
            .Where(g => g.GradebookEntries.Any(e => e.StudentId == studentId))
            .ToListAsync(cancellationToken);
    }
}
