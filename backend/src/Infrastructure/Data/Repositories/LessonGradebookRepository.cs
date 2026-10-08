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
}
