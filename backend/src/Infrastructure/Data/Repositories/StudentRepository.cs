using Microsoft.EntityFrameworkCore;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Gradebook;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Infrastructure.Data.Repositories;

public class StudentRepository(DbContext dbContext) : Repository<Student>(dbContext), IStudentRepository
{
    public async Task<IReadOnlyList<Student>> ListByGroupAsync(Guid groupId,
        CancellationToken cancellationToken = default)
    {
        // У Student нет навигации на группы — идём со стороны Group.
        return await DbContext.Set<Group>()
            .Where(g => g.Id == groupId)
            .SelectMany(g => g.Students)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync(cancellationToken);
    }
}
